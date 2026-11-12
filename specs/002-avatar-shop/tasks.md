---

description: "Danh sách task triển khai: Nhân vật và cửa hàng"
---

# Tasks: Nhân vật và cửa hàng

**Input**: [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/](contracts/), [quickstart.md](quickstart.md)

**Prerequisites**: plan.md, spec.md (đã có và đã được cố vấn duyệt). Nhánh `feat/avatar-shop`.

**Tests**: CÓ. Với mọi task logic (Domain, `AvatarRules`, `DefaultAvatarGenerator`, mua đồ đồng thời) task test đứng TRƯỚC task mã: viết test, chạy thấy FAIL (hoặc không biên dịch vì thiếu kiểu), rồi mới viết mã.

**Organization**: gom theo **Đợt A-E** đúng như [plan.md](plan.md) (điểm 9), cộng **Đợt F** (chốt trước merge). Mỗi task có nhãn user story `[US1]..[US4]` khi phục vụ trực tiếp một story trong spec. Mỗi Đợt kết thúc bằng một task **Cổng ra** rồi một task **Push** để cố vấn đọc diff. CLAUDE.md thắng khi mâu thuẫn.

## Format: `- [ ] [ID] [P?] [Story?] Mô tả kèm đường dẫn file`

- **[P]**: về file là độc lập (khác file, không phụ thuộc task chưa xong). Người làm vẫn chạy **tuần tự** trong một phiên; [P] chỉ để biết thứ tự có thể đảo.
- Đường dẫn tương đối từ gốc repo; backend ở `backend/`, frontend ở `frontend/`.
- Môi trường dev: `docker compose up -d` (PostgreSQL cổng 5433); test tích hợp cần `QUIZBATTLE_TEST_DB` (chuỗi kết nối admin có quyền `CREATEDB`).

## Quy ước "Cổng ra" (áp dụng cho mọi Đợt)

Mỗi Đợt chỉ coi là xong khi các lệnh dưới (chỉ những lệnh thuộc phạm vi Đợt đó) đều đạt, **kèm số liệu ghi vào dòng kết quả của task Cổng ra**:

| Lệnh | Đạt khi | Ghi lại |
|---|---|---|
| `dotnet build backend/QuizBattle.sln` | `0 Warning(s)`, `0 Error(s)` | số cảnh báo, số lỗi |
| `dotnet test backend/QuizBattle.sln` (có `QUIZBATTLE_TEST_DB`) | `Failed: 0` và **`Skipped: 0`** | **số test đã chạy** (Passed), **số bị bỏ qua** (Skipped), số tổng; so với đợt trước để thấy số tăng |
| `npm --prefix frontend test` (Vitest) | 0 fail | số file test, số test |
| `npm --prefix frontend run lint` | 0 lỗi, 0 cảnh báo | - |
| `npm --prefix frontend run typecheck` | 0 lỗi | - |
| `npm --prefix frontend run build` (vite build) | thành công | kích thước bundle chính (Đợt D, E) |

Không pipe lệnh build/test qua `tail`/`head`/`Select-Object` khi nối với `git push` bằng `&&`; kiểm tra mã thoát (`$LASTEXITCODE`) trước khi push (CLAUDE.md). Nếu đặt `QUIZBATTLE_TEST_DB` mà vẫn có test bị bỏ qua thì tìm nguyên nhân, không chấp nhận.

---

## Đợt A: Domain và dữ liệu

**Mục tiêu**: gói DiceBear ghim cứng; danh mục 201 món và các hàm thuần ở Domain; schema + migration `AddAvatarShop` (seed + RLS); `GET /api/shop/items`; test đồng bộ danh mục với file style; README đúng hiện trạng RLS; migration kiểm trên DB riêng hai lần.

### Ghim thư viện (bắt đầu Đợt A)

- [x] T001 Trong `frontend/`: tạo `frontend/.npmrc` với đúng một dòng `save-exact=true`, rồi chạy `npm install --save-exact @dicebear/core@10.6.0 @dicebear/styles@10.6.0` (commit cả `package-lock.json`). Chưa import gói ở đâu, chưa viết code frontend. Commit riêng, message: `chore: ghim chính xác DiceBear core và styles 10.6.0` kèm thân commit nêu **lý do `.npmrc` `save-exact`**: tên variant (ví dụ `hair.variant12`) là dữ liệu lưu trong DB; `^`/`~` có thể kéo bản mới làm đổi hình hoặc đổi tên variant mà không ai hay, nên mọi lần `npm install <gói>` sau này cũng phải ghi phiên bản chính xác (lưu ý: `.npmrc` chỉ đổi mặc định cho lần cài sau, không sửa các phạm vi `^` đã có của gói khác)
- [x] T002 Kiểm `frontend/package.json` bằng lệnh (ghi kết quả vào task này): `node -e "const p=require('./frontend/package.json');for(const n of ['@dicebear/core','@dicebear/styles']){const v=p.dependencies[n];if(v!=='10.6.0')throw new Error(n+' = '+v)}"` phải qua (đúng `"10.6.0"`, **không** có `^`/`~`); `npm --prefix frontend ls @dicebear/core @dicebear/styles` hiện đúng `10.6.0` cho cả hai; `npm --prefix frontend run build` vẫn qua (chưa import nên bundle không đổi)

### Domain thuần (test trước)

