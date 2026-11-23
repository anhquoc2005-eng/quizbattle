# Tasks: Chế độ ôn tập

**Input**: Design documents from `/specs/005-practice-mode/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/practice-api.md, quickstart.md

**Tests**: Có — bắt buộc theo CLAUDE.md và yêu cầu cố vấn (spec SC-001…SC-005). Tích hợp dùng Postgres thật (`DatabaseFixture` + `RequiresDatabaseFact`, đặt `QUIZBATTLE_TEST_DB`); HTTP qua `ApiFactory`; "mutation" là test độ nhạy bằng cờ `internal` (mẫu `ShopPurchaseSensitivityTests`), không thêm thư viện.

**Organization**: 5 ĐỢT. Mỗi đợt kết thúc bằng checkpoint build + test xanh và push độc lập (commit nhỏ Conventional Commits trong đợt vẫn khuyến khích). Nhãn [US1…US5] ánh xạ user story trong spec.md; Ch1–Ch10 là các điểm đã chốt (bảng ở spec.md); C1–C11 là lối cày (plan.md); M1–M7 là mutation (plan.md).

**Tổng: 46 task T001–T046** (Đợt 1: T001–T009, Đợt 2: T010–T030, Đợt 3: T031–T034, Đợt 4: T035–T042, Đợt 5: T043–T046). Checkpoint: T009, T030, T034, T042, T046.

## Format: `[ID] [P?] [Story] Mô tả + đường dẫn file`

- **[P]**: chạy song song được (khác file, không phụ thuộc task chưa xong)

---

## Đợt 1: Domain, cấu hình, schema, bất biến (Foundational — chặn mọi đợt sau)

**Purpose**: Luật thưởng thuần, cấu hình kiểm tra lúc khởi động, 3 bảng + migration, bất biến XP mới được ghi vào quy tắc và test. Chưa có repository, chưa có endpoint.

- [x] T001 [P] [US3] Domain: `backend/QuizBattle.Domain/PracticeRewardCalculator.cs` — hàm thuần `Calculate(bool isCorrect, bool alreadyRewardedTodayForQuestion, int xpPerCorrect) → (int DesiredXp, PracticeXpReason? Reason)`: sai → `(0, Wrong)`; đúng nhưng đã nhận hôm nay → `(0, AlreadyToday)`; đúng → `(xpPerCorrect, null)`. Enum `PracticeXpReason { Wrong, AlreadyToday, DailyCap, DailyCapPartial }` kèm ánh xạ chuỗi (`wrong`, `already_today`, `daily_cap`, `daily_cap_partial`). Không đọc DB, đồng hồ. Thêm hàm thuần `ApplyCap(int desired, int usedToday, int cap) → (int Granted, PracticeXpReason? Reason)`: `granted = Min(desired, Max(cap - used, 0))`; `granted == desired` → reason giữ nguyên; `0 < granted < desired` → `DailyCapPartial`; `granted == 0 && desired > 0` → `DailyCap`. (FR-006, Ch2, Ch3)
- [x] T002 [P] [US3] Test `backend/QuizBattle.Tests/Domain/PracticeRewardCalculatorTests.cs`: `Calculate_WrongAnswer_ReturnsZeroWithWrong`; `Calculate_CorrectAlreadyRewardedToday_ReturnsZeroWithAlreadyToday`; `Calculate_Correct_ReturnsXpPerCorrect`; `Calculate_XpPerCorrectZero_ReturnsZeroWithNullReason`; `ApplyCap_RemainingLessThanDesired_GrantsRemainderWithPartial` (7/10 sau 7 → 3); `ApplyCap_CapReached_ReturnsZeroWithDailyCap`; `ApplyCap_CapZero_ReturnsZeroWithDailyCap`; `ApplyCap_UsedAboveCap_ReturnsZero` (hạ trần giữa ngày, không âm). (FR-006, Ch3)
- [x] T003 [P] [US3] Cấu hình: `backend/QuizBattle.Application/Practice/PracticeOptions.cs` (`XpPerCorrect` mặc định 5, `DailyXpCap` mặc định 100), bind trong DI với `ValidateOnStart` kiểm `>= 0` (thông báo lỗi nêu tên khóa). KHÔNG thêm vào `ProductionConfigValidator`. Thêm `Practice__XpPerCorrect`, `Practice__DailyXpCap` vào `.env.example` (không giá trị thật). (FR-010, Ch2, Ch9)
- [x] T004 [P] [US3] Test `backend/QuizBattle.Tests/Practice/PracticeOptionsTests.cs`: `Options_NegativeXpPerCorrect_FailsOnStartWithClearMessage`; `Options_NegativeDailyXpCap_FailsOnStart`; `Options_Zero_IsValid`; `Options_Missing_UsesDefaults5And100`; khẳng định `ProductionConfigValidator` không yêu cầu hai khóa này (thiếu vẫn hợp lệ). (FR-010, Ch9)
- [x] T005 [P] [US2] Entity + cấu hình EF: `backend/QuizBattle.Domain/Entities/PracticeSession.cs`, `PracticeAnswer.cs`, `PracticeXpDaily.cs`; `backend/QuizBattle.Infrastructure/Persistence/Configurations/Practice*Configuration.cs`; thêm `DbSet`. Theo `data-model.md`: FK `UserId` CASCADE, `RoomId`/`QuestionId`/`SessionId` SET NULL; `QuestionIds` kiểu `uuid[]`; unique một phần lượt mở `IX_PracticeSessions_OpenPerUserRoom` (`WHERE "CompletedAt" IS NULL`); unique `(SessionId, QuestionId)`; unique một phần `(UserId, QuestionId, Day) WHERE "XpAwarded" > 0`; `CK_PracticeAnswers_XpReason` (một CHECK gom giá trị và quan hệ); CHECK `SelectedIndex` 0..3, `XpAwarded >= 0`, `PracticeXpDaily.Xp >= 0`. `XpReason` lưu chuỗi. (FR-011, FR-014, Ch1, Ch4, Ch7)
- [x] T006 [US2] Migration `AddPracticeMode` trong `backend/QuizBattle.Infrastructure/Persistence/Migrations/`: tạo bằng `dotnet ef migrations add`, ĐỌC LẠI file sinh ra (chỉ `CreateTable` 3 bảng, mọi index/CHECK có mặt đúng biểu thức, không đụng bảng có sẵn); áp lên Postgres Docker dev. Sau T005. (Ch1)
- [x] T007 [US2] Test tích hợp `backend/QuizBattle.Tests/Integration/PracticeSchemaTests.cs` (mẫu `SchemaConstraintsTests`): `XpReason_NullWithWrongAnswer_ViolatesCheck`; `XpReason_WrongWithXp_ViolatesCheck`; `XpReason_UnknownValue_ViolatesCheck`; `XpReason_PartialWithZeroXp_ViolatesCheck`; `XpReason_NullWithCorrectAnswer_Allowed` (kể cả `XpAwarded = 0`); `PracticeAnswers_DuplicateSessionQuestion_ViolatesUnique`; `PracticeAnswers_SameUserQuestionDayTwiceWithXp_ViolatesPartialUnique` và `..._SecondWithZeroXp_Allowed`; `PracticeSessions_TwoOpenForSameUserRoom_ViolatesPartialUnique`, `..._OneOpenOneCompleted_Allowed`, `..._NullRoomId_Allowed`; `DeleteRoom_KeepsPracticeAnswersWithNullRoomAndQuestion`; `DeleteUser_CascadesPracticeRows`. Sau T006. (FR-005, FR-011, FR-014, Ch4, Ch7)
- [x] T008 [US3] Bất biến XP mới: (a) helper dùng chung `backend/QuizBattle.Tests/TestHelpers/XpInvariant.cs` — `AssertAsync(db, userIds)` kiểm `TotalXp = SUM(MatchRewards.Xp) + SUM(PracticeAnswers.XpAwarded)`, `PracticeXpDaily.Xp = SUM(XpAwarded)` theo `(UserId, Day)`, `CoinBalance = SUM(CoinTransactions.Amount)`; (b) sửa `RewardSettlementTests.cs:82-91` và `ProfileRewardsTests.cs:45` gọi helper thay vì tính tay; (c) sửa `CLAUDE.md:60` thành `TotalXp = tổng MatchRewards.Xp + tổng PracticeAnswers.XpAwarded` (một dòng, nêu nguồn mới); (d) test `XpInvariant_UserWithMatchAndPracticeRows_Holds` seed tay một user có cả hai nguồn. Sau T006. (FR-015, Ch1, research R8)
- [x] T009 **Checkpoint Đợt 1**: build + toàn bộ test xanh (kể cả tích hợp với `QUIZBATTLE_TEST_DB`); commit `feat: schema ôn tập, luật thưởng thuần và bất biến XP hai nguồn` + push.

---

## Đợt 2: Repository, service, bằng chứng đồng thời (US1–US5)

**Purpose**: Toàn bộ nghiệp vụ chạy đúng trên Postgres thật, kể cả đồng thời, idempotent và bị mutation bắt được. Chưa có endpoint.

- [x] T010 [US2] Application contracts: `backend/QuizBattle.Application/Practice/` — `IPracticeService`, `IPracticeRepository` (`ListRoomsAsync`, `CreateOrGetSessionAsync`, `GetSessionAsync`, `SubmitAnswerAsync`, `GetTodayXpAsync`), DTO theo `contracts/practice-api.md`: `PracticeRoomsResponse`, `PracticeSessionDto`, `PracticeQuestionDto` (KHÔNG khai báo `CorrectIndex`), `PracticeResultDto` (dùng cho `lastResult`), `PracticeAnswerResultDto`, `PracticeSummaryDto`. Ngoại lệ dùng lại `NotFoundException`/`ConflictException` (mã `not_current_question`). (FR-004, C5)
- [x] T011 [P] [US2] Test `backend/QuizBattle.Tests/Practice/PracticeQuestionDtoTests.cs`: `PracticeQuestionDto_HasNoCorrectIndexProperty` (phản chiếu mọi thuộc tính, tên không chứa `correct`, không nhận `CorrectIndex`); `PracticeSessionDto_Serialized_DoesNotContainCorrectIndexForCurrent` (JSON của `current` không có khóa `correctIndex`). (FR-004, SC-001, C5)
- [x] T012 [US1] Repo `ListRoomsAsync` trong `backend/QuizBattle.Infrastructure/Persistence/PracticeRepository.cs`: MỘT truy vấn projection `AsNoTracking`: `Finished` AND (host OR có `Player.UserId = me`) AND `EXISTS MatchRewards Awarded` của phòng AND có câu `Approved` + `EndedAt != null`; kèm `questionCount`, `role`, `openSessionId` (LEFT JOIN lượt mở); sắp xếp mới nhất trước. Không N+1. (FR-002, Ch5, C4)
- [x] T013 [US4] Repo `CreateOrGetSessionAsync`: service chọn câu và xáo ngoài transaction (hàm thuần nhận `Random`/seed để test được); repo mở transaction READ COMMITTED TƯỜNG MINH, chạy `INSERT ... ON CONFLICT ("UserId","RoomId") WHERE "CompletedAt" IS NULL DO NOTHING RETURNING "Id"` bằng `DbCommand` thô; nếu không có dòng thì `SELECT "Id"` bằng câu lệnh RIÊNG rồi trả (Id, created=false). Cờ test `internal bool UseOnConflict = true` (M7: false → INSERT thường). Lý do ghi comment một dòng, dẫn research R11. (FR-011, Ch7, C10)
- [x] T014 [US2] Repo `GetSessionAsync`: đọc lượt (ràng buộc `UserId`), câu hiện tại theo `QuestionIds` bỏ qua câu không còn tồn tại, `answeredCount`, `lastResult` (dòng `PracticeAnswers` có `AnsweredAt` mới nhất; `correctIndex` đọc từ `Questions`), `summary` khi hoàn tất; nếu không còn câu nào để làm mà `CompletedAt` null thì ghi `CompletedAt = now` (chỉ ghi lượt, không đụng XP). (FR-012, FR-013, Ch8, C6)
- [x] T015 [US3] Repo `SubmitAnswerAsync` đúng luồng `plan.md` ("Luồng nộp đáp án"): transaction → `SELECT Users FOR UPDATE` → kiểm lượt thuộc user và câu thuộc lượt (404) → nếu đã có `PracticeAnswers (SessionId, QuestionId)` thì trả kết quả đã lưu (replay, không ghi gì) → kiểm câu hiện tại (409 `not_current_question`) → chấm → `PracticeRewardCalculator.Calculate` → câu SQL giữ chỗ `LEAST` (bỏ qua khi `desired == 0` hoặc `cap == 0`) với `granted = Xp trả về − Xp đọc trước` → `ApplyCap` suy lý do cuối → INSERT `PracticeAnswers` → `TotalXp += granted` → đóng lượt nếu hết câu → SaveChanges → Commit. KHÔNG viết nhánh bắt unique-violation (research R12). Không ghi `CoinTransaction`. Cờ `internal` chỉ test dùng (mặc định không làm gì), mẫu `AvatarRepository.cs:11-16`: `UseUserRowLock`, `ReserveVariant` (`Normal`, `NoWhere` M1, `NoLeast` M2, `RejectWhole` M3), `SkipAlreadyTodayCheck` (M5), `SkipReplay` (M6), `BeforeXpWrite` (hook ép xen kẽ / ép lỗi). (FR-005…FR-008, Ch3, Ch4, C2, C3, C11)
- [x] T016 [US2] `PracticeService` + đăng ký DI: lấy `now` từ `TimeProvider`, `day = VietnamDay.DayOf(now)` (dùng nguyên chỗ `Domain.AiQuota`, Ch6), đọc `PracticeOptions`, điều phối repo, map sang DTO, tính `xpGainedToday`/`remainingToday`/`resetAt` (`VietnamDay.NextResetAt`). Controller/Hub chưa tạo. (FR-006, Ch6)
- [x] T017 [US1] Test tích hợp `backend/QuizBattle.Tests/Integration/PracticeEligibilityTests.cs`: `ListRooms_PlayerAndHostRooms_Listed`; `ListRooms_UnrelatedRoom_NotListed`; `ListRooms_RoomNotFinished_NotListed`; `ListRooms_FinishedRoomWithoutAwardedReward_NotListed` (phòng một mình); `ListRooms_GuestPlayerOnly_NotListed`; `ListRooms_OnlyApprovedEndedQuestionsCounted`; `ListRooms_RoomWithNoPlayedQuestions_NotListed`; `ListRooms_IncludesOpenSessionId`; `CreateSession_IneligibleRoom_ReturnsNotFoundSameAsMissingRoom`. (FR-002, Ch5, C4, US1)
- [x] T018 [US3] Test tích hợp `backend/QuizBattle.Tests/Integration/PracticeSubmitTests.cs` (luật cơ bản): `Submit_CorrectAnswer_AwardsXpAndWritesRowAndUpdatesTotalXp`; `Submit_WrongAnswer_ZeroXpWithWrongReason`; `Submit_SameQuestionNextSessionSameDay_ZeroXpAlreadyToday`; `Submit_SameQuestionNextDay_AwardsAgain` (hai `now` hai bên 00:00 VN); `Submit_Correct_DoesNotCreateCoinTransactionOrChangeCoinBalance`; `Submit_OutOfOrderUnansweredQuestion_Throws409NotCurrent`; `Submit_QuestionNotInSession_Throws404`; `Submit_OtherUsersSession_Throws404`; `Submit_LastQuestion_CompletesSessionAndSummaryCorrect`. (FR-005…FR-008, Ch2, Ch4, C6)
- [x] T019 [US3] Test `PracticeCapTests.cs` (cùng thư mục): `Cap_ParallelSubmitsOverCap_NeverExceedsCap` — N = 30 request đồng thời (mỗi tác vụ một DbContext/connection riêng, nhiều câu khác nhau, nhiều lượt) với cap 100/5 XP: tổng `PracticeXpDaily.Xp = 100`, `TotalXp` khớp bất biến; `Cap_RemainingLessThanQuestionXp_GrantsRemainderThenZero` (cấu hình 7/10: 7 → `null`, 3 → `daily_cap_partial`, 0 → `daily_cap`; `PracticeXpDaily.Xp = 10`); `Cap_Zero_NoXpAndNoDailyRow`; `Cap_XpPerCorrectZero_CorrectAnswerNullReasonZeroXp`; `Cap_NewDay_UsesSeparateCounterRow`. (FR-006, SC-002, Ch3, C3)
- [x] T020 [US2] Test `PracticeIdempotencyTests.cs`: `Submit_SameQuestionAgain_ReturnsStoredResultAndDoesNotAddXp`; `Submit_AgainWithDifferentSelectedIndex_StillReturnsFirstResult` (đầu sai, sau gửi đúng → vẫn sai, `wrong`, XP 0); `Submit_AgainAfterSessionCompleted_ReturnsStoredResult`; `Submit_TwoParallelRequestsSameQuestion_BothSucceedWithSameResultAndXpOnce` (2 request đồng thời, cả hai thành công cùng `isCorrect/correctIndex/xpAwarded/xpReason`, đúng 1 dòng `PracticeAnswers`, XP một lần, bất biến đúng); `Submit_Replay_ReportsCurrentXpGainedTodayNotFrozen`; `Submit_UnansweredNonCurrentQuestion_StillConflict`. Không có test nào kỳ vọng `already_answered`. (FR-005, SC-003, C2, C11)
- [x] T021 [US4] Test `PracticeSessionTests.cs`: `CreateSession_Twice_ReturnsSameSessionIdSecondNotCreated`; `CreateSession_TwoParallelRequests_ReturnSameSessionId` (một created, một không; đúng 1 dòng mở trong DB); `CreateSession_AfterCompleted_CreatesNewSession`; `CreateSession_DifferentRooms_BothOpenAllowed`; `GetSession_AfterSubmit_ReturnsLastResultWithCorrectIndex`; `GetSession_NoAnswerYet_LastResultNull`; `GetSession_CurrentQuestionHasNoCorrectIndexInRawJson`; `Session_QuestionDeletedMidway_EndsWithSummaryByAnsweredCount` (xóa phòng/câu giữa lượt, Ch8); `Session_OrderIsStoredNotReshuffledOnRead`. (FR-003, FR-011…FR-013, SC-004, Ch7, Ch8, C10)
- [x] T022 [US3] Test `PracticeInvariantTests.cs`: `Invariant_MixedMatchAndPracticeSequence_Holds` (chơi trận qua settlement thật + ôn nhiều lượt, gọi `XpInvariant.AssertAsync`); `Invariant_RoomDeletedAfterPractice_RowsKeptAndInvariantHolds`; `Submit_FailureAfterCounterIncrement_RollsEverythingBack` (hook `BeforeXpWrite` ném lỗi: bộ đếm, dòng XP, `TotalXp` đều không đổi); `Submit_ConcurrentWithMatchSettlementOfSameUser_NoLostUpdateNoDeadlock` (kết thúc trận cộng XP cùng lúc user ôn; timeout cứng 30s, không `40P01`/`40001`, bất biến đúng). (FR-007, FR-015, research R4, R7)
- [x] T023 [US3] **M1**: trong `PracticeSensitivityTests.cs` — `Mutation_NoWhereInReserveSql_CapTestTurnsRed`: bật `ReserveVariant.NoWhere` rồi chạy bài trần đồng thời của T019 → PHẢI đỏ (`Xp > cap`); tắt cờ → xanh. Ghi quan sát vào `quickstart.md` mục "Bằng chứng mutation". (SC-005, C3)
- [x] T024 [US3] **M2**: `Mutation_NoLeast_RemainderTestTurnsRed`: `ReserveVariant.NoLeast` + cấu hình 7/10 → bài "Cap_RemainingLessThanQuestionXp" PHẢI đỏ (`Xp = 14`); tắt cờ → xanh. (Ch3, SC-005)
- [x] T025 [US3] **M3**: `Mutation_RejectWholeQuestion_RemainderTestTurnsRed`: `ReserveVariant.RejectWhole` + cấu hình 7/10 → PHẢI đỏ (kỳ vọng 7 + 3, quan sát 7 + 0); tắt cờ → xanh. (Ch3, SC-005)
- [x] T026 [US3] **M4**: `Mutation_NoUserRowLock_InvariantAndIdempotencyTestsTurnRed`: `UseUserRowLock = false` + hook `BeforeXpWrite` ép hai giao dịch đọc cùng lúc → (a) test bất biến `TotalXp` mất cập nhật PHẢI đỏ; (b) `Submit_TwoParallelRequestsSameQuestion...` của T020 PHẢI đỏ (một bên nhận lỗi từ unique index, tức 500); (c) khẳng định đồng thời bộ đếm vẫn `<= cap` nhờ câu SQL (chứng minh hai lớp độc lập). Tắt cờ → xanh. (FR-007, C2, research R12, SC-005)
- [x] T027 [US3] **M5**: `Mutation_SkipAlreadyTodayCheck_ReplayFarmingTestTurnsRed`: `SkipAlreadyTodayCheck = true` → `Submit_SameQuestionNextSessionSameDay_ZeroXpAlreadyToday` PHẢI đỏ (hoặc vi phạm unique một phần, ghi rõ quan sát); tắt cờ → xanh. (Ch4, C1, SC-005)
- [x] T028 [US2] **M6**: `Mutation_SkipReplay_IdempotencyTestsTurnRed`: `SkipReplay = true` → `Submit_SameQuestionAgain_ReturnsStoredResultAndDoesNotAddXp` và `Submit_AgainWithDifferentSelectedIndex_StillReturnsFirstResult` PHẢI đỏ; tắt cờ → xanh. (FR-005, C11, SC-005)
- [x] T029 [US4] **M7**: `Mutation_PlainInsertInCreateSession_ParallelPostTestTurnsRed`: `UseOnConflict = false` → `CreateSession_TwoParallelRequests_ReturnSameSessionId` PHẢI đỏ (một bên nhận lỗi unique index); tắt cờ → xanh. (FR-011, Ch7, SC-005)
- [x] T030 **Checkpoint Đợt 2**: build + toàn bộ test xanh; điền bảng "Bằng chứng mutation" ở `quickstart.md` (M1–M7); commit `feat: nghiệp vụ ôn tập idempotent, trần XP an toàn đồng thời` + push. Backend chưa có endpoint nhưng đã kiểm chứng ở tầng DB.

---

## Đợt 3: API, rate limit, test HTTP (US1–US4)

**Purpose**: Bốn endpoint đúng contract, bảo vệ bằng JWT người dùng và rate limit; kiểm bằng host thật.

- [x] T031 [P] [US2] `backend/QuizBattle.Api/Controllers/PracticeController.cs`: `GET /api/practice/rooms`, `POST /api/practice/sessions` (201 + `Location` khi tạo, 200 khi trả lượt đang mở), `GET /api/practice/sessions/{id}`, `POST /api/practice/sessions/{id}/answers` (luôn 200 cho câu đã chấm). `[Authorize]` theo scheme JWT người dùng (token khách → 401); Controller chỉ gọi `IPracticeService`; `ProducesResponseType` đủ 200/201/400/401/404/409/429. Không try/catch nuốt lỗi; `GlobalExceptionHandler` ánh xạ 404/409 `not_current_question` về ProblemDetails. (FR-001, FR-005, FR-011, C7)
- [x] T032 [P] [US2] `Program.cs`: policy rate limit `practice` phân vùng theo user (ví dụ 120 request/phút), `[EnableRateLimiting("practice")]` trên controller; giữ nguyên thứ tự middleware (`UseAuthorization` trước `UseRateLimiter`, bài học e319123). Test phản chiếu giữ tên policy. (FR-009, C9)
- [x] T033 [US2] Test HTTP `backend/QuizBattle.Tests/Integration/Http/PracticeHttpTests.cs` (qua `ApiFactory`): `AnyEndpoint_NoToken_Returns401`; `AnyEndpoint_GuestSessionToken_Returns401`; `CreateSession_IneligibleOrMissingRoom_Returns404Same`; `GetSession_OtherUsersSession_Returns404`; `Submit_QuestionNotInSession_Returns404`; `Submit_SelectedIndexOutOfRange_Returns400`; `RawPayload_RoomsAndSession_ContainNoCorrectIndexOfUnansweredQuestion` (so chuỗi trên JSON thô); `Submit_Response_HasCorrectIndexAndXpFields`; `Submit_Replay_Returns200SameResultEvenWithDifferentSelectedIndex`; `Submit_TwoParallelRequestsSameQuestion_BothReturn200SameResult`; `Submit_UnansweredNotCurrent_Returns409NotCurrentQuestion`; `CreateSession_FirstReturns201ThenReturns200WithSameSessionId`; `CreateSession_TwoParallelRequests_SameSessionId`; `GetSession_AfterSubmit_HasLastResult`; `FullFlow_CreateAnswerAllCompleteSummary_RemainingTodayCorrect`; `Practice_Over120PerMinute_Returns429`. (FR-001, FR-004, FR-005, FR-009, FR-011, FR-012, SC-001, SC-003, SC-004, C5–C7, C9, C11)
- [x] T034 **Checkpoint Đợt 3**: build + toàn bộ test xanh; commit `feat: API ôn tập /api/practice, rate limit policy practice` + push.

---

## Đợt 4: Frontend (US1, US2, US5)

**Purpose**: Ba màn hình đủ trạng thái tải/lỗi/rỗng, theo hệ thiết kế true-black-flat.

- [x] T035 [P] [US1] `frontend/src/types/practice.ts` (khớp DTO, camelCase) và `frontend/src/api/practice.ts` (`getPracticeRooms`, `createPracticeSession`, `getPracticeSession`, `submitPracticeAnswer`) qua API client dùng chung; URL từ `VITE_API_URL`. Không gọi `fetch` trong component. (FR-004)
- [x] T036 [P] [US5] Logic thuần `frontend/src/pages/Practice/practiceMessages.ts` (thông điệp theo `xpReason`: `wrong`, `already_today`, `daily_cap`, `daily_cap_partial`, `null`; định dạng "+X XP, còn Y hôm nay") và test Vitest `practiceMessages.test.ts` phủ mọi nhánh. (FR-006)
- [x] T037 [US1] `frontend/src/pages/Practice/PracticeRooms.tsx` (TanStack Query): danh sách phòng, số câu, "hôm nay X/100 XP", nút "Ôn" hoặc "Tiếp tục" khi có `openSessionId`; đủ trạng thái đang tải / lỗi (`ErrorRetry`) / rỗng. Test jsdom `PracticeRooms.test.tsx`: tải, lỗi + retry, rỗng, có dữ liệu, "Tiếp tục". (US1)
- [x] T038 [US2] `PracticeQuestion.tsx`: hiện câu hiện tại, chọn đáp án, nút disabled khi đang gửi, sau nộp hiện đúng/sai + đáp án đúng + XP; khi tải lại có `lastResult` thì hiện đáp án câu vừa nộp (F5 không mất); gửi lại không vỡ (phản hồi 200 như nhau). Test jsdom `PracticeQuestion.test.tsx`: disabled khi gửi, hiện kết quả, `lastResult` sau tải lại, lỗi 409 hiển thị rõ. Component quá ~150 dòng thì tách. (US2, FR-012)
- [x] T039 [US5] `PracticeSummary.tsx`: "+X XP trong lượt này, còn Y XP hôm nay", số câu đúng, nút về danh sách. Test jsdom `PracticeSummary.test.tsx`. (US5)
- [x] T040 [US2] Route `/practice` và `/practice/:sessionId` trong `App.tsx` dưới `ProtectedRoute`; liên kết vào trang Dashboard/menu theo kiểu có sẵn (`btn-ghost`, không dùng màu `accent` cho nút). (FR-001)
- [x] T041 [US2] Kiểm giao diện: nếu máy có skill `design-taste-frontend`, chạy `lint.mjs --app` và `inspect.mjs --app` trên bản build; sửa hết FAIL (không gradient, bóng, emoji, hex trực tiếp; `accent` chỉ cho thanh tiến độ XP). Build `npm run build` và `npm test`.
- [x] T042 **Checkpoint Đợt 4**: build + test frontend và backend xanh; commit `feat: màn ôn tập (danh sách phòng, từng câu, tổng kết)` + push.

---

## Đợt 5: Chốt — production, tài liệu, nghiệm thu

- [x] T043 [P] Script SQL idempotent cho production: xuất TỪ migration `20261119094951_AddAiUsageDaily` (production đang ở đó) ĐẾN `AddPracticeMode` bằng `dotnet ef migrations script 20261119094951_AddAiUsageDaily AddPracticeMode --idempotent`. Lưu NGOÀI repo tại `D:\IT_Code\Quiz_Battle\sql\005-AddPracticeMode.sql`. Báo kích thước và SHA-256. Xác nhận bằng grep không có `DROP`/`DELETE` (ALTER chỉ thêm ràng buộc của 3 bảng mới). Cuối file thêm RIÊNG một khối `ALTER TABLE ... ENABLE ROW LEVEL SECURITY` cho `PracticeSessions`, `PracticeAnswers`, `PracticeXpDaily` (EF không bật RLS) và hai câu `SELECT` kiểm sau khi chạy: `__EFMigrationsHistory` (có `AddPracticeMode`) và `pg_class.relrowsecurity` của 3 bảng (phải `true`). Ghi bước chạy thủ công trên Supabase (Session pooler 5432) trước khi merge. (CLAUDE.md: Môi trường và Deploy)
- [x] T044 Nghiệm thu `quickstart.md` mục 1–12 trên dev (dữ liệu thật), điền "Bằng chứng mutation" nếu chưa đủ; ghi kết quả vào mô tả PR. (SC-001…SC-005)
- [x] T045 [P] Tài liệu: kiểm `.env.example` có hai khóa mới; `spec.md` mục "Hạn chế đã biết" khớp thực tế (số 3000 XP/tháng = 100 × 30); nếu có README liệt kê API/tính năng thì thêm mục ôn tập. (Ch10)
- [ ] T046 **Checkpoint cuối**: toàn bộ test xanh; script đã chạy trên Supabase (T043). KHÔNG tự merge: chỉ merge `feat/practice-mode` vào `main` (merge commit, theo lịch sử repo) khi người làm báo cố vấn đã duyệt diff và test trình duyệt. Chưa chạy script thì cũng không merge: `/api/practice` trên production sẽ trả 500 vì thiếu bảng.

---

## Dependencies & Execution Order

- **Đợt 1**: T001/T002, T003/T004, T005 [P] song song → T006 (sau T005) → T007, T008 (sau T006) → T009.
- **Đợt 2**: cần Đợt 1 đã push. T010 → T011 [P], T012, T013, T014 (cùng file repo, viết tuần tự) → T015 (sau T012–T014) → T016 → T017–T022 (cùng thư mục test, độc lập) → T023–T029 (cần T019/T020/T021/T022 xanh trước, vì mutation so với bản xanh) → T030.
- **Đợt 3**: T031, T032 [P] → T033 → T034. Cần T030.
- **Đợt 4**: T035, T036 [P] có thể bắt đầu từ khi contract chốt (không cần backend); T037–T039 sau T035/T036; T040 sau T037–T039; T041; T042. Chạy với backend thật chỉ sau T034.
- **Đợt 5**: sau mọi đợt. T043, T045 [P]; T044 sau T042; T046 cuối cùng.

### Parallel Opportunities
T001–T005; T011; T031, T032; T035, T036; T043, T045.

## Implementation Strategy

- **MVP = Đợt 1 + 2 + 3**: backend đầy đủ, chặn gian lận và idempotent, kiểm bằng HTTP thật; frontend có thể trễ.
- Dừng ở checkpoint nào cũng còn sản phẩm chạy được (tính năng mới chỉ lộ khi frontend có route).
- Merge `main` chỉ sau T043 chạy xong trên Supabase.

---

## Bảng đối chiếu

Mỗi hàng: yêu cầu → task thực hiện → test chứng minh. Test dùng tên tệp/nhóm ở trên.

### Yêu cầu chức năng

| FR | Nội dung | Task thực hiện | Test chứng minh |
|---|---|---|---|
| FR-001 | Chỉ JWT người dùng, khách bị chặn | T031, T040 | T033 (`NoToken_Returns401`, `GuestSessionToken_Returns401`) |
| FR-002 | Phòng ôn được: Finished + host/Player + có `Awarded` + có câu đã chơi | T012 | T017 |
| FR-003 | Câu đã chơi, thứ tự ngẫu nhiên, lưu lại | T013, T014 | T021 (`OrderIsStoredNotReshuffledOnRead`), T033 (`FullFlow`) |
| FR-004 | DTO câu hỏi không có `CorrectIndex` | T010, T035 | T011, T021 (raw JSON), T033 (`RawPayload...`) |
| FR-005 | Nộp idempotent, 1 lần chấm cho mỗi (lượt, câu), không 409 `already_answered` | T015, T031 | T020, T033 (`Replay`, `TwoParallelRequests`), T028 (M6) |
| FR-006 | Luật XP: một lần/(user, câu, ngày), trần, `LEAST` | T001, T015, T016 | T002, T018, T019, T024 (M2), T025 (M3), T027 (M5) |
| FR-007 | Một transaction, khóa `Users`, không nhánh bắt unique-violation | T015 | T022 (`RollsEverythingBack`, `ConcurrentWithMatchSettlement`), T026 (M4) |
| FR-008 | Không Xu, không `CoinTransaction` | T015 | T018 (`DoesNotCreateCoinTransaction...`), T008 (bất biến Xu) |
| FR-009 | Rate limit theo user; 429 có `code` `PRACTICE_RATE_LIMIT` | T032 | T033 (`Over120PerMinute_Returns429WithPracticeRateLimitCode`, `RateLimitIsPerUser`), `PracticeControllerTests` (phản chiếu `[EnableRateLimiting("practice")]`, `CodeFor`), mutation M9 |
| FR-010 | Cấu hình, kiểm `>= 0` lúc khởi động | T003 | T004 |
| FR-011 | Một lượt mở cho mỗi (user, phòng); POST trả lượt dở | T005, T013, T031 | T007 (unique một phần), T021, T033, T029 (M7) |
| FR-012 | `lastResult` trong GET | T014, T038 | T021, T033 (`AfterSubmit_HasLastResult`), T038 |
| FR-013 | Lượt kết thúc khi hết câu còn tồn tại | T014, T015 | T021 (`QuestionDeletedMidway...`) |
| FR-014 | `XpReason` lưu + CHECK | T005, T006, T015 | T007 (các nhánh CHECK) |
| FR-015 | Bất biến XP hai nguồn + bộ đếm ngày | T008, T015 | T008, T022 (`MixedMatchAndPracticeSequence`) |

### Tiêu chí thành công

| SC | Task thực hiện | Test chứng minh |
|---|---|---|
| SC-001 | T010, T031 | T011, T033 (`RawPayload...`) |
| SC-002 | T015 | T019 (`ParallelSubmitsOverCap`), T023 (M1) |
| SC-003 | T015 | T020, T033 (`TwoParallelRequestsSameQuestion`) |
| SC-004 | T013 | T021, T033, T029 (M7) |
| SC-005 | T023–T029 | từng M tự là test + `quickstart.md` mục "Bằng chứng mutation" |

### Điểm đã chốt (Ch1–Ch10)

| Chốt | Nội dung | Task thực hiện | Test chứng minh |
|---|---|---|---|
| Ch1 | Bảng mới `PracticeAnswers.XpAwarded`; bất biến hai nguồn | T005, T006, T008 | T007, T008, T022 |
| Ch2 | `XpPerCorrect = 5`, `DailyXpCap = 100` | T003 (mặc định), T001 | T004 (`Missing_UsesDefaults5And100`), T018 |
| Ch3 | `LEAST` (cộng phần còn lại) | T001 (`ApplyCap`), T015 | T002, T019 (7/10), T024 (M2), T025 (M3) |
| Ch4 | XP một lần/(user, câu, ngày) | T005 (unique một phần), T015 | T007, T018, T027 (M5) |
| Ch5 | Phòng cần `MatchReward` `Awarded` | T012 | T017 (`FinishedRoomWithoutAwardedReward_NotListed`) |
| Ch6 | `VietnamDay` giữ nguyên ở `Domain.AiQuota` | T016 (dùng nguyên chỗ, không đổi namespace) | `VietnamDayTests` hiện có vẫn xanh (T009/T030 chạy toàn bộ) |
| Ch7 | Một lượt mở/(user, phòng); POST trả lượt dở; READ COMMITTED + `ON CONFLICT DO NOTHING` + SELECT riêng | T005, T013, T031 | T007, T021, T033, T029 (M7) |
| Ch8 | Câu bị xóa giữa lượt: lượt kết thúc | T014, T015 | T021 (`QuestionDeletedMidway...`) |
| Ch9 | Không vào `ProductionConfigValidator`; kiểm `>= 0` lúc khởi động | T003 | T004 |
| Ch10 | Chấp nhận, ghi "Hạn chế đã biết" kèm ~3000 XP/tháng | T045 (kiểm tài liệu) | Không có test (tài liệu); `spec.md` mục "Hạn chế đã biết" |

### Hai chỉnh của vòng duyệt gần nhất và lỗ idempotent

| Yêu cầu | Task thực hiện | Test chứng minh |
|---|---|---|
| KHÔNG nhánh bắt unique-violation → rollback → đọc lại; unique index giữ làm lưới, vi phạm = 500 | T015 (không viết nhánh), research R12 | T026 (M4: không khóa thì 500, chứng minh lưới còn hoạt động) |
| `POST /sessions`: READ COMMITTED, `ON CONFLICT ("UserId","RoomId") WHERE "CompletedAt" IS NULL DO NOTHING`, đọc lại bằng câu lệnh riêng, ghi lý do vào research.md | T013 (+ research R11 đã viết) | T021, T033, T029 (M7) |
| Nộp lại → 200 kết quả đã lưu; `selectedIndex` khác vẫn kết quả đầu; bỏ 409 `already_answered` | T015, T031 | T020, T033, T028 (M6) |
| `XpReason` + CHECK (giá trị thuộc tập hoặc NULL; NULL ⇔ đủ XP) | T005, T006, T015 | T007, T019 (`XpPerCorrectZero`) |
| `lastResult` ở GET | T014, T038 | T021, T033, T038 |
| 2 request đồng thời cùng câu → cả hai 200, cùng kết quả, XP một lần | T015 | T020, T033, T026 (M4) |
| 2 POST đồng thời → cùng một `sessionId` | T013 | T021, T033, T029 (M7) |

### Lối cày (C1–C11)

| Lối cày | Cách chặn | Task thực hiện | Test chứng minh |
|---|---|---|---|
| C1 Dò đáp án qua nhiều lượt | XP một lần/(user, câu, ngày) + trần ngày + một lượt mở/phòng; chấp nhận học thuộc | T005, T013, T015 | T018 (`SameQuestionNextSessionSameDay`), T021, T027 (M5) |
| C2 Nộp đồng thời cùng câu | Khóa `Users` + phát lại | T015 | T020, T033, T026 (M4) |
| C3 Nộp đồng thời nhiều câu vượt trần | Khóa `Users` + `WHERE`/`LEAST` | T015 | T019, T023 (M1), T026 (M4) |
| C4 Phòng tự tạo một mình | Cần `MatchReward` `Awarded` | T012 | T017 |
| C5 Đọc đáp án từ payload | DTO không có `CorrectIndex` | T010, T031 | T011, T033 |
| C6 Lượt của người khác | Ràng buộc `UserId`, 404 | T014, T015 | T018 (`OtherUsersSession`), T033 |
| C7 Khách gọi API | `[Authorize]` JWT người dùng | T031 | T033 (`GuestSessionToken_Returns401`) |
| C8 Đổi đồng hồ | Ngày VN tính ở server | T016 | T018 (`NextDay`), T022 (nửa đêm) |
| C9 Spam | Policy `practice` | T032 | T033 (`429`) |
| C10 Phình bảng bằng lượt dở | Unique một phần lượt mở | T005, T013 | T007, T021, T029 (M7) |
| C11 Gửi lại để kiếm XP / đổi đáp án | Idempotent, bỏ qua `selectedIndex` mới | T015 | T020, T033, T028 (M6) |

### Mutation (M1–M7)

| M | Cách mutate (cờ `internal`) | Task thực hiện | Test phải đỏ |
|---|---|---|---|
| M1 | `ReserveVariant.NoWhere` | T015 (cờ), T023 | T019 `Cap_LoweredBelowAlreadyUsed_DoesNotDecreaseCounterOrGrant` (đính chính: `LEAST` đã chặn vượt trần, `WHERE` chỉ cần khi trần bị hạ) |
| M2 | `ReserveVariant.NoLeast` | T015 (cờ), T024 | T019 `Cap_RemainingLessThanQuestionXp_GrantsRemainderThenZero` (7/10) |
| M3 | `ReserveVariant.RejectWhole` | T015 (cờ), T025 | T019 `Cap_RemainingLessThanQuestionXp_GrantsRemainderThenZero` (7/10) |
| M4 | `UseUserRowLock = false` + `BeforeXpWrite` | T015 (cờ), T026 | T022 bất biến `TotalXp`, T020 `TwoParallelRequestsSameQuestion` |
| M5 | `SkipAlreadyTodayCheck` | T015 (cờ), T027 | T018 `SameQuestionNextSessionSameDay_ZeroXpAlreadyToday` |
| M6 | `SkipReplay` | T015 (cờ), T028 | T020 `SameQuestionAgain...`, `AgainWithDifferentSelectedIndex...` |
| M7 | `UseOnConflict = false` | T013 (cờ), T029 | T021 `CreateSession_TwoParallelRequests_ReturnSameSessionId` |
| M8 (thêm) | `UseUserRowLock = false` + `ReserveVariant.NonAtomic` | T015 (cờ), `PracticeSensitivityTests.Mutation_M8_...` | T019 `Cap_ParallelSubmitsOverCap_NeverExceedsCap`, T020 `TwoParallelRequestsSameQuestion`, T022 `ConcurrentWithMatchSettlement` |
| M9 (thêm) | Bỏ `[EnableRateLimiting("practice")]` | T032 (attribute), bằng chứng ở `quickstart.md` | T033 `Practice_Over120PerMinute_Returns429WithPracticeRateLimitCode`, `PracticeControllerTests.PracticeController_HasEnableRateLimitingPracticePolicy` |

Mỗi M kèm bước "tắt cờ → xanh" trong chính task mutation của nó (chứng minh test không đỏ vì lý do khác) và dòng quan sát trong `quickstart.md`.
