# Implementation Plan: Nhân vật và cửa hàng

**Branch**: `feat/avatar-shop` | **Date**: 2026-11-12 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification từ [spec.md](spec.md); code thật đối chiếu trên nhánh (gốc `main` `c64e46a`).

## Summary

Người chơi có tài khoản sở hữu một nhân vật ghép 11 ô (kiểu Notionists của DiceBear, đen trắng, cộng ô màu nền do dự án định nghĩa), mua đồ bằng Xu của Tính năng 1 và mặc đồ ở `/profile`. Nhân vật hiện ở `/profile`, `/shop`, sảnh chờ, bảng điểm giữa/cuối trận và `/ranking`.

Cách làm:

- **Backend chỉ lưu và trả cặp ô → `variantKey`**; ảnh SVG vẽ ở frontend bằng `@dicebear/core` + `@dicebear/styles` (ghim `10.6.0`).
- **Danh mục 201 món** định nghĩa một lần trong Domain (`AvatarCatalog`), seed vào bảng `AvatarItems` bằng migration `AddAvatarShop`; nhân vật mặc định (tài khoản chưa chỉnh, khách) là hàm thuần ở Domain, chỉ chọn món miễn phí.
- **Mua đồ** là một transaction khóa dòng `Users` `FOR UPDATE`, kiểm sở hữu và số dư, trừ `CoinBalance`, ghi `CoinTransactions` âm (`ShopPurchase`) liên kết tới bản ghi sở hữu. Giữ bất biến `CoinBalance = SUM(CoinTransactions)`.
- **Hiển thị trong phòng**: thêm một truy vấn `UserAvatars` vào chỗ đã nạp `Users` của snapshot phòng; DTO `PlayerSummary`/`LeaderboardEntry`/`PlayerJoined`/`XpLeaderboardEntry` thêm trường `avatar`.

Chi tiết quyết định: [research.md](research.md). Dữ liệu: [data-model.md](data-model.md). Hợp đồng: [contracts/rest-api.md](contracts/rest-api.md), [contracts/signalr-events.md](contracts/signalr-events.md). Chạy kiểm chứng: [quickstart.md](quickstart.md).

## Technical Context

**Language/Version**: C# / .NET 8 (ASP.NET Core 8), TypeScript 5.8 strict + React 18 + Vite 6 (không đổi)

**Primary Dependencies**: EF Core + Npgsql, SignalR (đã có). **Mới (đã duyệt)**: `@dicebear/core@10.6.0` và `@dicebear/styles@10.6.0` (npm, ghim chính xác, frontend). Backend không thêm gói NuGet.

**Storage**: PostgreSQL. Thêm 3 bảng (`AvatarItems`, `UserAvatars`, `UserAvatarItems`) và 1 cột/FK/CHECK trên `CoinTransactions`; không đổi `Users`.

**Testing**: xUnit (Domain thuần, service với lớp giả tự viết, tích hợp Postgres qua `DatabaseFixture`/`QUIZBATTLE_TEST_DB`), Vitest + `jsdom` + `@testing-library/react` cho màn có trạng thái tải/lỗi/rỗng.

**Target Platform**: Web (điện thoại là chính) + backend ASP.NET chạy liên tục; DB production Supabase (chỉ dùng như PostgreSQL).

**Project Type**: Web application (backend + frontend), cấu trúc sẵn có của dự án.

**Performance Goals**: phòng 50 người: thêm đúng 1 truy vấn cho nhân vật mỗi lần dựng snapshot; payload cộng thêm (đo thật) ≈ 22 đến 24 KB (`RoomState`), ≈ 11,8 KB mỗi `LeaderboardUpdated`; `notionists.json` (372 KB thô, ≈ 111–113 KB gzip) nạp bằng `import()` động và tách thành chunk riêng khỏi bundle chính, chỉ tải ở màn có nhân vật; Đợt D ghi kích thước bundle chính trước/sau (`vite build`) vào báo cáo và kiểm bundle chính không chứa `notionists`.

**Constraints**: không gọi `api.dicebear.com`; giá do server; mọi ghi `Users` theo quy tắc khóa của CLAUDE.md; bảng mới bật RLS; migration production chạy tay trước khi merge.