- [x] T003 [P] Tạo `backend/QuizBattle.Domain/Enums/AvatarSlot.cs` (`Eyes, Eyebrows, Mouth, Nose, Hair, Clothes, Glasses, Beard, Gesture, ClothesGraphic, Background`) và `AvatarTier.cs` (`Free, Common, Rare, Special`), cộng hàm chuyển tên ô sang chuỗi camelCase (`eyes, eyebrows, mouth, nose, hair, clothes, glasses, beard, gesture, clothesGraphic, background`) và ngược lại, dùng cho `Key`, JSON và DiceBear ([data-model.md](data-model.md) mục 1)
- [x] T004 [P] [US1] Viết test (FAIL trước) `backend/QuizBattle.Tests/Domain/AvatarCatalogTests.cs`: `Items_Total_Is201`; `Items_FreeCount_Is85_PaidCount_Is116`; `Items_TierCounts_Common53_Rare52_Special11`; số kiểu mỗi ô (mắt 5, lông mày 13, miệng 30, mũi 20, tóc 64, áo 25, kính 11, râu 12, cử chỉ 10, hình in 3, nền 8); `Keys_AreUnique_AndEqualSlotDotVariantKey`; `FreeItems_MatchApprovedList` (mắt/lông mày/miệng/mũi toàn bộ; tóc `variant01, 02, 05, 08, 20, 23, 41, 45, 61, 62, 63`; áo `variant01, 02, 07, 14, 23`; nền `d4d4d4`); `HeadscarfHair_61_62_63_AreFreeAndNeutrallyNamed` (tên đúng "Khăn quấn đầu", "Khăn trùm đầu 1", "Khăn trùm đầu 2", `Tier = Free`); `Gesture_AllRare_150`; `ClothesGraphic_AllSpecial_400`; `Prices_Are50_150_400_AndFreeIsZero`; `PaidHair_Tiers_Common24_Rare21_Special8_SpecialIsHatAndVariant54To60`; `PaidClothes_Common12_Rare8`; `Backgrounds_HexMatchesLowercase6Hex_5Common2RareOneFree`; `TotalPriceOfAllPaidItems_Is14850`; `PrintableClothes_IsVariant01To10`; `StyleVersion_Is10_6_0`; `DefaultBackgroundHex_Is_d4d4d4`
- [x] T005 Tạo `backend/QuizBattle.Domain/Avatars/AvatarItemDefinition.cs` (record: `Slot`, `VariantKey`, `Name`, `Tier`, `Price`, `SortOrder`; thuộc tính tính `Key` = `"<ô>.<variantKey>"`) và `backend/QuizBattle.Domain/Avatars/AvatarCatalog.cs` (static: `StyleVersion = "10.6.0"`, `DefaultBackgroundHex = "d4d4d4"`, `RequiredSlots`, `PrintableClothesVariantKeys` = `variant01`..`variant10`, `PriceOf(tier)` = 0/50/150/400, `Items` đủ 201 món theo bảng [data-model.md](data-model.md) mục 5.1-5.3; tên tạm "Tóc 09", "Áo 06"... giữ nguyên, ba khăn trùm đầu tên trung tính, tên màu nền theo bảng 5.3). Làm T004 xanh
- [x] T006 [P] [US2] Viết test (FAIL trước) `backend/QuizBattle.Tests/Domain/AvatarRulesTests.cs`: `Validate_AllRequiredFreeItems_NoErrors`; `Validate_MissingRequiredSlot_ReturnsValidationFailedForThatSlot` (đủ 6 ô, lần lượt thiếu từng ô); `Validate_UnknownVariantInSlot_ReturnsUnknownVariant`; `Validate_OptionalSlotsNull_NoErrors`; `Validate_VariantOfAnotherSlot_ReturnsUnknownVariant` (ví dụ `eyes` = `variant12` hợp lệ về chuỗi nhưng đưa `glasses` = `ok` thì lỗi); `Validate_GraphicOnClothesVariant10_NoErrors`; `Validate_GraphicOnClothesVariant11_ReturnsGraphicIncompatibleClothes`; `Validate_GraphicOnFreeClothesVariant14_ReturnsGraphicIncompatibleClothes`; `Validate_NoGraphic_AnyClothesOk`; `Validate_GraphicCheckRunsAfterSlotChecks` (lỗi ô đến trước lỗi tương thích); `Validate_ReturnsErrorsInFixedSlotOrder`
- [x] T007 Tạo `backend/QuizBattle.Domain/Avatars/AvatarSelection.cs` (record bản đồ ô → `variantKey?`, đủ 11 ô) và `backend/QuizBattle.Domain/Avatars/AvatarRules.cs` (static thuần: `Validate(selection)` trả danh sách lỗi `{ Slot, Code }` với `Code` ∈ `validation_failed`, `unknown_variant`, `graphic_incompatible_clothes`; không phụ thuộc DB). Làm T006 xanh
- [x] T008 [P] [US2] Viết test (FAIL trước) `backend/QuizBattle.Tests/Domain/DefaultAvatarGeneratorTests.cs`: `ForAccount_SameUserId_ReturnsSameAvatar` (gọi nhiều lần; hai `Guid` bằng giá trị); `ForAccount_OnlyUsesFreeItems`; `ForAccount_HasSixRequiredSlots_OptionalSlotsEmpty_DefaultBackground`; `ForGuest_SameNameDifferentCaseAndPadding_ReturnsSameAvatar`; `ForGuest_OnlyUsesFreeItems_GreyBackground`; `ForGuest_DifferentNames_CanDiffer`; `ForAccount_GoldenValues` (3 `Guid` cố định và 3 tên khách cố định; **giá trị golden được ghi vào test sau khi chạy lần đầu và duyệt bằng mắt; nếu hash hoặc danh sách miễn phí đổi thì test fail vì nhân vật mặc định của mọi người đổi hình**); `ForAccount_TenThousandIds_AllValidAndWellDistributed` (mọi kết quả qua `AvatarRules.Validate` không lỗi; mỗi ô có ≥ 90% số kiểu miễn phí xuất hiện); `Hash_DoesNotUseStringGetHashCode` (kết quả golden bất biến giữa các lần chạy test chính là bằng chứng; thêm ghi chú trong test)
- [x] T009 Tạo `backend/QuizBattle.Domain/Avatars/DefaultAvatarGenerator.cs` (static, thuần): `ForAccount(Guid)` khóa `"u:" + userId.ToString("N")`, `ForGuest(string)` khóa `"g:" + displayName.Trim().ToLowerInvariant()`; băm **FNV-1a 64 bit** trên UTF-8; ô thứ `i` (thứ tự `eyes, eyebrows, mouth, nose, hair, clothes`) chọn `free[slot][SplitMix64(hash + (i+1) * 0x9E3779B97F4A7C15) % count]` từ món `Free` của `AvatarCatalog`; ô tùy chọn trống; nền = `DefaultBackgroundHex`. **Tuyệt đối không dùng `string.GetHashCode`**. Làm T008 xanh (ghi golden)

### Entities, EF, migration

- [ ] T010 [P] Tạo `backend/QuizBattle.Domain/Entities/AvatarItem.cs` (`Key`, `Slot`, `VariantKey`, `Name`, `Tier`, `Price`, `SortOrder`), `UserAvatarItem.cs` (`Id`, `UserId`, `ItemKey`, `AcquiredAt`), `UserAvatar.cs` (`UserId`; 11 thuộc tính `…ItemKey`: sáu ô bắt buộc `string` không null, năm ô tùy chọn `string?`; `UpdatedAt`); sửa `backend/QuizBattle.Domain/Entities/CoinTransaction.cs`: thêm `UserAvatarItemId` (`Guid?`) và sửa chú thích "mua đồ ở tính năng sau" thành mô tả đã dùng; sửa chú thích `backend/QuizBattle.Domain/Enums/CoinTransactionReason.cs` (`ShopPurchase` đã dùng)
- [ ] T011 Tạo ba cấu hình EF trong `backend/QuizBattle.Infrastructure/Persistence/Configurations/`, theo [data-model.md](data-model.md) mục 2 (trích nguyên văn ràng buộc): `AvatarItemConfiguration.cs`: bảng `AvatarItems`, PK `Key` (text), `Slot` và `Tier` lưu `text`, `Name` `varchar(60) NOT NULL`, `Price int NOT NULL`, `UNIQUE (Slot, VariantKey)`, `CK_AvatarItems_Key` `"Key" = "Slot" || '.' || "VariantKey"`, `CK_AvatarItems_Price` `("Tier" = 'Free' AND "Price" = 0) OR ("Tier" <> 'Free' AND "Price" > 0)`, **`HasData` seed từ `AvatarCatalog.Items` (201 dòng)**; `UserAvatarItemConfiguration.cs`: bảng `UserAvatarItems`, FK `UserId` → `Users` `ON DELETE CASCADE`, FK `ItemKey` → `AvatarItems` `ON DELETE RESTRICT`, `AcquiredAt timestamptz`, **`UNIQUE (UserId, ItemKey)`** tên `IX_UserAvatarItems_UserId_ItemKey`; `UserAvatarConfiguration.cs`: bảng `UserAvatars`, PK và FK `UserId` → `Users` `ON DELETE CASCADE`, sáu cột bắt buộc `NOT NULL`, năm cột tùy chọn nullable, mỗi cột FK → `AvatarItems(Key)`, `CHECK` theo ô (ví dụ `"HairItemKey" LIKE 'hair.%'`; ô tùy chọn `… IS NULL OR … LIKE '<ô>.%'`), `UpdatedAt timestamptz`
- [ ] T012 Sửa `backend/QuizBattle.Infrastructure/Persistence/Configurations/CoinTransactionConfiguration.cs`: FK `UserAvatarItemId` → `UserAvatarItems(Id)` `ON DELETE NO ACTION`; index duy nhất có lọc `IX_CoinTransactions_UserAvatarItemId` `WHERE "UserAvatarItemId" IS NOT NULL`; `CHECK` tên `CK_CoinTransactions_ReasonShape`: `("Reason" = 'MatchReward' AND "Amount" > 0 AND "UserAvatarItemId" IS NULL) OR ("Reason" = 'ShopPurchase' AND "Amount" < 0 AND "UserAvatarItemId" IS NOT NULL)`; thêm `DbSet<AvatarItem>`, `DbSet<UserAvatarItem>`, `DbSet<UserAvatar>` vào `backend/QuizBattle.Infrastructure/Persistence/AppDbContext.cs`
- [ ] T013 Sinh migration `AddAvatarShop`: `dotnet ef migrations add AddAvatarShop --project backend/QuizBattle.Infrastructure --startup-project backend/QuizBattle.Api`; mở file migration và **thêm ba lệnh** `migrationBuilder.Sql("ALTER TABLE \"AvatarItems\" ENABLE ROW LEVEL SECURITY;")` (tương tự `UserAvatars`, `UserAvatarItems`; không tạo policy, không `FORCE`); kiểm tay: có `CreateTable` cho ba bảng, `InsertData` đủ 201 dòng, thêm cột/FK/index/CHECK trên `CoinTransactions`, `Down` đảo ngược đúng; `dotnet build` 0 warning
- [ ] T014 [P] [US1] Viết test tích hợp `backend/QuizBattle.Tests/Integration/AvatarSchemaTests.cs` (`[RequiresDatabaseFact]`, dùng `DatabaseFixture`): `Migration_Seeds201Items_85Free`; `UserAvatarItems_DuplicateUserAndItem_ViolatesUnique`; `UserAvatars_ItemKeyOfWrongSlot_ViolatesCheck` (ví dụ `HairItemKey = 'eyes.variant01'`); `UserAvatars_UnknownItemKey_ViolatesForeignKey`; `UserAvatars_RequiredSlotNull_Rejected`; `AvatarItems_FreeWithPositivePrice_ViolatesCheck`; `CoinTransactions_ShopPurchaseWithoutItem_ViolatesReasonShape`; `CoinTransactions_ShopPurchasePositiveAmount_Violates`; `CoinTransactions_MatchRewardWithItem_Violates`; `CoinTransactions_ExistingMatchRewardRows_StillValid`; `NewTables_HaveRowLevelSecurityEnabled` (truy vấn `pg_class.relrowsecurity = true` cho `AvatarItems`, `UserAvatars`, `UserAvatarItems`)

