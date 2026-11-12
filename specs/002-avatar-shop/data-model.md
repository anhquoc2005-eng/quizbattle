# Data Model: Nhân vật và cửa hàng

Quy ước giữ nguyên Tính năng 1: bảng số nhiều PascalCase, `timestamptz`, enum lưu `text` (`HasConversion<string>()`), mọi thay đổi qua migration EF Core. Lý do từng quyết định: [research.md](research.md) (R2–R8).

## 1. Domain (QuizBattle.Domain)

| Thành phần | Mô tả |
|---|---|
| `enum AvatarSlot` | `Eyes, Eyebrows, Mouth, Nose, Hair, Clothes, Glasses, Beard, Gesture, ClothesGraphic, Background`. Tên chuỗi (camelCase, dùng ở `Key`, JSON và DiceBear) = `eyes, eyebrows, mouth, nose, hair, clothes, glasses, beard, gesture, clothesGraphic, background` |
| `enum AvatarTier` | `Free, Common (Thường), Rare (Hiếm), Special (Đặc biệt)` |
| `AvatarCatalog` (static) | Hằng `StyleVersion = "10.6.0"`, `DefaultBackgroundHex = "d4d4d4"`, `RequiredSlots`, `PriceOf(tier)` (50/150/400, Free = 0), `Items` (201 `AvatarItemDefinition`). Nguồn duy nhất cho seed, mặc định và test |
| `DefaultAvatarGenerator` (static, thuần) | `ForAccount(Guid)`, `ForGuest(string)`; FNV-1a 64 + SplitMix64, chỉ chọn món `Free` (research R2) |
| `AvatarSelection` (record) | Bản đồ ô → `variantKey?` đủ 11 ô; dùng chung cho nhân vật đã lưu, mặc định và DTO |
| `AvatarRules` (static, thuần) | Kiểm bộ chọn: đủ sáu ô bắt buộc; mỗi `variantKey` có trong danh mục đúng ô đó; trả danh sách lỗi (test được không cần DB) |

## 2. Thực thể và bảng mới

### 2.1 `AvatarItems` (danh mục, 201 dòng seed, bất biến)

| Cột | Kiểu | Ràng buộc |
|---|---|---|
| `Key` | text | **PK**, dạng `slot.variantKey`, ví dụ `hair.variant12` |
| `Slot` | text (enum `AvatarSlot`) | NOT NULL |
| `VariantKey` | text | NOT NULL, ví dụ `variant12`, `hat`, `galaxy`, `2a9d8f` |
| `Name` | text(60) | NOT NULL, tên hiển thị tiếng Việt (ví dụ "Tóc 12", "Xanh rêu") |
| `Tier` | text (enum `AvatarTier`) | NOT NULL |
| `Price` | int | NOT NULL, do server quyết định |
| `SortOrder` | int | NOT NULL, thứ tự hiển thị trong ô |

Ràng buộc: `UNIQUE (Slot, VariantKey)`; `CK_AvatarItems_Key` `"Key" = "Slot" || '.' || "VariantKey"`; `CK_AvatarItems_Price` `("Tier" = 'Free' AND "Price" = 0) OR ("Tier" <> 'Free' AND "Price" > 0)`. Index: PK đủ; danh mục đọc cả bảng (≈ 22 KB JSON).

### 2.2 `UserAvatarItems` (quyền sở hữu, chỉ thêm)

| Cột | Kiểu | Ràng buộc |
|---|---|---|
| `Id` | uuid | PK |
| `UserId` | uuid | NOT NULL, FK → `Users(Id)` ON DELETE CASCADE |
| `ItemKey` | text | NOT NULL, FK → `AvatarItems(Key)` ON DELETE RESTRICT |
| `AcquiredAt` | timestamptz | NOT NULL |

`UNIQUE (UserId, ItemKey)` (`IX_UserAvatarItems_UserId_ItemKey`): chặn sở hữu hai lần ở tầng DB. Chỉ lưu **món bán đã mua**; món miễn phí không có dòng (luôn dùng được). Không có thao tác xóa (không bán lại, hoàn, tặng).

### 2.3 `UserAvatars` (nhân vật đã lưu, 1-1 với `Users`; không có dòng = chưa từng chỉnh)

| Cột | Kiểu | Ràng buộc |
|---|---|---|
| `UserId` | uuid | **PK** và FK → `Users(Id)` ON DELETE CASCADE |
| `EyesItemKey`, `EyebrowsItemKey`, `MouthItemKey`, `NoseItemKey`, `HairItemKey`, `ClothesItemKey` | text | **NOT NULL**, FK → `AvatarItems(Key)` |
| `GlassesItemKey`, `BeardItemKey`, `GestureItemKey`, `ClothesGraphicItemKey`, `BackgroundItemKey` | text | NULL được (ô tùy chọn), FK → `AvatarItems(Key)` |
| `UpdatedAt` | timestamptz | NOT NULL |