**Scale/Scope**: ≤ 50 người chơi mỗi phòng; 201 món; ~10 file backend mới, ~25 file frontend mới/sửa.

## Constitution Check

*Cổng: phải qua trước Phase 0, kiểm lại sau Phase 1.* Nguồn: [constitution.md](../../.specify/memory/constitution.md) (CLAUDE.md thắng khi mâu thuẫn).

| Nguyên tắc | Đạt? | Ghi chú |
|---|---|---|
| 1. Server là nguồn sự thật | Đạt | Giá, sở hữu, nhân vật mặc định đều ở server; client chỉ gửi `itemKey`/`variantKey` và vẽ |
| 2. Phân lớp, DI qua interface, logic thuần ở Domain | Đạt | `AvatarCatalog`, `DefaultAvatarGenerator`, `AvatarRules` ở Domain (thuần, có unit test). Controller → `IShopService`/`IAvatarService` → `IAvatarRepository` → DbContext |
| 3. Bảo mật | Đạt | Validate mọi input; mua/lưu cần JWT; `UnmappedMemberHandling.Disallow`; RLS cho bảng mới; không secret mới. Không thêm rate limit mới (không phải endpoint AI/đăng nhập) |
| 4. Quy trình tính năng | Đạt | Nhánh `feat/avatar-shop`, chia Đợt, mỗi Đợt build + test xanh rồi push |
| 5. Không thêm thư viện khi chưa hỏi | Đạt | Hai gói DiceBear **đã được duyệt** (yêu cầu của người làm); không thêm gói nào khác |

Kiểm lại sau Phase 1: không phát sinh vi phạm. **Cần sửa CLAUDE.md** (đề xuất, làm ở Đợt D cùng thay đổi): thêm DiceBear vào mục Stack kèm cấm `api.dicebear.com` và quy tắc ghim phiên bản; thêm `src/avatar/` vào cấu trúc thư mục frontend. Theo CLAUDE.md, kiến trúc phân lớp là yêu cầu của dự án, nên `IAvatarRepository` có chủ đích và không phải abstraction thừa.

## Project Structure

### Documentation (this feature)

```text
specs/002-avatar-shop/
├── plan.md              # File này
├── research.md          # Phase 0 (gồm kết quả thử nghiệm DiceBear)
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1
├── contracts/
│   ├── rest-api.md
│   └── signalr-events.md
├── checklists/requirements.md
└── tasks.md             # Do /speckit-tasks tạo (chưa chạy)
```

### Source Code (chỉ liệt kê file thêm/sửa)