### Danh mục công khai và test đồng bộ style

- [ ] T015 [P] [US1] Viết test (FAIL trước) `backend/QuizBattle.Tests/Shop/ShopServiceCatalogTests.cs` (lớp giả `FakeAvatarRepository` tự viết trong `backend/QuizBattle.Tests/TestHelpers/`): `GetCatalog_ReturnsAllItemsOrderedBySlotThenSortOrder`; `GetCatalog_TierIsLowercaseString_PriceFromRepository`
- [ ] T016 [US1] Tạo `backend/QuizBattle.Application/Shop/` : `AvatarItemDto.cs` (`Key, Slot, VariantKey, Name, Tier, Price`; `Tier` chữ thường `free|common|rare|special`), `IShopService.cs` (`GetCatalogAsync`), `ShopService.cs`, `backend/QuizBattle.Application/Shop/IAvatarRepository.cs` (`GetCatalogAsync`), `backend/QuizBattle.Infrastructure/Persistence/AvatarRepository.cs` (`AsNoTracking`, sắp theo ô rồi `SortOrder`), đăng ký DI ở `DependencyInjection.cs` của Application và Infrastructure; tạo `backend/QuizBattle.Api/Controllers/ShopController.cs` với `GET /api/shop/items` (`[AllowAnonymous]`, `Cache-Control: public, max-age=3600`, `ProducesResponseType<IReadOnlyList<AvatarItemDto>>`). Làm T015 xanh
- [ ] T017 [US1] Viết `backend/QuizBattle.Tests/Domain/AvatarCatalogStyleSyncTests.cs`: mở `frontend/node_modules/@dicebear/styles/package.json` và `dist/notionists.min.json` (tìm đường dẫn từ vị trí solution lên gốc repo); kiểm: `StyleVersion_MatchesInstalledPackage` (version trong package.json == `AvatarCatalog.StyleVersion` == `"10.6.0"`); `EveryDiceBearSlot_VariantKeysEqualStyleVariants_BothDirections` (mọi ô trừ `background`: tập `variantKey` trong danh mục == tập khóa `components.<ô>.variants`; không thiếu, không thừa); `SlotNames_ExistInStyleComponents` (kể cả `clothesGraphic`); `PrintableClothes_AreSubsetOfStyleClothes`; `Backgrounds_VariantKeysAreLowercase6Hex`. Thiếu `frontend/node_modules` thì **fail** với thông báo "chạy `npm ci` trong frontend", trừ khi đặt `QB_SKIP_STYLE_SYNC=1` (in cảnh báo rõ)

### README và kiểm migration

- [ ] T018 [P] Sửa `README.md` mục Deploy bước 1 (khoảng dòng 177): bỏ ý "các bảng trong schema `public` chưa bật RLS nên nếu Data API bật thì ai có anon key cũng đọc/ghi được"; thay bằng đúng hiện trạng: production Supabase đã bật RLS cho cả 8 bảng hiện có (đã kiểm ngày 07/11 và 10/11) và bảng mới của `AddAvatarShop` bật RLS trong chính migration; vẫn khuyến nghị tắt Data API vì backend không dùng. Không sửa phần khác của README
- [ ] T019 Kiểm migration trên **DB riêng, lần 1** (không dùng DB dev hiện tại; theo mẫu T089 của Tính năng 1): tạo DB mới `qb_migcheck_avatar` trên Postgres cổng 5433; migrate tới `AddXpCoinsAndPlayerAccounts` (migration ngay trước) bằng `dotnet ef database update AddXpCoinsAndPlayerAccounts` với `ConnectionStrings__Default` trỏ DB đó; nạp dữ liệu mẫu (1 User, 1 `CoinTransaction` `MatchReward` `Amount = 10` kèm `CoinBalance = 10`); chạy `./scripts/migrate-prod.ps1` (sinh `scripts/out/migrate-prod.sql`, ghi **kích thước và SHA-256**); áp bằng `psql ... --set ON_ERROR_STOP=1 --file scripts/out/migrate-prod.sql`; kiểm: dữ liệu mẫu nguyên vẹn, `count(*) FROM "AvatarItems"` = 201 và `Free` = 85, `CK_CoinTransactions_ReasonShape` có và dòng mẫu cũ vẫn hợp lệ, `relrowsecurity = true` cho ba bảng, thử vi phạm (`INSERT` sở hữu trùng, `UserAvatars` sai ô, `ShopPurchase` dương) bị từ chối. Ghi kết quả vào task này
- [ ] T020 Kiểm migration trên DB riêng, **lần 2 (idempotent)**: áp **lại đúng file SQL đó** bằng `psql` lên cùng DB `qb_migcheck_avatar`; phải exit 0, không có `ERROR`; `AvatarItems` vẫn 201 dòng (không nhân đôi), dữ liệu mẫu không đổi; sau đó `DROP DATABASE qb_migcheck_avatar`. Ghi kết quả vào task này

### Cổng ra và push Đợt A

- [ ] T021 **Cổng ra Đợt A**: chạy theo bảng "Quy ước Cổng ra" cho backend và frontend (`build`, `typecheck`, `lint`, `test` cả hai); ghi vào đây: build `0 Warning(s)`; `dotnet test` **Passed = ?, Skipped = 0, Failed = 0** (với `QUIZBATTLE_TEST_DB`); Vitest số file/test; xác nhận `AvatarCatalogStyleSyncTests` chạy thật (không skip); `git status` sạch trừ thay đổi dự kiến
- [ ] T022 **Push Đợt A**: `git push` nhánh `feat/avatar-shop` (kiểm mã thoát lệnh build/test trước); báo cố vấn commit hash, danh sách file, các con số ở T021, kết quả T019-T020; **chờ cố vấn đọc diff** (đặc biệt `AvatarCatalog` và migration) trước khi sang Đợt B

**Checkpoint**: danh mục, mặc định, luật hợp lệ, schema và `GET /api/shop/items` xong và có test; chưa có mua đồ.

