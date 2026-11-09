---

description: "Danh sách task triển khai: Tài khoản người chơi, XP và Xu"
---

# Tasks: Tài khoản người chơi, XP và Xu

**Input**: [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/](contracts/), [quickstart.md](quickstart.md)

**Prerequisites**: plan.md, spec.md (đã có). Nhánh `feat/player-xp-coins`.

**Tests**: CÓ. Spec/plan yêu cầu test cho mọi luật FR-003..008, FR-019..021 (xem [research.md R10](research.md#r10-ma-trận-test-backend)). Viết test trước, thấy FAIL rồi mới viết mã.

**Organization**: gom theo user story (US1..US6 theo [spec.md](spec.md)). Quy tắc dự án trong [CLAUDE.md](../../CLAUDE.md) thắng khi mâu thuẫn: làm từng bước nhỏ, build + test sau mỗi tính năng, commit Conventional Commits.

## Format: `[ID] [P?] [Story] Mô tả kèm đường dẫn file`

- **[P]**: chạy song song được (khác file, không phụ thuộc task chưa xong)
- **[Story]**: US1..US6 (chỉ ở phase user story)
- Đường dẫn tương đối từ gốc repo. Backend ở `backend/`, frontend ở `frontend/`.

---

## Phase 1: Setup (hạ tầng chung)

**Purpose**: dựng hạ tầng test và CI đã được duyệt, chưa đụng logic.

- [x] T001 [P] Thêm devDependencies `jsdom` và `@testing-library/react` vào `frontend/package.json` (chạy `npm install` trong `frontend/`, commit cả `package-lock.json`); không thêm vào `dependencies`
- [x] T002 [P] Sửa `frontend/vite.config.ts`: giữ `environment: 'node'`, đổi `include` thành `['src/**/*.test.ts', 'src/**/*.test.tsx']`; các file component khai báo `// @vitest-environment jsdom` ở đầu file
- [x] T003 [P] Sửa `.github/workflows/ci.yml`: job `backend` thêm `services.postgres` (`postgres:16-alpine`, user/mật khẩu/db đặt trong `env` của job, có `options` healthcheck `pg_isready`, map cổng 5432) và biến `QUIZBATTLE_TEST_DB` trỏ tới service đó (role có quyền `CREATEDB`)
- [x] T004 [P] Tạo `backend/QuizBattle.Tests/Integration/RequiresDatabaseFactAttribute.cs`: `FactAttribute` đặt `Skip` trong constructor khi thiếu biến môi trường `QUIZBATTLE_TEST_DB` (xUnit thuần, không thêm package)
- [x] T005 Tạo `backend/QuizBattle.Tests/Integration/DatabaseFixture.cs`: `IAsyncLifetime` + `[CollectionDefinition("Database")]` chia sẻ một DB cho cả lần chạy; `CREATE DATABASE "qb_test_<guid>"` qua `QUIZBATTLE_TEST_DB`, tạo `AppDbContext`, `MigrateAsync()`, khi xong `NpgsqlConnection.ClearAllPools()` rồi `DROP DATABASE "qb_test_<guid>" WITH (FORCE)` (lỗi DROP chỉ log tên DB, không làm test fail); thiếu biến thì không tạo gì (phụ thuộc T004)

**Checkpoint**: `dotnet build backend/QuizBattle.sln` và `npm --prefix frontend test` vẫn qua.

---

## Phase 2: Foundational (BLOCKING cho mọi user story)

**Purpose**: Domain thuần, schema, migration, lỗi dùng chung và khung repository/snapshot.

**⚠️ CRITICAL**: chưa story nào bắt đầu được trước khi phase này xong.

### Domain (hàm thuần, test trước)

- [x] T006 [P] Tạo `backend/QuizBattle.Domain/Enums/RewardStatus.cs` (`Awarded`, `NotAwarded`), `RewardDenialReason.cs` (`Host`, `NoAnswers`, `NotEnoughAccounts`), `CoinTransactionReason.cs` (`MatchReward`, `ShopPurchase`; `ShopPurchase` để dành cho tính năng 2, chưa dùng)
- [x] T007 [P] Viết test (FAIL trước) `backend/QuizBattle.Tests/Domain/LevelCalculatorTests.cs`: `LevelFromXp_0_Returns1`, `_199_Returns1`, `_200_Returns2`, `_599_Returns2`, `_600_Returns3`, `XpForLevel_RoundTripsWithLevelFromXp`, `Progress_ReturnsXpIntoLevelAndXpForNext` (ngưỡng cấp n = `100 * n * (n - 1)`; cần `200 * n` XP để lên từ n sang n+1)
- [x] T008 Tạo `backend/QuizBattle.Domain/LevelCalculator.cs`: `XpForLevel(n) = 100 * n * (n - 1)`, `FromXp(xp)` = cấp lớn nhất n sao cho `XpForLevel(n) <= xp`, `Progress(xp)` trả `{ Level, XpIntoLevel, XpForNext = 200 * Level }`; không phụ thuộc DB/đồng hồ (phụ thuộc T007)
- [x] T009 [P] Viết test (FAIL trước) `backend/QuizBattle.Tests/Domain/RewardCalculatorTests.cs` theo ma trận R10: FR-004 (`Calculate_OnlyOneAnsweredAccount_NoOneRewarded`, `_TwoAccountsOneNeverAnswered_NoOneRewarded`, `_HostAndOneAccount_NotEnoughAccounts`, `_OneAccountManyGuests_NoOneRewarded`), FR-005 (`_HostPlaysOwnRoom_HostNotRewardedButOthersAre`), FR-006 (`_GuestPlayer_NeverRewarded`), FR-007 (`CalculateXp_Score999_Returns99`, `_ZeroScore_ReturnsZero`), FR-008 (`CalculateCoins_Score250Rank4_Returns2`, `...Rank1_Adds30`, `...Rank2_Adds20`, `...Rank3_Adds10`, `...ZeroScoreRank1_NoRankBonus`), hạng (`Rank_TiedAtTop_BothRank1AndNextIsRank3`, `Rank_GuestTakesFirst_AccountSecondGetsRank2Bonus`, `Rank_HostCountsInRankingButNotRewarded`), FR-013 (`Calculate_DenialReasonPrecedence_GuestThenHostThenNoAnswersThenNotEnoughAccounts`)
- [x] T010 Tạo `backend/QuizBattle.Domain/RewardCalculator.cs`: `Calculate(participants, hostUserId)` nhận `{ PlayerId, UserId?, Score, AnsweredCount }` của mọi người chơi, trả `{ PlayerId, RewardRank, Status, DenialReason?, Xp, Coins, RankBonus }`. Quy tắc: `RewardRank` = competition ranking theo `Score` giảm dần trên mọi người chơi (1, 1, 3); đủ điều kiện = có `UserId`, `UserId != hostUserId`, `AnsweredCount >= 1`; trận hợp lệ khi có `>= 2` `UserId` khác nhau đủ điều kiện; `Xp = Score / 10`, `Coins = Score / 100`, `RankBonus = Score > 0 ? {1:30, 2:20, 3:10}[RewardRank] : 0`; còn lại `NotAwarded`, `DenialReason` ưu tiên `Host` > `NoAnswers` > `NotEnoughAccounts` (phụ thuộc T006, T009)

### Entities và schema

- [x] T011 [P] Sửa `backend/QuizBattle.Domain/Entities/User.cs`: thêm `TotalXp` (`int`, mặc định 0) và `CoinBalance` (`int`, mặc định 0); sửa `backend/QuizBattle.Domain/Entities/Player.cs`: bỏ ghi chú "CHƯA dùng ở bước này" ở `UserId`
- [x] T012 [P] Tạo `backend/QuizBattle.Domain/Entities/MatchReward.cs` theo [data-model.md](data-model.md): `Id`; `PlayerId` (`Guid?`, `uuid NULL`, FK Players `ON DELETE SET NULL`, unique `IX_MatchRewards_PlayerId`); `UserId` (`uuid NOT NULL`, FK Users `ON DELETE CASCADE`); `RoomId` (`Guid?`, `uuid NULL`, FK Rooms `ON DELETE SET NULL`); `RoomCode` (`varchar(6) NOT NULL`); `RoomTopic` (`varchar(200) NOT NULL`); `FinishedAt` (`timestamptz NOT NULL`); `Status` (`RewardStatus`); `DenialReason` (`RewardDenialReason?`, NULL khi `Awarded`); `Score`, `RewardRank`, `Xp`, `Coins`, `RankBonus` (`int NOT NULL`)
- [x] T013 [P] Tạo `backend/QuizBattle.Domain/Entities/CoinTransaction.cs`: `Id`; `UserId` (`uuid NOT NULL`, FK Users `ON DELETE CASCADE`); `Amount` (`int NOT NULL`, `CHECK ("Amount" <> 0)`); `Reason` (`CoinTransactionReason`, lưu `text`); `RoomId` (`uuid NULL`, FK Rooms `ON DELETE SET NULL`); `MatchRewardId` (`uuid NULL`, FK MatchRewards `ON DELETE SET NULL`, unique có điều kiện `WHERE "MatchRewardId" IS NOT NULL`); `CreatedAt` (`timestamptz NOT NULL`)
- [x] T014 [P] Sửa `backend/QuizBattle.Infrastructure/Persistence/Configurations/UserConfiguration.cs`: cột `TotalXp` `int NOT NULL DEFAULT 0` với `CHECK ("TotalXp" >= 0)` tên `CK_Users_TotalXp_NonNegative`; `CoinBalance` `int NOT NULL DEFAULT 0` với `CHECK ("CoinBalance" >= 0)` tên `CK_Users_CoinBalance_NonNegative`; index `IX_Users_TotalXp` trên `(TotalXp DESC, CreatedAt, Id)` với `HasFilter("\"TotalXp\" > 0")`
- [x] T015 [P] Sửa `backend/QuizBattle.Infrastructure/Persistence/Configurations/PlayerConfiguration.cs`: thêm `HasIndex(p => new { p.RoomId, p.UserId }).IsUnique().HasFilter("\"UserId\" IS NOT NULL")` tên `IX_Players_RoomId_UserId`; cập nhật comment "chuẩn bị" thành mô tả đang dùng; giữ nguyên `IX_Players_RoomId_lower_DisplayName` và `IX_Players_SessionTokenHash`
- [x] T016 [P] Tạo `backend/QuizBattle.Infrastructure/Persistence/Configurations/MatchRewardConfiguration.cs`: bảng `MatchRewards`; quan hệ và `ON DELETE` như T012; `Status`/`DenialReason` lưu chuỗi; `CHECK ("Xp" >= 0 AND "Coins" >= 0 AND "RankBonus" >= 0)`; `CHECK (("Status" = 'Awarded') = ("DenialReason" IS NULL))`; unique `IX_MatchRewards_PlayerId`; index `IX_MatchRewards_UserId_FinishedAt` trên `(UserId, FinishedAt DESC)`
- [x] T017 [P] Tạo `backend/QuizBattle.Infrastructure/Persistence/Configurations/CoinTransactionConfiguration.cs`: bảng `CoinTransactions`; ràng buộc như T013; index `IX_CoinTransactions_UserId_CreatedAt` trên `(UserId, CreatedAt DESC)`
- [x] T018 Sửa `backend/QuizBattle.Infrastructure/Persistence/AppDbContext.cs`: thêm `DbSet<MatchReward> MatchRewards` và `DbSet<CoinTransaction> CoinTransactions` (phụ thuộc T012, T013)
- [x] T019 Sinh migration `AddXpCoinsAndPlayerAccounts` trong `backend/QuizBattle.Infrastructure/Persistence/Migrations/` bằng `dotnet ef migrations add` (không sửa tay snapshot); xem lại file sinh ra: không có SQL chuyển dữ liệu, cột mới có DEFAULT 0, `Down()` đúng (phụ thuộc T011–T018)
- [x] T020 Viết test tích hợp nền (cần `QUIZBATTLE_TEST_DB`) `backend/QuizBattle.Tests/Integration/SchemaConstraintsTests.cs`: `Users_NegativeCoinBalance_ViolatesCheck`, `Users_NegativeTotalXp_ViolatesCheck`, `MatchRewards_DuplicatePlayerId_ViolatesUniqueIndex`, `MatchRewards_MultipleNullPlayerId_AllowedByUniqueIndex`, `Players_SameUserSameRoom_ViolatesPartialUnique`, `Players_ManyGuestsSameRoom_Allowed`, `CoinTransactions_ZeroAmount_ViolatesCheck` (phụ thuộc T005, T019)

### Lỗi dùng chung và khung repository

- [x] T021 [P] Sửa `backend/QuizBattle.Application/Common/UnauthorizedException.cs`: thêm tham số `Code` tùy chọn (`string?`); tạo `backend/QuizBattle.Application/Common/SessionReplacedException.cs` (ý nghĩa 401, mã `session_replaced`)
- [x] T022 [P] Viết test (FAIL trước) `backend/QuizBattle.Tests/Api/GlobalExceptionHandlerTests.cs`: `GlobalExceptionHandler_UnauthorizedWithCode_WritesCodeExtension`, `..._UnauthorizedWithoutCode_NoCodeExtension`; và `backend/QuizBattle.Tests/Game/GameHubExceptionFilterTests.cs`: `GameHubExceptionFilter_SessionReplaced_SendsSessionReplacedCode`
- [x] T023 Sửa `backend/QuizBattle.Api/Middleware/GlobalExceptionHandler.cs`: ghi `ProblemDetails.Extensions["code"]` khi `UnauthorizedException.Code` có giá trị; sửa `backend/QuizBattle.Api/Hubs/GameHubExceptionFilter.cs`: map `SessionReplacedException` sang mã `session_replaced`; cập nhật comment ở `backend/QuizBattle.Api/Hubs/GameErrorDto.cs` (phụ thuộc T021, T022)
- [x] T024 Sửa `backend/QuizBattle.Application/Game/GameSnapshot.cs`: thêm `Users` (dictionary theo `User.Id` cho các người chơi có tài khoản), `IsFinalAdvance` (bool), `Rewards` (đọc từ `MatchRewards` khi phòng `Finished`), `NewMatchRewards` và `NewCoinTransactions` (danh sách có thể ghi, để service thêm)
- [x] T025 Sửa `backend/QuizBattle.Application/Game/IGameRepository.cs` và `backend/QuizBattle.Infrastructure/Persistence/GameRepository.cs`: `LoadSnapshotAsync` nạp `Users` (`AsNoTracking`, chỉ lấy `TotalXp`); khi `IsFinalAdvance` (phòng `InProgress`, câu đã duyệt cuối đã `StartedAt` và `EndedAt`) nạp `Users` bằng `SELECT * FROM "Users" WHERE "Id" = ANY(...) ORDER BY "Id" FOR UPDATE` (có theo dõi); khi `Status = Finished` nạp `Rewards`; trong `LockRoomAsync` thêm `db.MatchRewards.AddRange(snapshot.NewMatchRewards)` và `db.CoinTransactions.AddRange(snapshot.NewCoinTransactions)` trước `SaveChangesAsync` (phụ thuộc T018, T024)
- [x] T026 Sửa `FakeGameRepository` trong `backend/QuizBattle.Tests/Game/GameServiceTests.cs` (hoặc tách `backend/QuizBattle.Tests/TestHelpers/FakeGameRepository.cs`): thêm `Users`, `MatchRewards`, `CoinTransactions`; mô phỏng khóa phòng bằng `SemaphoreSlim`; áp `NewMatchRewards`/`NewCoinTransactions` sau `work` (phụ thuộc T024)

**Checkpoint**: Domain test PASS, migration sinh được, `dotnet build backend/QuizBattle.sln` và `dotnet test backend/QuizBattle.sln` qua; chưa có hành vi người dùng nhìn thấy.

---

## Phase 3: User Story 1 - Nhận XP và Xu sau trận hợp lệ (Priority: P1) 🎯 MVP

**Goal**: khi trận kết thúc bình thường, người chơi có tài khoản đủ điều kiện nhận đúng XP/Xu một lần; màn kết thúc hiển thị số nhận và lên cấp.

**Independent Test**: dựng hai tài khoản (tạo Player có `UserId` trực tiếp trong test, chưa cần luồng vào phòng của US2) chơi hết trận; kiểm tra thưởng, bất biến, không cộng đôi, kịch bản [quickstart.md](quickstart.md) 2, 5, 9, 10.

### Tests for User Story 1 (viết trước, phải FAIL)

- [x] T027 [P] [US1] Test service trong `backend/QuizBattle.Tests/Game/GameServiceTests.cs`: `AdvanceToFinished_AwardsEachEligiblePlayerOnce`, `NextQuestion_CalledTwiceAtEnd_SecondConflictAndNoDoubleReward`, `TryAutoAdvance_AfterHostFinished_ReturnsNullAndNoDoubleReward`, `AdvanceToFinished_RewardsAndStatusPersistedTogether` (lỗi ở bước ghi thưởng thì `Status` không đổi), `NextQuestionAndAutoAdvance_RunConcurrently_RewardedExactlyOnce` (`Task.WhenAll`), `GameEnded_RewardsMatchFormula`, `GameEnded_LevelUp_FlagsLeveledUp`, `GameEnded_CoinTransactionHasReasonAmountRoom`, `GameEnded_ZeroCoins_NoCoinTransactionCreated`, `GameEnded_ZeroScoreTopRank_NoRankBonusReasonZeroScore`, `GameEnded_RewardRowsCopyRoomCodeTopicFinishedAt`, `GameEnded_PlayerLeftAfterAnswering_StillRewarded`, `Finished_OldRoomWithoutRewards_NotRewardedOnRoomState`, `RoomState_Finished_ReturnsStoredRewards`
- [x] T028 [P] [US1] Test tích hợp `backend/QuizBattle.Tests/Integration/RewardSettlementTests.cs` (cần `QUIZBATTLE_TEST_DB`): `NextQuestion_TwoConcurrentCallsAtFinalQuestion_RewardedExactlyOnce` (kèm `CoinBalance == SUM(CoinTransactions.Amount)` và `TotalXp == SUM(MatchRewards.Xp)`), `TwoRoomsFinishingConcurrentlyForSameUser_NoLostUpdate` (không deadlock)
- [x] T029 [P] [US1] Test Vitest `frontend/src/realtime/rewardMessages.test.ts` cho hàm thuần `describeReward(reward)`: có thưởng, thưởng bằng 0, có/không thưởng hạng, `noRankBonusReason = 'zero_score'`, lên cấp; mọi mã `notAwardedReason` (`guest`, `host`, `no_answers`, `not_enough_accounts`) ra đúng câu tiếng Việt
- [x] T030 [P] [US1] Mở rộng `frontend/src/realtime/gameReducer.test.ts`: sự kiện `GameEnded` lưu `rewards`; `RoomState` có `rewards` được nạp vào state
- [x] T031 [P] [US1] Test component `frontend/src/components/RewardSummary.test.tsx` (`// @vitest-environment jsdom`): hiện XP/Xu/hạng thưởng/lên cấp; hiện đúng lý do khi `awarded = false`; ghi chú "không có thưởng hạng" khi `zero_score`

### Implementation for User Story 1

- [x] T032 [P] [US1] Tạo `backend/QuizBattle.Application/Game/PlayerRewardDto.cs` theo [contracts/signalr-events.md](contracts/signalr-events.md) (`PlayerId`, `Awarded`, `NotAwardedReason`, `RewardRank`, `Score`, `Xp`, `Coins`, `RankBonus`, `NoRankBonusReason`, `TotalXp`, `LevelBefore`, `LevelAfter`, `LeveledUp`); sửa `GameEndedDto.cs` thêm `Rewards`; sửa `RoomStateDto.cs` thêm `Rewards` (`null` khi chưa `Finished`)
- [x] T033 [US1] Sửa `backend/QuizBattle.Application/Game/GameService.cs` (`AdvanceFrom`, nhánh `Finished`): sau khi đặt `Finished`/`EndedAt`, gọi `RewardCalculator.Calculate` với `snapshot.Players` + số câu đã trả lời từ `snapshot.Answers` + `snapshot.Room.HostUserId`; cộng `User.TotalXp`/`CoinBalance` cho người `Awarded`; thêm `MatchReward` cho mọi Player có tài khoản (sao chép `RoomCode`, `RoomTopic`, `FinishedAt = Room.EndedAt`) vào `snapshot.NewMatchRewards`; thêm `CoinTransaction` (`Reason = MatchReward`, `Amount = Coins + RankBonus`, `RoomId`, `MatchRewardId`) khi `Amount > 0`; dựng `PlayerRewardDto` cho mọi người chơi (khách: `awarded=false`, `guest`) đưa vào `GameEndedDto`; `BuildRoomState` đọc `snapshot.Rewards` khi `Finished` (phụ thuộc T010, T025, T032) (Ghi chú khi làm: thêm guard `Status != InProgress` ở `NextQuestionAsync`, vì trước đó lần gọi thứ hai sau `Finished` chạy lại nhánh kết thúc trận; logic thưởng nằm ở `RewardSettlement.cs`.)
- [x] T034 [US1] Chạy `dotnet test backend/QuizBattle.sln` cho T027, T028 đến khi PASS; xác nhận `GameBroadcaster.NextQuestionAsync` ở `backend/QuizBattle.Api/Hubs/GameBroadcaster.cs` gửi payload mới ở cả đường chủ phòng lẫn `GameTimerService` (không cần đổi mã nếu đã gửi nguyên `outcome.GameEnded`)
- [x] T035 [P] [US1] Sửa `frontend/src/types/game.ts`: thêm `PlayerReward` (khớp `PlayerRewardDto`), `GameEnded.rewards`, `RoomState.rewards: PlayerReward[] | null`
- [x] T036 [P] [US1] Tạo `frontend/src/realtime/rewardMessages.ts`: `describeReward(reward)` thuần (phụ thuộc T035, làm T029 PASS)
- [x] T037 [US1] Sửa `frontend/src/realtime/gameReducer.ts`: lưu `rewards` từ `GameEnded` và `RoomState` (làm T030 PASS; phụ thuộc T035)
- [x] T038 [P] [US1] Tạo `frontend/src/components/RewardSummary.tsx`: nhận `PlayerReward | undefined`, dùng `describeReward`; Tailwind theo token hệ thiết kế (xem CLAUDE.md: không gradient/bóng/emoji, `tabular-nums` cho số, `accent` chỉ cho dữ liệu); làm T031 PASS (phụ thuộc T036)
- [x] T039 [US1] Sửa `frontend/src/pages/PlayPage/PlayFinished.tsx` và `frontend/src/pages/RoomLivePage/LiveFinished.tsx`: hiển thị `RewardSummary` cho `playerId` của mình (người chơi) / cho chủ phòng tự chơi (nếu có), giữ file dưới ~150 dòng (tách nhỏ nếu vượt) (phụ thuộc T037, T038) (Ghi chú khi làm: LiveFinished không đổi vì chủ phòng vào bằng JoinAsHost, không có playerId nên không có thưởng; chủ phòng tự chơi dùng PlayPage/PlayFinished.)

**Checkpoint**: US1 chạy được độc lập: trận hợp lệ cho thưởng một lần, màn kết thúc đúng, bất biến giữ.

---

## Phase 4: User Story 2 - Gắn tài khoản khi vào phòng (Priority: P1)

**Goal**: người dùng đã đăng nhập vào phòng được gắn `UserId`, vào lại dùng Player cũ (đổi thiết bị đá thiết bị cũ), token hết hạn không biến thành khách âm thầm; khách chơi như cũ.

**Independent Test**: [quickstart.md](quickstart.md) kịch bản 11, 12a–12d, 13; unit test dịch vụ + Hub.

### Tests for User Story 2 (viết trước, phải FAIL)

- [x] T040 [P] [US2] Test service trong `backend/QuizBattle.Tests/Game/GameServiceTests.cs`: `CreatePlayer_LoggedInUser_SetsUserId`, `CreatePlayer_GuestWithoutToken_UserIdNull`, `CreatePlayer_SameAccountAgain_ReusesPlayerAndRotatesToken`, `CreatePlayer_RejoinWithDifferentName_KeepsOriginalName`, `CreatePlayer_RejoinDuringInProgress_Allowed`, `CreatePlayer_NewAccountDuringInProgress_Conflict`, `CreatePlayer_UserNoLongerExists_ThrowsTokenInvalid`, `SubmitAnswer_StaleSessionHash_ThrowsSessionReplaced`, `SubmitAnswer_CurrentSessionHash_Works`, `JoinAsPlayer_ReturnsCurrentTokenHash`
- [x] T041 [P] [US2] Test `backend/QuizBattle.Tests/Api/HttpContextExtensionsTests.cs` (dùng `DefaultHttpContext`): `GetOptionalUserId_NoAuthorizationHeader_ReturnsNull`, `_ValidToken_ReturnsUserId`, `_ExpiredToken_ThrowsTokenExpired`, `_GarbageToken_ThrowsTokenInvalid`
- [x] T042 [P] [US2] Test `backend/QuizBattle.Tests/Api/PlayerConnectionRegistryTests.cs`: `NewConnectionSameHash_NoKick`, `NewConnectionDifferentHash_ReturnsOldConnection`, `DisconnectOfStaleConnection_DoesNotRemoveNewEntry`
- [x] T043 [P] [US2] Test tích hợp `backend/QuizBattle.Tests/Integration/PlayerJoinTests.cs` (cần `QUIZBATTLE_TEST_DB`): `Players_RejoinWithOtherName_DoesNotTouchNameIndex`, `JoinPlayer_TwoConcurrentFirstJoinsSameAccount_OnePlayer`
- [x] T044 [P] [US2] Mở rộng `frontend/src/api/tokenStorage.test.ts`: `loadAuth_ExpiredToken_ClearsTokenAndSetsExpiredFlag`, `loadAuth_ValidToken_DoesNotSetFlag`, `clearSessionExpiredFlag_RemovesFlag`, `logout_DoesNotSetFlag`
- [x] T045 [P] [US2] Mở rộng `frontend/src/api/apiError.test.ts`: `describeJoinAuthError_TokenExpired_ReturnsExpired`, `_TokenInvalid_ReturnsExpired`, `_Other401_ReturnsNull`; mở rộng `frontend/src/realtime/sessionErrors.test.ts` cho `session_replaced` (xóa phiên đã lưu, không tự kết nối lại)
- [x] T046 [P] [US2] Test component `frontend/src/pages/JoinPage/JoinPage.test.tsx` (`// @vitest-environment jsdom`, giả lập `createPlayer` và `AuthProvider`): đường 1 `JoinPage_FlagSetFromLoadAuth_ShowsExpiredDialogOnMount`; đường 2 `JoinPage_Server401TokenExpired_ShowsDialogAndKeepsItAfterUserBecomesNull`; `JoinPage_ExpiredDialogOpen_SubmitIsDisabledAndNothingIsSent`; `JoinPage_ChooseGuest_ClearsFlagAndClosesDialog`; `JoinPage_LoginAgainSuccess_ClearsFlag`; `JoinPage_TokenExpiresBetweenMountAndSubmit_BlocksSendAndShowsDialog`

### Implementation for User Story 2 - Backend

- [x] T047 [US2] Sửa `backend/QuizBattle.Api/Program.cs` (`JwtBearerEvents`): thêm `OnAuthenticationFailed` ghi `context.HttpContext.Items["auth.failure"]` = `"token_expired"` khi `SecurityTokenExpiredException`, ngược lại `"token_invalid"`; không log nội dung token (giữ nguyên `OnMessageReceived`)
- [x] T048 [US2] Tạo `backend/QuizBattle.Api/Extensions/HttpContextExtensions.cs`: `GetOptionalUserId(this HttpContext)` trả `null` khi không có header `Authorization`; trả `Guid` khi đã xác thực; có header mà không xác thực thì ném `UnauthorizedException(message, code)` với `code` từ `Items` (phụ thuộc T021, T047; làm T041 PASS)
- [x] T049 [US2] Sửa `backend/QuizBattle.Application/Game/IGameService.cs` + `GameService.cs` (`CreatePlayerAsync(string code, Guid? userId, CreatePlayerRequest request, ...)`): có `userId` thì nạp `User` (không tồn tại: `UnauthorizedException` code `token_invalid`); đã có Player `(RoomId, userId)` thì dùng lại, xoay `SessionTokenHash`, bỏ qua `DisplayName`, cho phép khi `Lobby` hoặc `InProgress`; chưa có thì giữ nguyên các kiểm tra cũ (phải `Lobby`, tối đa 50, tên không trùng không phân biệt hoa thường) và gán `UserId` (làm T040 PASS)
- [x] T050 [US2] Sửa `backend/QuizBattle.Application/Game/IGameRepository.cs` + `backend/QuizBattle.Infrastructure/Persistence/GameRepository.cs`: đổi `AddPlayerAsync` thành `JoinPlayerAsync` (callback có thể trả Player đã theo dõi; chỉ `Add` khi `db.Entry(player).State == Detached`); bắt `PostgresException 23505` theo `ConstraintName`: `IX_Players_RoomId_lower_DisplayName` giữ `ConflictException("Tên hiển thị đã có người dùng trong phòng.")`, thêm `IX_Players_RoomId_UserId` -> `ConflictException("Bạn đã ở trong phòng này.")`; cập nhật `FakeGameRepository` (T026) (Ghi chú khi làm: `JoinPlayerAsync` nhận thêm `userId` và nạp User đó vào `snapshot.Users` để kiểm tra "User còn tồn tại" trong khóa phòng mà không đổi constructor `GameService`.)
- [x] T051 [US2] Sửa `backend/QuizBattle.Api/Controllers/GameController.cs` (`CreatePlayer`): gọi `HttpContext.GetOptionalUserId()` rồi truyền `Guid?` vào `IGameService.CreatePlayerAsync`; giữ `[AllowAnonymous]` và `[EnableRateLimiting("join-room")]`; không thêm logic nghiệp vụ vào Controller
- [x] T052 [US2] Tạo `backend/QuizBattle.Api/Hubs/PlayerConnectionRegistry.cs` (singleton, `ConcurrentDictionary<Guid, (string ConnectionId, string TokenHash)>`) với thao tác đăng ký/gỡ có điều kiện theo `ConnectionId`; đăng ký DI trong `backend/QuizBattle.Api/Program.cs` (làm T042 PASS)
- [x] T053 [US2] Sửa `backend/QuizBattle.Application/Game/IGameService.cs` + `GameService.cs`: `JoinAsPlayerAsync` trả thêm `SessionTokenHash`; `SubmitAnswerAsync(playerId, sessionTokenHash, ...)` so với `snapshot.Player.SessionTokenHash` và ném `SessionReplacedException` khi khác (không thêm truy vấn); `backend/QuizBattle.Api/Hubs/GameHub.cs`: lưu hash vào `Context.Items["sessionTokenHash"]`, truyền vào `SubmitAnswer`, bỏ qua `LeaveRoom` khi hash không khớp, đăng ký/gỡ trong `PlayerConnectionRegistry` ở `JoinRoom`/`LeaveRoom`/`OnDisconnectedAsync`; khi `JoinRoom` mới gặp kết nối cũ với hash khác: `Groups.RemoveFromGroupAsync(oldConnectionId, code)` rồi gửi `Error { method: "JoinRoom", code: "session_replaced" }` cho kết nối cũ (phụ thuộc T023, T052) (Ghi chú khi làm: `OnDisconnectedAsync` và `LeaveRoom` của kết nối đã bị thay thế không phát `PlayerLeft`, nhờ `PlayerConnectionRegistry.Unregister` trả false.)
- [x] T054 [US2] Chạy `dotnet test backend/QuizBattle.sln` cho T040–T043 đến khi PASS

### Implementation for User Story 2 - Frontend

- [x] T055 [P] [US2] Sửa `frontend/src/api/tokenStorage.ts`: thêm khóa `quizbattle.sessionExpired` (localStorage, mọi truy cập bọc `try/catch`), `markSessionExpired()`, `isSessionExpiredFlagSet()`, `clearSessionExpiredFlag()` và cơ chế đăng ký lắng nghe thay đổi cờ; `loadAuth()` khi xóa token hết hạn gọi `markSessionExpired()` (làm T044 PASS)
- [x] T056 [P] [US2] Sửa `frontend/src/api/apiError.ts`: thêm `describeJoinAuthError(problem)` trả `'expired' | null` theo `problem.code` (`token_expired`/`token_invalid`); sửa `frontend/src/types/problemDetails.ts` thêm `code?: string`; sửa `frontend/src/types/game.ts` `GameError['code']` thêm `'session_replaced'`; sửa `frontend/src/realtime/sessionErrors.ts` (làm T045 PASS)
- [x] T057 [US2] Sửa `frontend/src/components/AuthProvider.tsx` + `frontend/src/components/authContext.ts` (`AuthContextValue`): thêm `sessionExpired: boolean` (khởi tạo từ cờ sau khi `loadAuth()` chạy; cập nhật qua đăng ký ở T055 và sự kiện `storage` cho tab khác) và `dismissSessionExpired()`; `login()`/`register()` thành công gọi `clearSessionExpiredFlag()`; `logout()` không đặt cờ; `sessionExpired` độc lập với `user` (phụ thuộc T055)
- [x] T058 [P] [US2] Tạo `frontend/src/components/SessionExpiredDialog.tsx`: hộp "Phiên đăng nhập đã hết hạn", hai nút "Đăng nhập lại" (đi `/login`, quay lại `/join?code=...` theo cơ chế `state.from` mà `ProtectedRoute` đang dùng; kiểm tra `frontend/src/components/ProtectedRoute.tsx` và `frontend/src/pages/LoginPage/LoginPage.tsx`) và "Vào làm khách"; có `role="dialog"`, tiêu điểm vào hộp, theo token hệ thiết kế; dùng `ConfirmDialog` hiện có nếu phù hợp
- [x] T059 [US2] Sửa `frontend/src/pages/JoinPage/JoinPage.tsx`: hiện `SessionExpiredDialog` khi `sessionExpired` (kể cả lúc mở trang) và vô hiệu nút "Vào phòng" khi hộp mở; `handleSubmit` gọi `loadAuth()` rồi đọc cờ trước `mutate` (cờ bật thì không gửi); `onError` với `describeJoinAuthError === 'expired'` gọi `markSessionExpired()` và không hiện lỗi form; "Vào làm khách" gọi `dismissSessionExpired()` (làm T046 PASS; phụ thuộc T056–T058)
- [x] T060 [US2] Sửa `frontend/src/realtime/GameConnectionProvider.tsx` + `frontend/src/realtime/gameReducer.ts`: xử lý `Error` `code = 'session_replaced'`: hiện thông báo "Bạn đã vào phòng từ thiết bị khác", xóa phiên đã lưu (`clearPlayerSession`), dừng tự kết nối lại, điều hướng về `/join` (phụ thuộc T056) (Ghi chú khi làm: `shouldDiscardSession` coi `session_replaced` là xóa phiên ở mọi method; `PlayPage` chuyển về `/join` kèm `state.notice`, `JoinPage` hiện thông báo; `LoginPage` giữ cả `?query` khi quay lại.)

**Checkpoint**: US2 chạy được độc lập: vào lại dùng Player cũ, đổi thiết bị đá thiết bị cũ, hết hạn không thành khách âm thầm.

---

## Phase 5: User Story 3 - Quy tắc trận hợp lệ và chủ phòng (Priority: P1)

**Goal**: thưởng chỉ khi đủ 2 tài khoản khác nhau (không tính chủ phòng) đã trả lời; chủ phòng, khách, người không trả lời không nhận; màn kết thúc nêu rõ lý do.

**Independent Test**: [quickstart.md](quickstart.md) kịch bản 6, 7, 8, 9. Luật đã nằm ở `RewardCalculator` (T010); phase này khóa hành vi qua service và giao diện.

### Tests for User Story 3 (viết trước, phải FAIL nếu thiếu mã tương ứng)

- [x] T061 [P] [US3] Test service trong `backend/QuizBattle.Tests/Game/GameServiceTests.cs`: `GameEnded_InvalidMatch_AllRewardsHaveReason`, `GameEnded_HostAsPlayer_HostRewardNotAwarded`, `GameEnded_OneAccountAndManyGuests_NoOneRewarded`, `GameEnded_GuestFirstAccountSecond_AccountGetsRank2Bonus`, `GameEnded_GuestPlayer_NoMatchRewardRow`, `GameEnded_AccountNeverAnswered_DeniedWithNoAnswers`
- [x] T062 [P] [US3] Mở rộng `frontend/src/realtime/rewardMessages.test.ts` và `frontend/src/components/RewardSummary.test.tsx`: câu hiển thị cho `guest`, `host`, `no_answers`, `not_enough_accounts` đúng ngữ cảnh người xem (người chơi vs chủ phòng tự chơi)

### Implementation for User Story 3

- [x] T063 [US3] Rà soát `backend/QuizBattle.Application/Game/GameService.cs` (T033) để T061 PASS: hạng thưởng tính trên mọi người chơi kể cả khách/chủ phòng; Player của chủ phòng nhận `MatchReward` `NotAwarded` với `DenialReason = Host`; khách không có dòng `MatchReward`
- [x] T064 [P] [US3] Hoàn thiện câu lý do tiếng Việt trong `frontend/src/realtime/rewardMessages.ts` và hiển thị trong `frontend/src/components/RewardSummary.tsx` cho đủ 4 lý do (chưa đủ 2 tài khoản đã trả lời; chủ phòng không nhận thưởng; là khách; chưa trả lời câu nào) (làm T062 PASS)
- [x] T065 [US3] Chạy `dotnet test backend/QuizBattle.sln` và `npm --prefix frontend test` đến khi PASS

**Checkpoint**: US1–US3 cùng chạy: thưởng đúng luật, lý do rõ.

---

## Phase 6: User Story 4 - Hồ sơ của tôi (Priority: P2)

**Goal**: người dùng xem cấp, tổng XP, tiến trình, số dư Xu và lịch sử thưởng; lịch sử còn nguyên khi phòng bị xóa.

**Independent Test**: [quickstart.md](quickstart.md) kịch bản 3 và 14.

### Tests for User Story 4 (viết trước, phải FAIL)

- [ ] T066 [P] [US4] Test `backend/QuizBattle.Tests/Profile/ProfileServiceTests.cs` (repo giả): `GetProfile_ComputesLevelAndProgressFromTotalXp`, `GetProfile_NewUser_Level1ZeroXpZeroCoins`, `GetRewards_OnlyAwardedNewestFirst`, `GetRewards_LimitOutOfRange_Validation`, `GetProfile_UserNotFound_NotFound`
- [ ] T067 [P] [US4] Test tích hợp `backend/QuizBattle.Tests/Integration/ProfileRewardsTests.cs` (cần `QUIZBATTLE_TEST_DB`): `DeleteFinishedRoom_RewardsKeptAndProfileAndInvariantsUnchanged` (gọi `RoomService.DeleteRoomAsync`; `MatchRewards` còn với `PlayerId`/`RoomId` NULL; `GET me/rewards` vẫn trả `roomCode`/`topic`/`playedAt`; `TotalXp == SUM(MatchRewards.Xp)` và `CoinBalance == SUM(CoinTransactions.Amount)` trước và sau)
- [ ] T068 [P] [US4] Test Vitest cho hàm thuần hiển thị tiến trình (nếu có) và `frontend/src/pages/ProfilePage/ProfilePage.test.tsx` (`// @vitest-environment jsdom`): đang tải, lỗi (nút thử lại), rỗng (chưa có trận), có dữ liệu (cấp, XP, thanh tiến trình, Xu, danh sách)

### Implementation for User Story 4

- [ ] T069 [P] [US4] Tạo DTO trong `backend/QuizBattle.Application/Profile/`: `ProfileDto` (`userId`, `displayName`, `level`, `totalXp`, `xpIntoLevel`, `xpForNextLevel`, `coinBalance`) và `RewardHistoryItemDto` (`roomCode`, `topic`, `playedAt`, `score`, `rewardRank`, `xp`, `coins`, `rankBonus`) khớp [contracts/rest-api.md](contracts/rest-api.md)
- [ ] T070 [US4] Sửa `backend/QuizBattle.Application/Auth/IUserRepository.cs` + `backend/QuizBattle.Infrastructure/Persistence/UserRepository.cs`: truy vấn `AsNoTracking`, projection một câu SQL cho hồ sơ và cho lịch sử từ chính `MatchRewards` (`WHERE UserId = @id AND Status = 'Awarded' ORDER BY FinishedAt DESC LIMIT @limit`), không join `Rooms`/`Players`
- [ ] T071 [US4] Tạo `backend/QuizBattle.Application/Profile/IProfileService.cs` + `ProfileService.cs` (dùng `LevelCalculator`), đăng ký DI trong `backend/QuizBattle.Application/DependencyInjection.cs`; `limit` 1–50 mặc định 20, ngoài khoảng trả lỗi validate theo ProblemDetails (làm T066 PASS)
- [ ] T072 [US4] Tạo `backend/QuizBattle.Api/Controllers/UsersController.cs`: `[Authorize] GET /api/users/me` và `GET /api/users/me/rewards?limit=`; Controller chỉ gọi `IProfileService`; làm T067 PASS
- [ ] T073 [P] [US4] Tạo `frontend/src/types/profile.ts` (`Profile`, `RewardHistoryItem`), `frontend/src/api/profile.ts` (`getProfile`, `getRewards` qua `request()`), `frontend/src/hooks/useProfile.ts` (TanStack Query, không lưu bản sao vào `useState`)
- [ ] T074 [US4] Tạo `frontend/src/pages/ProfilePage/ProfilePage.tsx` (+ tách `ProfileSummary.tsx`, `RewardHistory.tsx` nếu vượt ~150 dòng): đủ 3 trạng thái tải/lỗi/rỗng, thanh tiến trình dùng `bg-accent` (dữ liệu), số dùng `tabular-nums`, responsive điện thoại; làm T068 PASS
- [ ] T075 [US4] Sửa `frontend/src/App.tsx`: thêm route `/profile` bọc `ProtectedRoute`; sửa header `frontend/src/pages/DashboardPage/DashboardPage.tsx` thêm liên kết tới hồ sơ (không dùng `accent` cho link)

**Checkpoint**: US4 chạy độc lập; xóa phòng không làm mất lịch sử.

---

## Phase 7: User Story 5 - Cấp độ trong phòng và bảng xếp hạng (Priority: P2)

**Goal**: cấp độ cạnh tên ở sảnh chờ và bảng điểm; bảng xếp hạng top 50 theo tổng XP.

**Independent Test**: [quickstart.md](quickstart.md) kịch bản 1 và 4.

### Tests for User Story 5 (viết trước, phải FAIL)

- [ ] T076 [P] [US5] Test service trong `backend/QuizBattle.Tests/Game/GameServiceTests.cs`: `RoomState_AccountPlayer_HasLevel`, `RoomState_Guest_LevelNull`, `PlayerJoined_AccountPlayer_HasLevel`, `Leaderboard_EntriesCarryLevel`
- [ ] T077 [P] [US5] Test `backend/QuizBattle.Tests/Profile/XpLeaderboardServiceTests.cs` (repo giả): `GetTop_ExcludesZeroXp`, `GetTop_OrderedByXpDescThenCreatedAt`, `GetTop_Max50`, `GetTop_ComputesLevel`; và tích hợp `backend/QuizBattle.Tests/Integration/XpLeaderboardTests.cs` (cần `QUIZBATTLE_TEST_DB`): `XpLeaderboard_ExcludesZeroXpAndReturnsTop50InOrder`
- [ ] T078 [P] [US5] Test component `frontend/src/pages/XpRankingPage/XpRankingPage.test.tsx` (`// @vitest-environment jsdom`): đang tải, lỗi, rỗng, có dữ liệu; và `frontend/src/components/LevelBadge.test.tsx`: hiện cấp, không hiện gì khi `level = null`

### Implementation for User Story 5

- [ ] T079 [US5] Sửa DTO `backend/QuizBattle.Application/Game/PlayerSummaryDto.cs`, `PlayerJoinedDto.cs`, `LeaderboardEntryDto.cs`: thêm `Level` (`int?`, `null` với khách); sửa `GameService.cs` (`BuildRoomState`, `BuildLeaderboard`, `JoinAsPlayerAsync`) dùng `LevelCalculator` trên `snapshot.Users` (làm T076 PASS; phụ thuộc T025)
- [ ] T080 [P] [US5] Tạo `backend/QuizBattle.Application/Profile/XpLeaderboardEntryDto.cs`, thêm vào `IProfileService`/`ProfileService` hàm lấy top 50 (`displayName`, `level`, `totalXp`, `rank` = thứ tự 1..N); sửa `UserRepository`: `WHERE TotalXp > 0 ORDER BY TotalXp DESC, CreatedAt, Id LIMIT 50`, `AsNoTracking`, projection (làm T077 PASS)
- [ ] T081 [US5] Tạo `backend/QuizBattle.Api/Controllers/LeaderboardsController.cs`: `[AllowAnonymous] GET /api/leaderboards/xp`, chỉ trả `displayName`, `level`, `totalXp`, `rank` (không email, không Id người dùng)
- [ ] T082 [P] [US5] Sửa `frontend/src/types/game.ts`: thêm `level: number | null` vào `PlayerSummary`, `LeaderboardEntry`, `PlayerJoined`; tạo `frontend/src/types/profile.ts` thêm `XpLeaderboardEntry`; tạo `frontend/src/api/leaderboards.ts` và `frontend/src/hooks/useXpLeaderboard.ts`
- [ ] T083 [P] [US5] Tạo `frontend/src/components/LevelBadge.tsx` (chữ nhỏ "Cấp N", không chỉ dựa vào màu, không dùng `accent`); sửa `frontend/src/components/PlayerList.tsx` và `frontend/src/components/Leaderboard.tsx` hiển thị `LevelBadge` cạnh tên; kiểm tra `frontend/src/pages/PlayPage/PlayLobby.tsx` và `frontend/src/pages/RoomLivePage/LiveLobby.tsx` truyền đủ dữ liệu; sửa `frontend/src/realtime/gameReducer.ts` để `PlayerJoined` mang `level`
- [ ] T084 [US5] Tạo `frontend/src/pages/XpRankingPage/XpRankingPage.tsx` (đủ 3 trạng thái; số `tabular-nums`; responsive); sửa `frontend/src/App.tsx` thêm route `/ranking` (công khai); thêm liên kết từ `frontend/src/pages/HomePage/HomePage.tsx` hoặc header dùng chung (làm T078 PASS)

**Checkpoint**: US5 chạy độc lập.

---

## Phase 8: User Story 6 - Trang /join báo trước về thưởng (Priority: P3)

**Goal**: đã đăng nhập thì /join điền sẵn tên và nói trận có thể được tính thưởng; chưa đăng nhập thì gợi ý khách không nhận thưởng.

**Independent Test**: [quickstart.md](quickstart.md) kịch bản 1.

### Tests for User Story 6 (viết trước, phải FAIL)

- [ ] T085 [P] [US6] Mở rộng `frontend/src/pages/JoinPage/JoinPage.test.tsx`: `JoinPage_LoggedIn_PrefillsDisplayNameEditableAndShowsRewardNote`, `JoinPage_LoggedIn_RewardNoteDoesNotPromiseReward` (nêu điều kiện ≥ 2 tài khoản), `JoinPage_Guest_ShowsGuestNoRewardHint`, `JoinPage_UserTypesNewName_UsesTypedName`

### Implementation for User Story 6

- [ ] T086 [US6] Sửa `frontend/src/pages/JoinPage/JoinPage.tsx`: khởi tạo `displayName` từ `user.displayName` khi đã đăng nhập (đồng bộ khi `AuthProvider` xong `isLoading`, không ghi đè khi người dùng đã gõ), thêm dòng thông báo tính thưởng ("XP và Xu được tính khi trận có ít nhất 2 tài khoản tham gia trả lời") hoặc gợi ý khách không nhận thưởng; sửa `frontend/src/validation/joinForm.ts` nếu cần (làm T085 PASS)

**Checkpoint**: cả 6 story hoạt động.

---

## Phase 9: Polish & cross-cutting

**Purpose**: tài liệu quy tắc, kiểm chứng migration production, rà soát chất lượng.

- [ ] T087 [P] Sửa `CLAUDE.md`: (1) danh sách mã `Error` thêm `session_replaced` ("`code` là `unauthorized` | `not_found` | `conflict` | `session_replaced` | `unknown`" và cách client xử lý); (2) dòng "Test: ... Vitest (frontend, chỉ cho logic)" nêu thêm test component bằng `jsdom` + `@testing-library/react` cho màn có trạng thái; (3) ghi chú Users khóa theo `Id` khi kết thúc trận để tính năng 2 (cửa hàng) tuân theo
- [ ] T088 [P] Kiểm tra `backend/QuizBattle.Api/Hubs/GameHubEvents.cs` và `frontend/src/realtime/events.ts`: tên sự kiện không đổi; cập nhật comment nếu cần
- [ ] T089 Kiểm chứng migration production theo [quickstart.md](quickstart.md) mục 5: dựng DB dev ở trạng thái trước migration kèm dữ liệu mẫu (User, Room `Finished`, Player khách), chạy `scripts/migrate-prod.ps1` (sinh `scripts/out/migrate-prod.sql`), áp SQL bằng `psql` hai lần, kiểm tra dữ liệu cũ nguyên vẹn, 2 CHECK, `IX_Users_TotalXp`, `IX_Players_RoomId_UserId`, và thử vi phạm `CoinBalance = -1`; ghi kết quả vào mô tả PR (Ghi chú: DB dev đã áp migration `AddXpCoinsAndPlayerAccounts`, nên T089 PHẢI dùng một DB riêng: tạo DB mới, migrate tới `UseCaseInsensitivePlayerName` (migration ngay trước migration mới), nạp dữ liệu mẫu, rồi mới chạy `scripts/migrate-prod.ps1`. Không chạy trên DB dev hiện tại.)
- [ ] T090 Chạy toàn bộ `quickstart.md` mục 2 (kịch bản 1–14), mục 3 (truy vấn bất biến) và mục 4 (đồng thời) trên môi trường dev; ghi kết quả
- [ ] T091 [P] Rà soát CLAUDE.md "Giao diện": chạy skill `design-taste-frontend` `lint.mjs` và `inspect.mjs` cho `ProfilePage`, `XpRankingPage`, `RewardSummary`, `SessionExpiredDialog`, `JoinPage` nếu máy đã cài skill; sửa hết FAIL
- [ ] T092 Chạy `dotnet build backend/QuizBattle.sln`, `dotnet test backend/QuizBattle.sln` (có đặt `QUIZBATTLE_TEST_DB` để chạy cả test tích hợp), `npm --prefix frontend run build`, `npm --prefix frontend test`, `npm --prefix frontend run lint`; kiểm tra mã thoát trước khi push (không pipe build/test qua `tail`/`head` khi nối `&&` với `git push`)
- [ ] T093 Đề xuất commit message Conventional Commits cho từng nhóm task (domain, schema, kết thúc trận, vào phòng, hồ sơ, xếp hạng, /join, tài liệu), giải thích ngắn thiết kế để trả lời phỏng vấn

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: bắt đầu ngay. T005 phụ thuộc T004.
- **Foundational (Phase 2)**: sau Phase 1 (riêng nhóm Domain T006–T010 không cần Phase 1). **Chặn mọi user story.**
- **US1 (P1)**: sau Phase 2. MVP.
- **US2 (P1)**: sau Phase 2; độc lập US1 về mã (cùng sửa `GameService.cs`/`GameRepository.cs`/`GameHub.cs`/`JoinPage.tsx`, nên làm tuần tự để tránh xung đột). Khi gộp với US1 thì thưởng mới gắn với người chơi thật.
- **US3 (P1)**: sau US1 (kiểm thử hành vi trên luồng thưởng).
- **US4 (P2)**: sau Phase 2; test `DeleteFinishedRoom...` cần dữ liệu thưởng tạo bằng test (không cần US2).
- **US5 (P2)**: sau Phase 2; T079 sửa cùng file với US1 (`GameService.cs`, `types/game.ts`, `gameReducer.ts`) nên làm sau US1.
- **US6 (P3)**: sau US2 (cùng `JoinPage.tsx`).
- **Polish (Phase 9)**: sau các story cần có.

### Within each story

Test viết trước và FAIL → DTO/entity → repository → service → Controller/Hub → frontend (type → api → hook → component → page → route).

### Parallel Opportunities

- Phase 1: T001, T002, T003, T004 song song.
- Phase 2: T006, T007, T009, T011, T012, T013, T014, T015, T016, T017, T021, T022 song song (khác file); T008 sau T007; T010 sau T006+T009; T018 sau T012+T013; T019 sau mọi entity/config.
- Trong mỗi story: các task test `[P]` chạy song song; frontend và backend của cùng story làm song song sau khi hợp đồng ở [contracts/](contracts/) đã chốt.
- US4 và US5 (backend lẫn frontend) làm song song nếu có hai người, trừ các file dùng chung đã nêu.

### Parallel Example: User Story 1

```text
Song song (test, khác file):
  T027 GameServiceTests.cs   T028 RewardSettlementTests.cs   T029 rewardMessages.test.ts
  T030 gameReducer.test.ts   T031 RewardSummary.test.tsx
Sau đó song song (khác file):
  T032 PlayerRewardDto.cs    T035 types/game.ts
Rồi tuần tự: T033 GameService -> T034 chạy test -> T036/T037/T038 -> T039
```

---

## Implementation Strategy

### MVP trước (US1)

1. Phase 1 → Phase 2 → US1 → **dừng và kiểm chứng**: thưởng đúng một lần, bất biến, màn kết thúc.
2. Chưa có US2 thì kiểm thử US1 bằng Player có `UserId` tạo trực tiếp trong test; không thể thử bằng giao diện (vì chưa gắn tài khoản khi vào phòng). Vì vậy US1 + US2 nên được hoàn thành liền nhau trước khi demo.

### Giao từng phần

1. Foundation (schema + Domain) → US1 + US2 + US3 (thưởng đúng và an toàn) → US4 (hồ sơ) → US5 (cấp độ, xếp hạng) → US6 (/join) → Polish.
2. Mỗi story build + test sạch rồi mới commit; commit nhỏ, Conventional Commits, nhánh `feat/player-xp-coins`.
3. Migration lên production chạy thủ công có chủ đích, **trước** khi deploy backend mới (xem [research.md R8](research.md#r8-migration-và-triển-khai-production)).

---

## Notes

- Tổng số task: 93. Số task theo phase: Setup 5, Foundational 21, US1 13, US2 21, US3 5, US4 10, US5 9, US6 2, Polish 7.
- `[P]` = khác file và không phụ thuộc task chưa xong.
- Không thêm thư viện runtime nào; devDependencies frontend chỉ `jsdom`, `@testing-library/react` (đã duyệt).
- Mọi giá trị ràng buộc (độ dài, nullable, CHECK, `ON DELETE`) trích nguyên văn từ [data-model.md](data-model.md); khi lệch thì data-model thắng.
- Không chạy `/speckit-implement` trong bước này.