`CHECK` theo ô cho cả 11 cột, ví dụ `CK_UserAvatars_Hair` `"HairItemKey" LIKE 'hair.%'`, `CK_UserAvatars_Background` `"BackgroundItemKey" IS NULL OR "BackgroundItemKey" LIKE 'background.%'`. Kiểm "món bán thì phải sở hữu" do service làm trong transaction (không biểu diễn được bằng ràng buộc một dòng; quyền sở hữu không bị xóa nên không có race).

### 2.4 Thay đổi `CoinTransactions`

| Thay đổi | Chi tiết |
|---|---|
| Cột mới `UserAvatarItemId` | uuid NULL, FK → `UserAvatarItems(Id)`, `ON DELETE NO ACTION` (xóa tài khoản cascade cả hai bảng trong cùng một lệnh; `NO ACTION` được kiểm cuối lệnh nên không vướng) |
| Index duy nhất có lọc | `IX_CoinTransactions_UserAvatarItemId` UNIQUE `WHERE "UserAvatarItemId" IS NOT NULL`: mỗi lần mua sinh đúng một dòng sổ cái |
| CHECK mới | `CK_CoinTransactions_ReasonShape`: `("Reason" = 'MatchReward' AND "Amount" > 0 AND "UserAvatarItemId" IS NULL) OR ("Reason" = 'ShopPurchase' AND "Amount" < 0 AND "UserAvatarItemId" IS NOT NULL)` |

CHECK mới được kiểm trên dữ liệu cũ khi áp migration: mọi dòng cũ là `MatchReward` với `Amount > 0` (`RewardSettlement` chỉ thêm khi `coinsTotal > 0`) và chưa có `UserAvatarItemId` ⇒ thỏa. Truy ra "mua món nào" = `CoinTransactions → UserAvatarItems → AvatarItems`.

**Bất biến giữ nguyên** (CLAUDE.md): `Users.CoinBalance` = `SUM(CoinTransactions.Amount)` (truy vấn kiểm trong [quickstart.md](quickstart.md)); `TotalXp` không bị mua đồ chạm tới.

## 3. Quan hệ

```text
Users 1──0..1 UserAvatars ──(11 cột ItemKey)──> AvatarItems
Users 1──* UserAvatarItems ──(ItemKey)──────────> AvatarItems
UserAvatarItems 1──0..1 CoinTransactions (mua) ; Users 1──* CoinTransactions
```

## 4. Trạng thái và quy tắc

| Quy tắc | Nơi thực thi |
|---|---|
| Mua: món tồn tại, bán (không Free), chưa sở hữu, đủ Xu; trừ Xu + thêm sở hữu + thêm sổ cái cùng một transaction dưới khóa dòng `Users` | `ShopService` + `AvatarRepository` (tra `AvatarItems` trong DB); lưới an toàn: `UNIQUE (UserId, ItemKey)` |
| Lưu nhân vật: đủ sáu ô bắt buộc; mỗi món đúng ô, có trong DB, Free hoặc có sở hữu; thất bại thì không lưu gì | `AvatarService` + `AvatarRules`; FK/CHECK là lưới an toàn |
| Chưa từng lưu ⇒ nhân vật mặc định từ `DefaultAvatarGenerator` | Service (không ghi DB) |
| Ô tùy chọn trống ⇒ cột NULL; `background` NULL vẽ nền mặc định | Cột NULL + frontend dùng hằng cùng giá trị `DefaultBackgroundHex` |

## 5. Danh mục: món miễn phí và bảng giá (cần cố vấn duyệt trước khi áp lên prod)

Tổng: **201 món = 82 miễn phí + 119 bán** (53 Thường, 55 Hiếm, 11 Đặc biệt). Mua hết cần 15.300 Xu. Tham chiếu: thưởng một trận = `floor(điểm/100)` + 30/20/10 cho top 3 (Tính năng 1), nên món Thường ≈ 1-3 trận, Hiếm ≈ 4-8, Đặc biệt ≈ 10+.

### 5.1 Miễn phí (82)