---

## Đợt B: Mua đồ và lưu nhân vật (backend)

**Mục tiêu**: `POST /api/shop/purchases`, `GET/PUT /api/users/me/avatar` đúng [contracts/rest-api.md](contracts/rest-api.md); mã lỗi máy đọc được; toàn bộ test đồng thời và test kiểm độ nhạy.

### Mã lỗi `code` dùng chung

- [ ] T023 [P] [US1] Viết test (FAIL trước) bổ sung vào `backend/QuizBattle.Tests/Api/GlobalExceptionHandlerTests.cs`: `ConflictException_WithCode_WritesCodeExtension`; `NotFoundException_WithCode_WritesCodeExtension`; `RequestValidationException_WithCode_WritesCodeAndErrors`; `ConflictException_WithoutCode_HasNoCodeExtension` (giữ hành vi cũ); `UnauthorizedException_Code_Unchanged` (không đổi `token_expired`/`token_invalid`/`session_replaced`)
- [ ] T024 [US1] Thêm thuộc tính `Code` (chuỗi, tùy chọn) vào `ConflictException`, `NotFoundException`, `RequestValidationException` trong `backend/QuizBattle.Application/Common/` (hàm tạo cũ vẫn hợp lệ); sửa `backend/QuizBattle.Api/Middleware/GlobalExceptionHandler.cs` ghi `Extensions["code"]` cho mọi ngoại lệ có `Code` (mã 401 cũ giữ nguyên). Làm T023 xanh

### Mua đồ (test trước)

- [ ] T025 [US1] Tạo khung kiểu cho Shop trong `backend/QuizBattle.Application/Shop/`: `PurchaseRequest.cs` (`ItemKey`), `PurchaseResultDto.cs` (`ItemKey, Price, CoinBalance`), mở rộng `IShopService` (`PurchaseAsync(userId, itemKey)`) và `IAvatarRepository` (`PurchaseAsync(userId, item, cancellationToken)`); chưa cài đặt
- [ ] T026 [P] [US1] Viết test (FAIL trước) `backend/QuizBattle.Tests/Shop/ShopServicePurchaseTests.cs` với `FakeAvatarRepository`: `Purchase_InvalidKeyFormat_ThrowsValidationFailed` (rỗng, thiếu dấu chấm, dài > 80 ký tự); `Purchase_UnknownItem_ThrowsNotFoundItemNotFound`; `Purchase_FreeItem_ThrowsValidationItemNotPurchasable`; `Purchase_AlreadyOwned_ThrowsConflictAlreadyOwned`; `Purchase_InsufficientCoins_ThrowsConflictInsufficientCoins`; `Purchase_OwnedAndPoor_ThrowsAlreadyOwnedNotInsufficient` (kiểm sở hữu trước số dư); `Purchase_Success_ReturnsPriceAndNewBalance`; `Purchase_ClientNeverSuppliesPrice` (giá lấy từ danh mục)
- [ ] T027 [US1] Cài `backend/QuizBattle.Application/Shop/ShopService.cs` `PurchaseAsync`: validate khóa → tra món (404 `item_not_found`; miễn phí → 400 `item_not_purchasable`) → ủy quyền `IAvatarRepository.PurchaseAsync` (kiểm sở hữu `409 already_owned` rồi số dư `409 insufficient_coins` xảy ra **trong transaction**). Làm T026 xanh
- [ ] T028 [US1] Viết test tích hợp (FAIL trước) `backend/QuizBattle.Tests/Integration/ShopPurchaseTests.cs` (`[RequiresDatabaseFact]`, mẫu `RewardSettlementTests`: nhiều `AppDbContext`/kết nối riêng, lặp nhiều vòng, `WaitAsync(30s)` bắt deadlock, sau mỗi test chạy `AssertInvariantsAsync`: `CoinBalance = SUM(CoinTransactions.Amount)` mọi người dùng, không số dư âm): `Purchase_Success_DeductsCoinsAddsOwnershipAndNegativeLedgerRow`; `Purchase_SameItemTwiceSequentially_SecondIsAlreadyOwnedAndChargedOnce`; `Purchase_InsufficientCoins_BalanceAndOwnershipUnchanged`; `Purchase_SameItemConcurrently_ChargedOnce` (8 request × 10 vòng: đúng 1 thành công, còn lại `already_owned`, số dư = đầu − giá, 1 dòng sở hữu, 1 dòng sổ cái); `Purchase_TwoItemsConcurrently_CoinsForOnlyOne_NeverNegative` (60 Xu, hai món 50 Xu × 10 vòng: đúng 1 thành công + 1 `insufficient_coins`, số dư 10); `Purchase_ConcurrentWithMatchEnd_NoLostUpdate` (8 vòng: số dư cuối = đầu + thưởng − giá, không deadlock, sổ cái khớp; dùng cách dựng phòng kết thúc của `RewardSettlementTests`); `Purchase_ConcurrentWithTwoMatchEndsForSameUser_NoLostUpdateNoDeadlock`; `Purchase_FreeOrUnknownItem_DoesNotTouchBalance`
- [ ] T029 [US1] Cài `AvatarRepository.PurchaseAsync` trong `backend/QuizBattle.Infrastructure/Persistence/AvatarRepository.cs`: một transaction; **`SELECT * FROM "Users" WHERE "Id" = {userId} FOR UPDATE`** (khóa một dòng, theo quy tắc khóa của CLAUDE.md); kiểm sở hữu (`409 already_owned`); kiểm số dư (`409 insufficient_coins`); trừ `CoinBalance`; thêm `UserAvatarItem`; thêm `CoinTransaction` (`Amount = -price`, `Reason = ShopPurchase`, `UserAvatarItemId` = id vừa thêm, `CreatedAt` từ `TimeProvider`); `SaveChanges` + commit; bắt `PostgresException 23505` trên `IX_UserAvatarItems_UserId_ItemKey` → `409 already_owned`. Hai **điểm tiêm chỉ dành cho test** (`internal`, kèm `InternalsVisibleTo("QuizBattle.Tests")` ở project Infrastructure): `bool UseUserRowLock = true` và `Func<Task>? BeforeBalanceWrite`. Làm T028 xanh
- [ ] T030 [US1] **Test kiểm độ nhạy khóa `Users`** trong `backend/QuizBattle.Tests/Integration/ShopPurchaseSensitivityTests.cs`: `Purchase_WithoutUserRowLock_LosesUpdate_ProvingConcurrencyTestsAreSensitive`: đặt `UseUserRowLock = false` và `BeforeBalanceWrite` là một `Barrier(2)` để hai giao dịch (hai món khác nhau, cùng người dùng) cùng đọc số dư rồi mới ghi; **khẳng định bất biến bị phá** (`CoinBalance != SUM(Amount)` hoặc số dư không khớp giá đã trả); thêm test đối chứng `Purchase_WithUserRowLock_SameScenarioKeepsInvariant` (khóa bật, bỏ barrier). Ghi vào task này kết quả **kiểm tay**: chú thích tạm câu `FOR UPDATE` trong `PurchaseAsync` → chạy `dotnet test --filter "FullyQualifiedName~ShopPurchase"` → `Purchase_TwoItemsConcurrently_…` hoặc `Purchase_ConcurrentWithMatchEnd_…` phải **đỏ**; hoàn nguyên (`git diff` không còn thay đổi) → xanh. Không commit bản đã chú thích

### Lưu và đọc nhân vật (test trước)