```text
backend/
├── QuizBattle.Domain/
│   ├── Entities/ AvatarItem.cs, UserAvatar.cs, UserAvatarItem.cs (mới); CoinTransaction.cs (thêm UserAvatarItemId)
│   ├── Enums/ AvatarSlot.cs, AvatarTier.cs (mới)
│   └── Avatars/ AvatarCatalog.cs, AvatarItemDefinition.cs, DefaultAvatarGenerator.cs, AvatarRules.cs, AvatarSelection.cs (mới)
├── QuizBattle.Application/
│   ├── Shop/ IShopService, ShopService, IAvatarRepository, AvatarItemDto, PurchaseRequest, PurchaseResultDto (mới)
│   ├── Avatars/ IAvatarService, AvatarService, AvatarDto, MyAvatarDto, SaveAvatarRequest (mới)
│   ├── Common/ ConflictException, NotFoundException, RequestValidationException (thêm Code)
│   ├── Game/ GameSnapshot (Avatars), GameService (AvatarOf), PlayerSummaryDto, LeaderboardEntryDto, PlayerJoinedDto (thêm avatar)
│   └── Profile/ XpLeaderboardEntryDto (thêm avatar), ProfileService, XpRankRow
├── QuizBattle.Infrastructure/Persistence/
│   ├── Configurations/ AvatarItemConfiguration, UserAvatarConfiguration, UserAvatarItemConfiguration (mới); CoinTransactionConfiguration (sửa)
│   ├── AvatarRepository.cs (mới); GameRepository.cs, UserRepository.cs (sửa)
│   └── Migrations/ <timestamp>_AddAvatarShop.cs (+ Designer, Snapshot)
├── QuizBattle.Api/
│   ├── Controllers/ ShopController.cs (mới); UsersController.cs (thêm /avatar)
│   └── Middleware/ GlobalExceptionHandler.cs (ghi code chung)
└── QuizBattle.Tests/ Domain/ Avatars/…; Shop/…; Integration/ ShopPurchaseTests, AvatarSaveTests, AvatarRlsTests, GameAvatarsQueryTests

frontend/
├── package.json (+2 gói, ghim), .npmrc (save-exact)
└── src/
    ├── avatar/ avatarOptions.ts, avatarRenderer.ts (nạp lazy + cache), avatarSlots.ts, printableClothes.ts, *.test.ts
    ├── api/ shop.ts, avatar.ts (+ ánh xạ code lỗi), leaderboards.ts (kiểu mới)
    ├── components/ Avatar.tsx, AvatarEditor.tsx (tách nhỏ), ShopItemCard.tsx, CoinBalance.tsx
    ├── hooks/ useShopItems.ts, useMyAvatar.ts
    ├── pages/ ShopPage/ (mới), ProfilePage/ (thêm trình chỉnh)
    ├── types/ avatar.ts, game.ts, profile.ts (thêm avatar)
    └── realtime/ gameReducer.ts (giữ avatar)
THIRD_PARTY_NOTICES.md (gốc repo)
```

**Structure Decision**: giữ nguyên bố cục `backend/*` + `frontend/src/*` của CLAUDE.md. Thêm thư mục `src/avatar/` cho logic thuần (dựng tùy chọn DiceBear, bộ nhớ đệm, danh sách ô) vì test được bằng Vitest không cần DOM.

## Câu trả lời cho 9 điểm yêu cầu

### 1. DiceBear (chi tiết: [research.md R1](research.md))

- Gói và phiên bản ghim chính xác: **`@dicebear/core@10.6.0`** (MIT) và **`@dicebear/styles@10.6.0`** (file `notionists.json` là CC0 1.0). Thế hệ 10.x **không còn gói `@dicebear/notionists`** (bản cuối 9.4.2); kiểu nằm trong `@dicebear/styles`, phiên bản "styles 10.6.0" trong yêu cầu chính là gói này. `@dicebear/core` `latest` hiện là 10.7.0 nên ghim tường minh `10.6.0`.
- Đã thử ngoài repo (thư mục tạm, không commit): vẽ `hair=variant12`, kính rỗng (`glassesProbability: 0`), `gesture=ok`, nền `2a9d8f` ra SVG hợp lệ; ép được từng variant của mọi ô (193 trường hợp không lỗi); số variant mỗi ô khớp bảng yêu cầu. Lệnh và đoạn code đầy đủ ở research.md R1.
- **Hai phát hiện** (cố vấn đã duyệt cách xử lý): (a) hình in chỉ hiện trên áo `variant01`..`variant10` (15 kiểu áo còn lại không có chỗ in) ⇒ server từ chối lưu, xem mục "Quyết định đã duyệt" Q-A; (b) không có nền thì SVG trong suốt, nét đen mất trên nền giao diện đen ⇒ ô nền trống phải vẽ nền mặc định `d4d4d4`.

### 2. Nhân vật mặc định ([research R2](research.md))

Hàm thuần ở Domain: `DefaultAvatarGenerator.ForAccount(Guid)` và `ForGuest(string)`. Hash **FNV-1a 64 bit** trên UTF-8 của `"u:<userId N>"` / `"g:<tên chuẩn hoá>"`, mỗi ô lấy `SplitMix64(hash + (i+1)·φ) % số món miễn phí dùng cho mặc định của ô` (ô tóc: 8 kiểu, **không bao giờ khăn trùm đầu** `variant61`..`63`, dù khăn vẫn miễn phí và chọn được ở trình chỉnh); tuyệt đối không dùng `string.GetHashCode`. Chỉ ô bắt buộc có giá trị, nền = xám mặc định (cũng là nền của khách). Mọi DTO (`avatar` trong `PlayerSummary`, `LeaderboardEntry`, `PlayerJoined`, `XpLeaderboardEntry`, `GET /api/users/me/avatar`) luôn có đủ ô; **frontend không sinh**. Test: cùng đầu vào cùng kết quả (cả giá trị cố định "golden"), chỉ món miễn phí, đủ sáu ô bắt buộc, khách không phân biệt hoa thường, 10.000 khóa ngẫu nhiên đều hợp lệ và phân tán.