| Ô | Món miễn phí | Số |
|---|---|---|
| mắt `eyes` | toàn bộ `variant01`..`variant05` | 5 |
| lông mày `eyebrows` | toàn bộ `variant01`..`variant13` | 13 |
| miệng `mouth` | toàn bộ `variant01`..`variant30` | 30 |
| mũi `nose` | toàn bộ `variant01`..`variant20` | 20 |
| tóc `hair` | **`variant01`..`variant08`** (8 kiểu) | 8 |
| áo `clothes` | **`variant01`..`variant05`** (5 kiểu; đều in được hình, xem R1-a) | 5 |
| nền `background` | `d4d4d4` "Xám mặc định" | 1 |

Chọn theo thứ tự đầu danh sách vì chưa thể xem hình trong plan; đây là dữ liệu, đổi được **trước khi áp migration lên prod** (xem câu hỏi Q-C ở [plan.md](plan.md)).

### 5.2 Món bán (119) và mức giá (Thường 50, Hiếm 150, Đặc biệt 400)

| Ô | Thường (50) | Hiếm (150) | Đặc biệt (400) | Tổng |
|---|---|---|---|---|
| tóc `hair` (56) | `variant09`..`variant32` (24) | `variant33`..`variant56` (24) | `hat`, `variant57`..`variant63` (8) | 56 |
| áo `clothes` (20) | `variant06`..`variant17` (12) | `variant18`..`variant25` (8) | không | 20 |
| kính `glasses` (11) | `variant01`..`variant06` (6) | `variant07`..`variant11` (5) | không | 11 |
| râu `beard` (12) | `variant01`..`variant06` (6) | `variant07`..`variant12` (6) | không | 12 |
| cử chỉ `gesture` (10) | không | **tất cả 10** (theo yêu cầu) | không | 10 |
| hình in `clothesGraphic` (3) | không | không | **`electric`, `galaxy`, `saturn`** (theo yêu cầu) | 3 |
| nền `background` (7) | 5 màu | 2 màu | không | 7 |
| **Cộng** | **53** | **55** | **11** | **119** |

Quy tắc phân mức: ô nhiều lựa chọn (tóc, áo) chia theo thứ tự chỉ số, hai mức thấp chiếm phần lớn, mức Đặc biệt chỉ dành cho hình in (yêu cầu) và nhóm tóc cuối danh sách có `hat`. Tên hiển thị tạm là "Tóc 09", "Áo 06"...; cố vấn có thể đổi tên/mức bằng danh sách trong `AvatarCatalog` trước Đợt A.

### 5.3 Tám màu nền (`background`)

Phẳng, không gradient (`backgroundColorFill` mặc định `solid`). Nét vẽ của nhân vật là `#000000` nên mọi nền đều sáng vừa, tương phản với nét ≥ 11:1, và đều tách khỏi nền giao diện `#000000` và thẻ `#111111`. Không dùng vàng của `accent` (`#F5C518`).

| `variantKey` (hex, không `#`) | Tên | Mức | Giá |
|---|---|---|---|
| `d4d4d4` | Xám mặc định | Miễn phí | 0 |
| `b9c6d6` | Xanh sương | Thường | 50 |
| `b8d4be` | Xanh rêu | Thường | 50 |
| `e3d5b8` | Cát | Thường | 50 |
| `e8c4c4` | Hồng bụi | Thường | 50 |
| `cfc4e3` | Tím oải hương | Thường | 50 |
| `a9d3dc` | Xanh biển | Hiếm | 150 |
| `ebc4a0` | Đất nung | Hiếm | 150 |

## 6. Migration

- Tên: **`AddAvatarShop`** (một migration, sinh `dotnet ef migrations add AddAvatarShop`): tạo ba bảng, thêm cột/FK/index/CHECK vào `CoinTransactions`, seed `AvatarItems` bằng `HasData` (201 `InsertData`), cộng ba `ALTER TABLE ... ENABLE ROW LEVEL SECURITY` bằng `migrationBuilder.Sql`.
- Kiểm trên DB riêng trước (cách của T089 Tính năng 1): tạo DB mới, migrate tới `AddXpCoinsAndPlayerAccounts` (migration ngay trước), nạp dữ liệu mẫu (User, một `CoinTransaction` `MatchReward`), chạy `scripts/migrate-prod.ps1` (sinh `scripts/out/migrate-prod.sql`), áp bằng `psql` **hai lần** (idempotent), kiểm dữ liệu cũ nguyên vẹn, CHECK mới không làm hỏng dòng cũ, 201 dòng danh mục, RLS bật, thử các vi phạm (mua trùng, CHECK sai ô). Không chạy trên DB dev hiện tại.
- Production: chạy `scripts/migrate-prod.ps1` **trước khi merge** (theo yêu cầu). Migration chỉ thêm (không phá dữ liệu); backend cũ (chưa có tính năng) vẫn chạy được với schema mới ⇒ thứ tự "migration trước, merge sau" an toàn.