- [ ] T031 [US2] Tạo khung kiểu trong `backend/QuizBattle.Application/Avatars/`: `AvatarDto.cs` (11 ô; sáu ô bắt buộc `string`, năm ô tùy chọn `string?`), `MyAvatarDto.cs` (`Avatar, IsDefault, OwnedItemKeys`), `SaveAvatarRequest.cs` (cùng 11 ô, `[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]`), `IAvatarService.cs` (`GetMyAvatarAsync`, `SaveMyAvatarAsync`), mở rộng `IAvatarRepository` (`GetUserAvatarAsync`, `GetOwnedItemKeysAsync`, `GetItemsByKeysAsync`, `SaveUserAvatarAsync`); chưa cài đặt
- [ ] T032 [P] [US2] Viết test (FAIL trước) `backend/QuizBattle.Tests/Avatars/AvatarServiceTests.cs` với `FakeAvatarRepository`: `GetMyAvatar_NeverSaved_ReturnsDefaultAvatarFromGeneratorAndIsDefaultTrue`; `GetMyAvatar_Saved_ReturnsSavedAndIsDefaultFalse`; `GetMyAvatar_OwnedKeysListPaidItemsOnly`; `Save_AllFreeItems_Succeeds`; `Save_MissingRequiredSlot_Throws400ValidationFailed`; `Save_UnknownVariant_Throws400UnknownVariant`; `Save_PaidItemNotOwned_Throws409ItemNotOwned`; `Save_PaidItemOwned_Succeeds`; `Save_GraphicOnNonPrintableClothes_Throws400GraphicIncompatibleClothes`; `Save_OneBadSlot_RejectsWholeRequestAndSavesNothing`; `Save_OptionalSlotsNull_ClearsThem`; `Save_ErrorsReportedInFixedSlotOrder`
- [ ] T033 [US2] Cài `backend/QuizBattle.Application/Avatars/AvatarService.cs`: `GetMyAvatarAsync` (đã lưu hoặc `DefaultAvatarGenerator.ForAccount`), `SaveMyAvatarAsync` dùng `AvatarRules.Validate` rồi tra món theo khóa `"<ô>.<variantKey>"` từ DB (`GetItemsByKeysAsync`, **một truy vấn**), kiểm món bán phải thuộc `ownedItemKeys` (`409 item_not_owned` kèm `errors.<ô>`), toàn bộ hoặc không gì cả. Làm T032 xanh
- [ ] T034 [US2] Viết test tích hợp (FAIL trước) `backend/QuizBattle.Tests/Integration/AvatarSaveTests.cs` (`[RequiresDatabaseFact]`): `Save_FirstTime_InsertsRow`; `Save_SecondTime_UpdatesSameRow`; `Save_UnownedPaidItem_RejectedAndRowUnchanged`; `Save_ConcurrentFirstSaves_BothSucceedWithoutServerError` (hai PUT đầu tiên đồng thời: không 500; một bên thấy `23505` rồi thử lại dưới dạng cập nhật); `Save_OwnershipSurvivesAcrossSaves`; `Get_AccountCreatedBeforeFeature_ReturnsDefaultWithoutWritingRow`
- [ ] T035 [US2] Cài các hàm lưu/đọc trong `AvatarRepository` (`GetUserAvatarAsync` `AsNoTracking`, `GetOwnedItemKeysAsync` chỉ cột `ItemKey`, `GetItemsByKeysAsync`, `SaveUserAvatarAsync` upsert một `SaveChanges`; bắt `23505` trên PK `UserId` và thử lại một lần dưới dạng cập nhật). Làm T034 xanh

### Endpoint

- [ ] T036 [US1] [US2] Thêm vào `backend/QuizBattle.Api/Controllers/ShopController.cs` `POST /api/shop/purchases` (`[Authorize]`, `201 Created` kèm `Location: /api/users/me/avatar`, trả `PurchaseResultDto`) và vào `backend/QuizBattle.Api/Controllers/UsersController.cs` `GET /api/users/me/avatar` và `PUT /api/users/me/avatar` (trả `MyAvatarDto` / `{ avatar, isDefault: false }`); Controller chỉ nhận request và gọi Service; đăng ký `IAvatarService` ở DI; `ProducesResponseType` đủ 200/201/400/401/404/409. Bổ sung test hợp đồng mã lỗi (theo mẫu `backend/QuizBattle.Tests/Api/`) cho: 400 `validation_failed`, 400 `item_not_purchasable`, 404 `item_not_found`, 409 `already_owned`, 409 `insufficient_coins`, 400 `unknown_variant`, 400 `graphic_incompatible_clothes`, 409 `item_not_owned`, 401 khi không có token, và `PUT` có ô lạ → 400

### Cổng ra và push Đợt B

- [ ] T037 **Cổng ra Đợt B**: `dotnet build` (0 warning), `dotnet test` (ghi Passed/Skipped/Failed; Skipped = 0), frontend vẫn xanh (không đổi). Ghi vào đây số test mới so với Đợt A và kết quả kiểm tay độ nhạy ở T030
- [ ] T038 **Push Đợt B** và báo cố vấn: commit, file, số liệu T037, kết quả độ nhạy; **chờ cố vấn đọc diff** (`AvatarRepository`, `ShopService`, `AvatarService`) trước Đợt C

**Checkpoint**: mua đồ và lưu nhân vật hoàn chỉnh ở backend, an toàn khi đồng thời.

---

## Đợt C: Nhân vật vào game (backend)

**Mục tiêu**: mọi DTO phòng và bảng xếp hạng XP mang `avatar` đủ ô, nạp bằng đúng một truy vấn thêm (không N+1). Đúng [contracts/signalr-events.md](contracts/signalr-events.md).

- [ ] T039 [P] [US3] Viết test (FAIL trước) `backend/QuizBattle.Tests/Game/GameServiceAvatarTests.cs` (lớp giả `FakeGameRepository` có sẵn, mở rộng để có `Avatars`): `JoinAsPlayer_Broadcast_HasAvatar`; `RoomState_Players_AllHaveFullAvatar`; `RoomState_Leaderboard_AllHaveFullAvatar`; `LeaderboardUpdated_HasAvatars`; `GameEnded_FinalLeaderboard_HasAvatars`; `AccountWithSavedAvatar_ShowsSaved`; `AccountWithoutRow_ShowsDefaultForAccount`; `Guest_ShowsDefaultForGuestByName_GreyBackground`; `TwoGuestsSameName_SameAvatar`; `AvatarChangedBetweenSnapshots_NextSnapshotShowsNew` (nhân vật hiện tại, không chụp lại); `ChangingAvatar_DoesNotChangeScoresRanksOrRewards`
- [ ] T040 [US3] Sửa `backend/QuizBattle.Application/Game/`: thêm `Avatars` (`IReadOnlyDictionary<Guid, UserAvatar>`) vào `GameSnapshot.cs`; thêm trường `AvatarDto Avatar` vào `PlayerSummaryDto`, `LeaderboardEntryDto`, `PlayerJoinedDto`; `GameService.cs` thêm `AvatarOf(snapshot, player)` (đã lưu → `ForAccount(userId)` → `ForGuest(displayName)`) dùng ở mọi chỗ dựng ba DTO trên (`JoinAsPlayerAsync`, `BuildLeaderboard`, `BuildRoomState`); sửa mọi nơi gọi hàm tạo các record này (kể cả `GameHub`/`GameBroadcaster` nếu có). Làm T039 xanh; sửa các test cũ theo chữ ký DTO mới
- [ ] T041 [P] [US3] Viết test (FAIL trước) `backend/QuizBattle.Tests/Profile/XpLeaderboardAvatarTests.cs` (và/hoặc tích hợp `XpLeaderboardTests`): mỗi dòng có `avatar` đủ ô; tài khoản chưa lưu nhận nhân vật mặc định ổn định; DTO **không** có `userId`
- [ ] T042 [US3] Sửa `backend/QuizBattle.Application/Auth/IUserRepository.cs` (`XpRankRow` thêm `UserId` và `UserAvatar?`), `backend/QuizBattle.Infrastructure/Persistence/UserRepository.cs` (`GetTopByXpAsync`: một projection `LEFT JOIN UserAvatars`, `AsNoTracking`, vẫn `Take(count)` 50 dòng), `backend/QuizBattle.Application/Profile/XpLeaderboardEntryDto.cs` (thêm `AvatarDto Avatar`), `ProfileService.GetXpLeaderboardAsync` (mặc định cho dòng không có avatar đã lưu); cập nhật `FakeUserRepository`. Làm T041 xanh
- [ ] T043 [US3] Viết test tích hợp (FAIL trước) `backend/QuizBattle.Tests/Integration/GameAvatarsQueryTests.cs` (`[RequiresDatabaseFact]`): **đếm lệnh SQL** bằng một `DbCommandInterceptor` trong test khi gọi `FindSnapshotByCodeAsync`: `Snapshot_SqlCommandCount_IsSameForTwoAndFiftyPlayers` (phòng 2 người và phòng 50 người, cùng số lệnh; ghi con số cụ thể vào task); `Snapshot_AvatarsLoadedWithExactlyOneExtraQuery` (so với số lệnh trước tính năng, +1); `XpLeaderboard_FiftyRows_SingleQuery`; `Snapshot_Avatars_NotLockedForUpdate` (không có `FOR UPDATE` trên `UserAvatars`; thứ tự khóa `Room → Users` không đổi)
- [ ] T044 [US3] Sửa `backend/QuizBattle.Infrastructure/Persistence/GameRepository.cs` `LoadSnapshotAsync`: thêm **đúng một** truy vấn `AsNoTracking` `UserAvatars WHERE UserId = ANY(userIds)` (cùng mảng `userIds` đã dùng cho `Users`, không khóa) vào `GameSnapshot.Avatars`. Làm T043 xanh
- [ ] T045 [P] [US3] Cập nhật `specs/002-avatar-shop/contracts/signalr-events.md` bằng con số **đo thật** của T043 (số lệnh SQL) và kích thước payload `RoomState` thực với phòng 50 người (serialize bằng `System.Text.Json` trong một test hoặc script, ghi kích thước byte); nếu khác ước tính (≈ 21 KB / 10,5 KB) thì sửa số liệu trong contract và plan