### 3. Data model ([data-model.md](data-model.md))

- **Danh mục**: bảng `AvatarItems`, khóa chính chuỗi `"<ô>.<variantKey>"` (ví dụ `hair.variant12`): seed cố định giống nhau mọi môi trường, đọc được trong log, client gửi nguyên mã; Guid cần bảng đối chiếu mà không thêm bảo đảm nào (R4).
- **Sở hữu**: `UserAvatarItems` với `UNIQUE (UserId, ItemKey)`; chỉ lưu món đã mua.
- **Nhân vật**: bảng riêng `UserAvatars` 1-1 (không thêm cột vào `Users`, bảng bị khóa nóng); mỗi ô một cột `…ItemKey` có FK → `AvatarItems`, `NOT NULL` cho 6 ô bắt buộc, `CHECK … LIKE '<ô>.%'` theo ô. Mọi variant hợp lệ nhờ FK + CHECK; "chưa sở hữu" kiểm trong service cùng transaction (R5).
- **Sổ cái**: `CoinTransactions.UserAvatarItemId` (FK nullable → `UserAvatarItems`, unique có lọc `IS NOT NULL`) và `CK_CoinTransactions_ReasonShape`: `MatchReward` ⇒ `Amount > 0` và không có `UserAvatarItemId`; `ShopPurchase` ⇒ `Amount < 0` và bắt buộc có. Dòng cũ thỏa CHECK mới.

### 4. Món miễn phí, giá, màu nền ([data-model.md mục 5](data-model.md))

- Miễn phí (**85**): mắt 5, lông mày 13, miệng 30, mũi 20 (toàn bộ); **tóc 11** = `variant01, 02, 05, 08, 20, 23, 41, 45` + 3 khăn trùm đầu `variant61` "Khăn quấn đầu", `62` "Khăn trùm đầu 1", `63` "Khăn trùm đầu 2" (trang phục tôn giáo, luôn miễn phí); **áo 5** = `variant01, 02, 07, 14, 23`; nền `d4d4d4`.
- Bán (**116**): bảng phân bổ theo ô và mức ở data-model 5.2 (**53 Thường, 52 Hiếm, 11 Đặc biệt**; cử chỉ toàn Hiếm, hình in toàn Đặc biệt; tóc Thường 24, Hiếm 21, Đặc biệt 8 = `hat` + `variant54`..`60`, không có khăn). Mua hết = **14.850 Xu**.
- 8 màu nền (hex): `d4d4d4` (miễn phí), Thường `b9c6d6` `b8d4be` `e3d5b8` `e8c4c4` `cfc4e3`, Hiếm `a9d3dc` `ebc4a0`. Phẳng, tương phản với nét đen ≥ 11:1, không dùng vàng `accent`.

### 5. API ([contracts/rest-api.md](contracts/rest-api.md))

| Endpoint | Auth | Kết quả |
|---|---|---|
| `GET /api/shop/items` | công khai | 200 `AvatarItemDto[]` (cache 1 giờ) |
| `POST /api/shop/purchases` `{itemKey}` | JWT | 201 `{itemKey, price, coinBalance}`; 400 `validation_failed`/`item_not_purchasable`; 404 `item_not_found`; **409 `already_owned`**; **409 `insufficient_coins`** |
| `GET /api/users/me/avatar` | JWT | 200 `{avatar, isDefault, ownedItemKeys}` |
| `PUT /api/users/me/avatar` (thay toàn bộ) | JWT | 200 `{avatar, isDefault:false}`; 400 `validation_failed`/`unknown_variant`/**`graphic_incompatible_clothes`**; 409 `item_not_owned` |
| `GET /api/leaderboards/xp` | công khai | thêm `avatar` mỗi dòng |

Lỗi theo ProblemDetails + trường mở rộng `code` (máy đọc được); `GlobalExceptionHandler` ghi `code` chung cho ngoại lệ có `Code`. Nhân vật **tách khỏi** `GET /api/users/me` (giữ `ProfileDto` nguyên, không đổi hợp đồng cũ).

### 6. Đồng thời ([research R7](research.md))

**Trình tự mua**: `BEGIN` → `SELECT … FROM "Users" WHERE "Id" = @id FOR UPDATE` → tra món (404/400) → kiểm sở hữu (409) → kiểm số dư (409) → trừ `CoinBalance` + thêm sở hữu + thêm `CoinTransactions` → `COMMIT`; vi phạm unique `(UserId, ItemKey)` ánh xạ về `already_owned` (lưới an toàn).

**Không deadlock với kết thúc trận**: kết thúc trận khóa `Room` rồi các `Users` theo `ORDER BY Id`; mua đồ chỉ giữ **một** khóa `Users` và không chờ khóa nào khác trong lúc giữ ⇒ không thể thuộc chu trình chờ. Hai giao dịch cùng tài khoản (mua với mua, mua với kết thúc trận) tuần tự hoá ở khóa dòng `Users` ⇒ không mất cập nhật.

**Test tích hợp đồng thời** (Postgres thật, giống `RewardSettlementTests`, lặp nhiều vòng, `WaitAsync(30s)` để bắt deadlock):

| Test | Khẳng định |
|---|---|
| `Purchase_SameItemTwiceSequentially_SecondIsAlreadyOwned` | lần 2 `409 already_owned`; Xu trừ một lần |
| `Purchase_SameItemConcurrently_ChargedOnce` (8 request × 10 vòng) | đúng 1 thành công, còn lại `already_owned`; `CoinBalance = đầu − giá`; 1 sở hữu; 1 dòng sổ cái |
| `Purchase_TwoItemsConcurrently_CoinsForOnlyOne_NeverNegative` (60 Xu, hai món 50 Xu × 10 vòng) | đúng 1 thành công, 1 `insufficient_coins`; số dư 10, không âm |
| `Purchase_ConcurrentWithMatchEnd_NoLostUpdate` (× 8 vòng) | số dư cuối = đầu + thưởng − giá; không deadlock; sổ cái khớp |
| `Purchase_InsufficientCoins_BalanceAndOwnershipUnchanged` | từ chối không đổi gì |
| `Invariant_BalanceEqualsLedger_AfterMixedOperations` | `CoinBalance = SUM(Amount)` mọi người dùng |
| `SaveAvatar_ConcurrentFirstSaves_BothSucceed` | PUT đầu tiên đồng thời không lỗi 500 |

**Kiểm độ nhạy (tạm bỏ `FOR UPDATE` thì test phải fail)**: repository có hai điểm tiêm test-only `internal` (đặt `InternalsVisibleTo` cho dự án test): cờ `UseUserRowLock` (mặc định `true`) và móc `BeforeBalanceWrite` (một `Barrier` trong test). Test `Purchase_WithoutUserRowLock_LosesUpdate_ProvingTestsAreSensitive` tắt khóa và dùng barrier ép hai giao dịch đọc số dư rồi mới ghi; **khẳng định bất biến bị phá** (`CoinBalance != SUM(Amount)` hoặc sở hữu trùng) ⇒ chứng minh rằng `Purchase_TwoItemsConcurrently_…` (chạy khóa thật) sẽ bắt được lỗi nếu ai bỏ khóa. Hướng dẫn kiểm tay thêm ở quickstart (comment dòng `FOR UPDATE`, test đồng thời phải đỏ, hoàn nguyên).

### 7. Hiển thị trong phòng một truy vấn ([research R6](research.md), [signalr-events.md](contracts/signalr-events.md))

`LoadSnapshotAsync` đã nạp `Users` của cả phòng bằng một truy vấn; thêm đúng một truy vấn `AsNoTracking` `UserAvatars WHERE UserId = ANY(userIds)` vào `GameSnapshot.Avatars`. `GameService.AvatarOf` (đã lưu → mặc định tài khoản → mặc định khách) dùng cho `RoomState.players`, mọi `LeaderboardEntryDto` (cả `LeaderboardUpdated` và `GameEnded`) và `PlayerJoined`. Không lưu vào `Players` (đúng "nhân vật hiện tại"). Bảng xếp hạng XP: một câu `LEFT JOIN`. Test đếm số lệnh SQL (interceptor) cho phòng 2 và 50 người: bằng nhau. Số đo thật ở Đợt C: `FindSnapshotByCodeAsync` từ 5 lên 6 lệnh SQL (cả phòng 2 và 50 người); payload một `AvatarDto` 210 đến 245 byte ⇒ phòng 50 người `RoomState` +22 đến +23,7 KB (tổng 35 đến 36,7 KB), `LeaderboardUpdated`/`GameEnded` +11,8 KB.

### 8. Migration ([data-model.md mục 6](data-model.md), [research R8](research.md))

- Tên: **`AddAvatarShop`** (một migration).
- Seed danh mục bằng `HasData` từ `AvatarCatalog` (201 `InsertData`).
- Kiểm trên **DB riêng** trước: tạo DB mới, migrate tới `AddXpCoinsAndPlayerAccounts`, nạp mẫu, chạy `scripts/migrate-prod.ps1`, áp `psql` hai lần, kiểm dữ liệu cũ + 201 dòng + CHECK + RLS; không chạy trên DB dev hiện tại.
- **RLS**: production đã bật RLS cho cả 8 bảng hiện có (cố vấn đã kiểm); README lỗi thời sẽ được sửa ở Đợt A. `ALTER TABLE … ENABLE ROW LEVEL SECURITY` cho `AvatarItems`, `UserAvatars`, `UserAvatarItems` trong chính migration; không tạo policy (anon/authenticated bị chặn, backend dùng vai trò chủ bảng nên không ảnh hưởng); có test `pg_class.relrowsecurity`.
- Production: chạy `scripts/migrate-prod.ps1` **trước khi merge**; migration chỉ thêm nên backend cũ vẫn chạy được.

### 9. Chia Đợt (mỗi Đợt: `dotnet build` + `dotnet test` + (frontend) `npm run build`/`test`/`lint` xanh, rồi push để cố vấn đọc diff)

| Đợt | Phạm vi | Cổng ra |
|---|---|---|
| **A: Domain và dữ liệu** | `AvatarSlot/Tier`, `AvatarCatalog` (201 món), `DefaultAvatarGenerator`, `AvatarRules` + unit test; thực thể và cấu hình EF; migration `AddAvatarShop` (+RLS); `GET /api/shop/items`; test đồng bộ với file style (xem lưu ý dưới bảng); hằng `PrintableClothesVariantKeys` và luật `graphic_incompatible_clothes` trong `AvatarRules` + test; **sửa dòng README về RLS cho đúng hiện trạng** (production đã bật RLS cho 8 bảng); kiểm migration trên DB riêng | build + test xanh; migration kiểm xong; danh mục được duyệt |
| **B: Mua đồ và lưu nhân vật (backend)** | `IShopService`/`ShopService`, `IAvatarService`/`AvatarService`, `AvatarRepository`, `Code` trên ngoại lệ + `GlobalExceptionHandler`, `POST /api/shop/purchases`, `GET/PUT /api/users/me/avatar`; unit test (lớp giả) và toàn bộ test đồng thời + kiểm độ nhạy | build + test xanh (cả tích hợp có `QUIZBATTLE_TEST_DB`) |
| **C: Nhân vật vào game (backend)** | `GameSnapshot.Avatars`, `AvatarOf`, thêm `avatar` vào DTO phòng và `XpLeaderboardEntryDto`, join ở `UserRepository`; sửa test hiện có theo chữ ký DTO; test đếm truy vấn | build + test xanh |
| **D: Frontend nền** | `npm install --save-exact` hai gói, `.npmrc`, `THIRD_PARTY_NOTICES.md`, cập nhật CLAUDE.md (Stack + cấu trúc), `src/avatar/`, kiểu `types/`, `src/api/`, `Avatar`, trình chỉnh ở `/profile` (vô hiệu hóa hình in khi áo không in được); test Vitest gồm test áo in được so với SVG đúng phiên bản ghim; **báo cáo kích thước bundle chính trước và sau** (`vite build`) và kích thước chunk `notionists` | `npm run build`, `test`, `lint`, `typecheck` xanh |
| **E: Cửa hàng và hiển thị** | `/shop` (+ liên kết điều hướng), nhân vật nhỏ ở sảnh chờ, bảng điểm giữa/cuối trận và `/ranking`, ánh xạ `code` lỗi; lint/inspect giao diện (nếu có skill `design-taste-frontend`); chạy quickstart; kiểm migration production | mọi build/test xanh; quickstart đạt; sẵn sàng chạy `migrate-prod.ps1` rồi merge |

**Lưu ý Đợt A**: test đồng bộ cần `@dicebear/styles@10.6.0` trong `frontend/node_modules`, mà cài gói là việc của Đợt D. Để Đợt A tự đủ, **đưa riêng việc `npm install --save-exact` hai gói (và `.npmrc`) lên đầu Đợt A** (chỉ thay đổi `package.json`/lock, chưa viết code frontend); Đợt D còn lại phần code. Thay đổi này là hệ quả của yêu cầu "test so với file style đúng phiên bản đã ghim".

## Quyết định đã duyệt (cố vấn)

| # | Vấn đề | Quyết định |
|---|---|---|
| **Q-A** | Hình in chỉ hiện trên áo `variant01`..`variant10` (R1-a) | **Server từ chối** lưu nhân vật có `clothesGraphic` khi áo ngoài tập áo in được: `400`, `code: "graphic_incompatible_clothes"` (FR-028 mới trong spec). Tập áo in được là hằng `AvatarCatalog.PrintableClothesVariantKeys`, kiểm bởi `AvatarRules` (hàm thuần, có test). Giao diện cũng chặn: vô hiệu hóa danh sách hình in kèm lý do; xem thử trong cửa hàng tự đổi sang áo in được. Test Vitest ở frontend, với đúng phiên bản DiceBear đã ghim, vẽ từng áo có và không có hình in và xác nhận tập áo cho SVG khác nhau đúng bằng hằng số (nâng cấp DiceBear không làm hằng lệch) |
| **Q-B** | Mã lỗi | Giữ `409` cho `insufficient_coins` và `item_not_owned`; `400` cho `item_not_purchasable`, `unknown_variant`, `graphic_incompatible_clothes` |
| **Q-C** | Danh sách món miễn phí | Cố vấn chọn: tóc `variant01, 02, 05, 08, 20, 23, 41, 45` + khăn trùm đầu `61, 62, 63` (luôn miễn phí, tên trung tính); áo `variant01, 02, 07, 14, 23`. Mức giá phần còn lại phân lại cùng nguyên tắc cũ (data-model 5.2). Khăn trùm đầu là trang phục tôn giáo nên không bao giờ được bán (FR-003) |
| **Q-D** | RLS bảng cũ | Tiền đề sai: production đã bật RLS cho cả 8 bảng hiện có. **Bỏ** đề xuất chore RLS; giữ 3 lệnh `ENABLE ROW LEVEL SECURITY` trong `AddAvatarShop`; sửa dòng README ở Đợt A |
| **Q-E** | Kích thước bundle | `notionists.min.json` (≈ 372 KB, ≈ 111 KB gzip) nạp bằng `import()` động, tách chunk khỏi bundle chính; Đợt D báo cáo kích thước bundle chính trước và sau |

## Complexity Tracking

Không có vi phạm Constitution cần biện minh. Các điểm có chủ đích tăng độ phức tạp: bảng `UserAvatars` riêng (thay vì cột trên `Users`) để không mở rộng bảng bị khóa nóng; hai điểm tiêm test-only trong repository phục vụ yêu cầu kiểm độ nhạy.