### Cổng ra và push Đợt C

- [ ] T046 **Cổng ra Đợt C**: `dotnet build` (0 warning), `dotnet test` (Passed/Skipped/Failed; Skipped = 0; ghi số test tăng), frontend `typecheck`/`test` còn xanh (DTO TS chưa đổi nên chưa lệch). Ghi số lệnh SQL phòng 2 và 50 người từ T043
- [ ] T047 **Push Đợt C** và báo cố vấn; **chờ cố vấn đọc diff** (`GameRepository`, `GameService`, DTO) trước Đợt D

**Checkpoint**: backend hoàn chỉnh; toàn bộ hợp đồng đã sẵn cho frontend.

---

## Đợt D: Frontend nền

**Mục tiêu**: vẽ được nhân vật từ DTO; trình chỉnh ở `/profile`; THIRD_PARTY_NOTICES.md; CLAUDE.md cập nhật; số đo bundle trước/sau. Không dùng `api.dicebear.com`.

- [ ] T048 Đo bundle **TRƯỚC**: `npm --prefix frontend run build`, ghi vào task này kích thước (raw và gzip) của file JS chính (chunk `index-*.js`) và tổng thư mục `frontend/dist/assets`; lúc này chưa có mã import DiceBear nên đây là số gốc
- [ ] T049 [P] Sửa `CLAUDE.md` (đề xuất đã nêu ở plan): mục Stack thêm "Vẽ nhân vật: `@dicebear/core` + `@dicebear/styles` (kiểu Notionists, CC0), ghim phiên bản chính xác, KHÔNG dùng API HTTP `api.dicebear.com`"; mục Cấu trúc thư mục frontend thêm `src/avatar/ Logic thuần của nhân vật (dựng tùy chọn DiceBear, cache)`; không sửa phần khác
- [ ] T050 [P] Tạo `THIRD_PARTY_NOTICES.md` ở gốc repo: bảng `@dicebear/core` `10.6.0` (MIT, Copyright (c) 2026 Florian Körner, https://github.com/dicebear/dicebear) và `@dicebear/styles` `10.6.0` (file `notionists.json`: **CC0 1.0**, bản phối lại từ "Notionists" của Zoish, nguồn gốc https://heyzoish.gumroad.com/l/notionists và https://bio.link/heyzoish, gói https://github.com/dicebear/styles; mã và manifest của gói: MIT); ghi chú rằng chỉ `notionists.json` được dùng trong bản build; trích văn bản giấy phép MIT từ `LICENSE.md` của gói và đường dẫn tới CC0 1.0 (https://creativecommons.org/publicdomain/zero/1.0/)
- [ ] T051 [P] Tạo `frontend/src/types/avatar.ts` (`AvatarSlot`, `Avatar` 11 ô, `AvatarItem`, `AvatarTier`, `MyAvatar`, `PurchaseResult`) khớp DTO backend; sửa `frontend/src/types/game.ts` (thêm `avatar: Avatar` vào `PlayerSummary`, `LeaderboardEntry`, `PlayerJoined` nếu có kiểu riêng) và `frontend/src/types/profile.ts` (`XpLeaderboardEntry.avatar`)
- [ ] T052 [P] Viết test (FAIL trước) `frontend/src/avatar/avatarOptions.test.ts`: `buildOptions_FullAvatar_SetsVariantAndProbability100PerSlot`; `buildOptions_NullOptionalSlot_SetsProbability0`; `buildOptions_NullBackground_UsesDefaultD4d4d4`; `buildOptions_BackgroundHexWithoutHash`; `buildOptions_SeedIsStableJoinOfSlotKeys`; `signature_SameAvatarSameString_DifferentAvatarDifferent`; `DEFAULT_BACKGROUND_matchesBackendConstant` (so với hằng `d4d4d4` được nhập từ một fixture JSON khớp backend)
- [ ] T053 Tạo `frontend/src/avatar/avatarSlots.ts` (danh sách ô theo thứ tự, ô bắt buộc/tùy chọn, tên hiển thị tiếng Việt), `frontend/src/avatar/avatarOptions.ts` (hàm thuần `buildOptions(avatar)` → tùy chọn DiceBear như [research.md](research.md) R1: `<ô>Variant` + `<ô>Probability: 100/0`, `backgroundColor: [hex]`, `seed`; `signature(avatar)`), `frontend/src/avatar/printableClothes.ts` (hằng `PRINTABLE_CLOTHES` = `variant01`..`variant10`, hàm `isPrintable(clothesKey)`). Làm T052 xanh
- [ ] T054 [P] Viết test (FAIL trước) `frontend/src/avatar/printableClothes.test.ts` (**test tập áo in được**): với đúng phiên bản DiceBear đã ghim, nạp `@dicebear/styles/notionists.json`, với **mỗi** trong 25 kiểu áo vẽ SVG bằng `Avatar` có và không có `clothesGraphicVariant: 'saturn'` (cố định mọi ô khác, `seed` giống nhau), bỏ hậu tố id 8 hex rồi so sánh; tập áo cho SVG **khác nhau** phải bằng đúng `PRINTABLE_CLOTHES` (`variant01`..`variant10`). Thông báo lỗi nói rõ "nâng cấp DiceBear làm lệch tập áo in được: cập nhật `AvatarCatalog.PrintableClothesVariantKeys` ở backend và `PRINTABLE_CLOTHES`"
- [ ] T055 [P] Viết test (FAIL trước) `frontend/src/avatar/guards.test.ts`: `package.json_DicebearPackages_PinnedExactly` (`dependencies['@dicebear/core'] === '10.6.0'`, `['@dicebear/styles'] === '10.6.0'`, không có `^`/`~`); `src_DoesNotReference_api_dicebear_com` (quét mọi file dưới `frontend/src`, ngoại trừ chính file test, không chứa chuỗi `api.dicebear.com`); `src_OnlyImportsNotionistsStyleFile` (không import `@dicebear/styles` ở đường dẫn khác `notionists.json`); `main_entry_doesNotStaticallyImportStyleFile` (chỉ `import()` động)
- [ ] T056 Tạo `frontend/src/avatar/avatarRenderer.ts`: nạp style bằng **`import('@dicebear/styles/notionists.json')` động** (một Promise dùng chung), tạo `new Style(def)` một lần, `toDataUri(avatar)` bằng `new Avatar(style, buildOptions(avatar))`, bộ nhớ đệm `Map` theo `signature`; không bao giờ chèn SVG thô vào DOM (dùng data URI cho `<img>`). Làm T054, T055 xanh
- [ ] T057 [P] Tạo `frontend/src/api/shop.ts` (`getShopItems`, `purchaseItem`) và `frontend/src/api/avatar.ts` (`getMyAvatar`, `saveMyAvatar`), mọi HTTP qua `request()`; tạo `frontend/src/api/avatarErrors.ts`: **một bảng duy nhất** ánh xạ `code` (`item_not_found`, `item_not_purchasable`, `already_owned`, `insufficient_coins`, `unknown_variant`, `item_not_owned`, `graphic_incompatible_clothes`, `validation_failed`) → thông điệp tiếng Việt, có test `avatarErrors.test.ts` (mọi mã có thông điệp; mã lạ → thông điệp chung); sửa `frontend/src/api/leaderboards.ts` dùng kiểu mới
- [ ] T058 [P] Tạo `frontend/src/hooks/useShopItems.ts` (TanStack Query khóa `['shop','items']`) và `frontend/src/hooks/useMyAvatar.ts` (khóa `['me','avatar']`, bật khi đã đăng nhập; hàm làm mới `['me','avatar']` và `['me','profile']`)
- [ ] T059 [US3] Viết test component (FAIL trước) `frontend/src/components/Avatar.test.tsx` (`// @vitest-environment jsdom`): hiện khung chờ khi style chưa nạp; hiện `<img>` với `src` data URI khi sẵn sàng; `alt` có nghĩa (hoặc `aria-hidden` khi trang trí cạnh tên); cùng `avatar` hai lần chỉ gọi renderer một lần (cache); hai cỡ (`sm` 28 px, `lg` 160 px); không bao giờ có `dangerouslySetInnerHTML`
- [ ] T060 [US3] Tạo `frontend/src/components/Avatar.tsx` (component hàm, khung tròn/bo góc bằng Tailwind, không bóng, không gradient, màu theo dữ liệu; props `avatar`, `size`). Làm T059 xanh
- [ ] T061 [US2] Viết test component (FAIL trước) `frontend/src/components/AvatarEditor.test.tsx`: chỉ liệt kê món miễn phí và món đã sở hữu theo ô (không thấy món chưa mua); ô tùy chọn có nút "bỏ chọn", ô bắt buộc không có; nút Lưu `disabled` khi đang gửi và khi không có thay đổi; lưu gửi đủ 11 ô một lần; **danh sách hình in bị vô hiệu hóa kèm lý do khi áo đang chọn không in được**, và chọn lại áo in được thì bật lại; lỗi server (`graphic_incompatible_clothes`, `item_not_owned`) hiện thông điệp từ bảng ánh xạ; trạng thái đang tải, lỗi (nút thử lại), rỗng (danh mục rỗng) đủ cả ba
- [ ] T062 [US2] Tạo `frontend/src/components/AvatarEditor.tsx` (tách nhỏ nếu > ~150 dòng: `AvatarSlotPicker.tsx`) và nhúng vào `frontend/src/pages/ProfilePage/ProfilePage.tsx`: nhân vật lớn + trình chỉnh, trạng thái lưu nháp bằng `useState` chỉ cho **lựa chọn chưa lưu** (dữ liệu server vẫn qua TanStack Query), mutation lưu rồi làm mới `['me','avatar']`; cập nhật `ProfilePage.test.tsx`. Làm T061 xanh
- [ ] T063 Đo bundle **SAU**: `npm --prefix frontend run build`; ghi vào task này: kích thước file JS chính raw/gzip so với T048 (độ chênh phải nhỏ: chỉ `@dicebear/core` và mã mới), kích thước **chunk riêng** chứa `notionists` (kỳ vọng ≈ 372 KB raw, ≈ 111-113 KB gzip), và bằng chứng chunk chính **không** chứa dữ liệu style (ví dụ `grep -c` một chuỗi đặc trưng của `notionists.min.json` trong chunk chính = 0, trong chunk riêng > 0); nếu `@dicebear/core` làm bundle chính phình bất thường (> 100 KB gzip) thì dừng và báo cố vấn

### Cổng ra và push Đợt D

- [ ] T064 **Cổng ra Đợt D**: `dotnet build` (0 warning) và `dotnet test` (Passed/Skipped/Failed; Skipped = 0) còn xanh; `npm --prefix frontend run typecheck`, `lint`, `test` (ghi số file/test), `build` (số liệu T048 → T063)
- [ ] T065 **Push Đợt D** và báo cố vấn: commit, file, số liệu bundle trước/sau, chunk `notionists`; **chờ cố vấn đọc diff** trước Đợt E

**Checkpoint**: vẽ được nhân vật; chỉnh và lưu ở `/profile`.

---

## Đợt E: Cửa hàng và hiển thị

**Mục tiêu**: `/shop`; nhân vật nhỏ ở sảnh chờ, bảng điểm giữa/cuối trận, `/ranking`; chạy đủ quickstart.

- [ ] T066 [P] [US1] Viết test component (FAIL trước) `frontend/src/pages/ShopPage/ShopPage.test.tsx` (`// @vitest-environment jsdom`): đủ ba trạng thái tải/lỗi (có thử lại)/rỗng; hiện danh sách theo ô và mức giá, đánh dấu "đã sở hữu"; hiện số dư Xu khi đã đăng nhập; **khách**: xem danh mục và xem thử trên nhân vật mặc định chỉ món miễn phí, nút mua dẫn tới đăng nhập, không gọi API mua; **đã đăng nhập**: nút Mua `disabled` khi đang gửi (bấm hai lần chỉ gửi một request); không đủ Xu → nút bị vô hiệu hóa/báo `insufficient_coins`; `already_owned` sau khi bấm hai lần được hiểu là thành công không đổi (làm mới, không hiện đỏ); mua thành công làm mới số dư và trạng thái sở hữu; xem thử một hình in trên áo không in được tự đổi sang áo in được và có ghi chú; xem thử không thay đổi nhân vật đã lưu
- [ ] T067 [US1] Tạo `frontend/src/pages/ShopPage/ShopPage.tsx` (+ `ShopItemCard.tsx`, `CoinBalance.tsx` trong `frontend/src/components/` hoặc thư mục trang; mỗi file một component chính, ≤ ~150 dòng); thêm route `/shop` ở `frontend/src/App.tsx` (công khai, không bọc `ProtectedRoute` vì xem danh mục không cần đăng nhập; hành động mua yêu cầu đăng nhập); thêm liên kết "Cửa hàng" vào header ở những nơi đang có liên kết `/ranking`/`/profile` (`DashboardPage.tsx`, `ProfilePage.tsx`, `HomePage.tsx`, `XpRankingPage.tsx`) theo hệ thiết kế true-black-flat (nút/liên kết không dùng màu `accent`). Làm T066 xanh
- [ ] T068 [P] [US3] Viết test (FAIL trước) cho hiển thị nhân vật nhỏ: cập nhật `frontend/src/components/PlayerList.tsx`/`Leaderboard.tsx` có test (`PlayerList.test.tsx`, `Leaderboard.test.tsx`), `frontend/src/pages/XpRankingPage/XpRankingPage.test.tsx`, `frontend/src/realtime/gameReducer.test.ts` (reducer giữ `avatar` từ `RoomState`, `PlayerJoined`, `LeaderboardUpdated`, `GameEnded`); khẳng định mỗi dòng có `Avatar` cỡ nhỏ cạnh tên
- [ ] T069 [US3] Sửa hiển thị: `frontend/src/components/PlayerList.tsx`, `frontend/src/components/Leaderboard.tsx` (dùng ở sảnh chờ, bảng điểm giữa trận, cuối trận của `frontend/src/pages/PlayPage/` và `RoomLivePage/`), `frontend/src/pages/XpRankingPage/XpRankingPage.tsx`; `frontend/src/realtime/gameReducer.ts` giữ `avatar` đúng như nhận; không tự sinh nhân vật ở frontend. Làm T068 xanh
- [ ] T070 [US3] Kiểm `frontend/src/realtime/events.ts` **không đổi** (danh sách sự kiện cố định); xác nhận bằng test hiện có không đổi tên sự kiện
- [ ] T071 [P] Nếu máy đã cài skill `design-taste-frontend` (CLAUDE.md): chạy `node <đường dẫn skill>/scripts/lint.mjs --app <file>` cho các file giao diện mới/đổi và `node <đường dẫn skill>/scripts/inspect.mjs --app <url bản build>`; sửa hết FAIL (không gradient, không bóng, không emoji, màu nhấn chỉ cho dữ liệu, `tabular-nums` cho Xu); không có skill thì ghi "không có skill" vào task này
- [ ] T072 Chạy toàn bộ [quickstart.md](quickstart.md) mục 2 (kịch bản 1-12) trên môi trường dev, mục 3 (truy vấn bất biến), mục 4 (đồng thời, gồm kiểm tay độ nhạy ở bước 3 nếu chưa làm ở T030); ghi kết quả từng kịch bản vào task này. Lỗi tìm được sửa thành commit `fix:` riêng nhỏ

### Cổng ra và push Đợt E

- [ ] T073 **Cổng ra Đợt E**: `dotnet build` (0 warning), `dotnet test` (Passed/Skipped/Failed; Skipped = 0), `npm --prefix frontend run typecheck`, `lint`, `test` (số file/test), `build` (bundle chính và chunk `notionists` so với T063); quickstart T072 đạt
- [ ] T074 **Push Đợt E** và báo cố vấn: commit, file, số liệu T073, kết quả quickstart; **chờ cố vấn đọc diff** trước Đợt F

**Checkpoint**: tính năng hoàn chỉnh trên dev.

---

## Đợt F: Chốt trước merge

**Mục tiêu**: cố vấn tự kiểm bằng trình duyệt; sinh SQL production rồi dừng để người dùng tự chạy. **Không merge vào `main` khi chưa có xác nhận của người dùng.**

- [ ] T075 **Cố vấn test trình duyệt trên localhost (DỪNG, chờ người dùng báo kết quả)**: khởi động `docker compose up -d`, backend (`dotnet run` với DB dev đã `dotnet ef database update`) và frontend (`npm --prefix frontend run dev`, `VITE_API_URL` đúng); báo cho người dùng URL, tài khoản thử (`host`, `a`, `b`, một khách), các điểm nên thử (mua trùng, mua thiếu Xu, hình in với áo không in được, đổi đồ giữa trận, điện thoại cỡ nhỏ, khăn trùm đầu miễn phí). **Dừng và chờ**: không làm tiếp T076 cho tới khi người dùng báo kết quả. Mỗi lỗi tìm được sửa thành **commit `fix:` riêng** (kèm test nếu là logic), chạy lại cổng ra liên quan và push; lặp tới khi người dùng xác nhận "đạt" rồi mới đi tiếp
- [ ] T076 **Sinh SQL production và DỪNG**: chạy `./scripts/migrate-prod.ps1` **không** truyền `-ConnectionString` (chỉ sinh `scripts/out/migrate-prod.sql`, file bị `.gitignore`, không commit); ghi kích thước và SHA-256 của file; kiểm file chứa `AddAvatarShop`, 201 `INSERT` danh mục, ba `ENABLE ROW LEVEL SECURITY`, và là idempotent (`IF NOT EXISTS`/`__EFMigrationsHistory`); **đưa đường dẫn file cho người dùng và DỪNG**: người dùng tự chạy lên Supabase (Session pooler cổng 5432). Tuyệt đối không chạy `psql` vào production, không dùng connection string production. Chờ người dùng báo đã áp xong và kết quả kiểm (201 dòng `AvatarItems`, RLS bật ba bảng mới, `GET /api/shop/items` trả 201 phần tử). **Không merge** `feat/avatar-shop` vào `main` (không PR-merge, không `git merge`) cho tới khi người dùng xác nhận bằng lời

---

## Dependencies & Execution Order

- **Đợt A → B → C → D → E → F**, tuần tự, mỗi Đợt kết thúc bằng Cổng ra rồi Push; chỉ sang Đợt sau khi cố vấn đã đọc diff.
- Trong Đợt: test (task "FAIL trước") đứng trước mã; task [P] khác file có thể đảo thứ tự nhưng vẫn chạy tuần tự.
- **T001-T002 (ghim DiceBear) phải xong trước T017 (test đồng bộ style) và mọi task frontend DiceBear (T054 trở đi).**
- **T013 (migration) sau T010-T012**; T014, T019, T020 sau T013; T019 trước T020.
- **T025 (khung kiểu) trước T026/T028**; T029 sau T028 (test trước); T030 sau T029.
- **T040 (GameService) sau T039; T044 (GameRepository) sau T043**.
- **T048 (đo bundle trước) trước mọi import DiceBear vào mã ứng dụng (T056)**; T063 sau T062.
- **T075 (trình duyệt) sau Cổng ra Đợt E; T076 sau khi người dùng xác nhận T075.**

## Phụ thuộc giữa user story

- **US1** (mua đồ): T004-T005, T010-T016, T023-T030, T036, T066-T067.
- **US2** (chỉnh và lưu nhân vật): T006-T009, T031-T036, T061-T062.
- **US3** (nhân vật trong phòng, bảng xếp hạng): T039-T045, T059-T060, T068-T070.
- **US4** (đồng thời mua đồ và kết thúc trận): T028 (`Purchase_ConcurrentWithMatchEnd_…`), T029, T030.

## Cơ hội song song (chỉ để tham khảo; chạy tuần tự)

- Đợt A: T003, T004, T006, T008 (test Domain); T010, T014, T015, T018.
- Đợt B: T023, T026, T032.
- Đợt C: T039, T041, T045.
- Đợt D: T049, T050, T051, T052, T054, T055, T057, T058.
- Đợt E: T066, T068, T071.

## Implementation Strategy

- **MVP**: Đợt A-B (backend: danh mục, mua đồ, lưu nhân vật) rồi Đợt D-E phần `/shop` và `/profile` cho người dùng thấy giá trị; Đợt C (nhân vật trong phòng) là phần thêm.
- Mỗi Đợt để lại `main` luôn chạy được (làm trên nhánh), build và test xanh, push để cố vấn đọc diff.
- Không tự quyết khi gặp mâu thuẫn với CLAUDE.md hoặc [plan.md](plan.md): dừng và hỏi.

## Notes

- **Số lượng**: 76 task (Đợt A: T001-T022 = 22; Đợt B: T023-T038 = 16; Đợt C: T039-T047 = 9; Đợt D: T048-T065 = 18; Đợt E: T066-T074 = 9; Đợt F: T075-T076 = 2).
- Commit Conventional Commits theo từng task logic nhỏ (`feat:`, `test:`, `fix:`, `docs:`, `chore:`), không commit `.env`, `bin/`, `obj/`, `node_modules/`, `scripts/out/`.
- Tên hiển thị tạm ("Tóc 09", "Áo 06"...) được cố vấn chấp nhận, giữ nguyên.
- Không thêm thư viện nào ngoài hai gói DiceBear đã duyệt.
