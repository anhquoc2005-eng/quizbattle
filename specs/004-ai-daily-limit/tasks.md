# Tasks: Giới hạn AI theo ngày

**Input**: Design documents from `/specs/004-ai-daily-limit/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ai-quota.md, quickstart.md

**Tests**: Có — bắt buộc theo CLAUDE.md và yêu cầu cố vấn (spec SC-001…SC-008). Test tích hợp dùng Postgres thật (`DatabaseFixture` + `RequiresDatabaseFact`, đặt `QUIZBATTLE_TEST_DB`).

**Organization**: 4 ĐỢT theo plan.md. Mỗi đợt kết thúc bằng checkpoint build + test xanh và push độc lập (commit nhỏ Conventional Commits trong đợt vẫn khuyến khích). Nhãn [US1/US2/US3] ánh xạ về user story trong spec.md; nhãn quyết định Q1–Q7 và A–E trỏ tới bảng quyết định trong spec.md.

**Tổng: 32 task T001–T032 + T016a (vá lỗ hủy request) + 3 checkpoint T013a/T023a/T029a / 4 đợt** (Đợt 1: T001–T013 + T013a, Đợt 2: T014–T023 + T023a, Đợt 3: T024–T029 + T029a, Đợt 4: T030–T032).

## Format: `[ID] [P?] [Story] Mô tả + đường dẫn file`

- **[P]**: chạy song song được (khác file, không phụ thuộc task chưa xong)

---

## Đợt 1: Backend nền + migration + bộ đếm nguyên tử (Foundational — chặn mọi đợt sau)

**Purpose**: Có bảng, câu SQL giữ chỗ/hoàn lượt và chứng minh bằng Postgres thật rằng nó đúng khi đồng thời. Chưa đụng `QuestionService`, chưa có endpoint.

- [x] T001 [P] [US2] Domain: `backend/QuizBattle.Domain/AiQuota/VietnamDay.cs` — hàm thuần `DayOf(DateTimeOffset utcNow) → DateOnly` (= `utcNow.UtcDateTime.AddHours(7)` lấy ngày) và `NextResetAt(DateOnly day) → DateTimeOffset` (00:00 +07:00 của ngày kế tiếp). Không dùng `TimeZoneInfo`, không đọc đồng hồ. (Q2)
- [x] T002 [P] [US2] Test `backend/QuizBattle.Tests/Domain/VietnamDayTests.cs`: `DayOf_At235959Vietnam_ReturnsSameDay` (`16:59:59Z`), `DayOf_At000000Vietnam_ReturnsNextDay` (`17:00:00Z`), `DayOf_InputWithOffset_NormalizesToUtcFirst` (đầu vào mang offset +07/−05 cho cùng kết quả), `NextResetAt_ReturnsMidnightVietnamOfNextDay` (offset `+07:00`), `DayOf_DoesNotDependOnLocalTimeZone` (đặt `TZ`/không đọc `TimeZoneInfo.Local`: kết quả cố định theo bảng giá trị). (Q2, E)
- [x] T003 [P] Entity + cấu hình: `backend/QuizBattle.Domain/Entities/AiUsage.cs` (`DateOnly Day`, `string Scope`, `int Count`), `backend/QuizBattle.Infrastructure/Persistence/Configurations/AiUsageConfiguration.cs` (`ToTable("AiUsageDaily")`, khóa `(Day, Scope)`, `Scope` NOT NULL `HasMaxLength(50)`, CHECK `Count >= 0`), thêm `DbSet` vào DbContext. KHÔNG dùng `UserId` nullable — ghi lý do bằng comment một dòng. (B)
- [x] T004 Migration `AddAiUsageDaily` trong `backend/QuizBattle.Infrastructure/Migrations/`: tạo bằng `dotnet ef migrations add`, đọc lại file sinh ra (chỉ `CreateTable`, PK `(Day, Scope)`, CHECK có mặt, không đụng bảng khác); áp lên Postgres Docker dev. Sau T003. (Q7, B)
- [x] T005 [P] Application contracts: `backend/QuizBattle.Application/AiQuota/` — `IAiQuotaRepository` (`ReserveAsync(Guid userId, DateOnly day, int userLimit, int globalLimit, ct) → AiReserveResult`; `RefundUserAsync(Guid userId, DateOnly day, ct)`; `GetUsageAsync(Guid userId, DateOnly day, ct) → (int UserCount, int GlobalCount)`), `AiReserveResult` (Granted với `Day`/`UserCount`, hoặc Rejected với `AiLimitKind.User|Global`), `AiQuotaScopes` (`Global`, `ForUser(Guid)` → `user:{guid:D}`). Interface KHÔNG có hàm hoàn global. (Q4, A)
- [x] T006 Infrastructure: `backend/QuizBattle.Infrastructure/Persistence/AiQuotaRepository.cs` — `ReserveAsync`: một transaction READ COMMITTED, chạy câu `INSERT ... ON CONFLICT (Day, Scope) DO UPDATE SET Count = Count + 1 WHERE Count < @limit RETURNING Count` cho `global` rồi cho `user:{id}`; câu nào không trả dòng thì rollback và trả `Rejected(Global|User)`; COMMIT trước khi return. Dùng `DbCommand` từ connection của DbContext (không dùng `SqlQuery`, xem research R3). `RefundUserAsync`: `UPDATE ... SET Count = GREATEST(Count - 1, 0) WHERE Day = @day AND Scope = 'user:{id}'`, một câu, không transaction nhiều bước. `GetUsageAsync`: `AsNoTracking`/projection. Đăng ký DI trong `Infrastructure/DependencyInjection`. Sau T003–T005. (Q4, A, C)
- [x] T007 [US2] Test tích hợp `backend/QuizBattle.Tests/Integration/AiQuotaReservationTests.cs` (cơ bản): `Reserve_FirstCallOfDay_CreatesGlobalAndUserRowsWithCount1`; `Reserve_UntilLimit_GrantsEveryCallThenRejectsWithUser`; `Reserve_AtLimit_DoesNotChangeAnyRow`; `Reserve_DifferentDays_CountSeparately`. Sau T006. (Q4)
- [x] T008 [US2] Cùng file — **đồng thời trên dòng user**: `Reserve_ParallelCallsOverUserLimit_GrantsExactlyLimit` — N = 30 tác vụ song song (mỗi tác vụ một DbContext/connection riêng), `userLimit = K = 5`, `globalLimit` lớn: đúng 5 `Granted`, 25 `Rejected(User)`, `Count` dòng user trong DB = 5, dòng global = 5. (Q4, E, SC-001)
- [x] T009 [US2] Cùng file — **đồng thời trên dòng global**: `Reserve_ManyUsersOverGlobalLimit_GrantsExactlyGlobalLimit` — `globalLimit = K = 5`, N = 20 user khác nhau gọi song song (`userLimit` lớn): đúng 5 `Granted`, 15 `Rejected(Global)`, dòng global = 5, tổng `Count` các dòng user = 5, và không có dòng user nào tồn tại với `Count > 0` mà không thuộc một `Granted` (chặn trường hợp trừ user mà global từ chối). (Q4, E, SC-002)
- [x] T010 [US2] Cùng file — **tính nguyên tử hai dòng**: `Reserve_UserAtLimitGlobalHasRoom_RollsBackGlobalIncrement` (global giữ nguyên, kết quả `Rejected(User)`); `Reserve_GlobalAtLimitUserHasRoom_DoesNotTouchUserRow` (dòng user không tồn tại hoặc không đổi, `Rejected(Global)`); `Reserve_BothAtLimit_RejectsAsGlobal` (global kiểm trước). (Q4, FR-006)
- [x] T011 [US3] Cùng file — **hoàn lượt**: `Refund_AfterReserve_DecrementsUserRowOnly` (global KHÔNG đổi); `Refund_WhenCountIsZero_StaysZero` (`GREATEST`); `Refund_UsesGivenDay_NotCurrentDay` (giữ chỗ ngày D, hoàn với D khi "hiện tại" đã sang D+1 → dòng D giảm, dòng D+1 không bị tạo/đổi). (A, SC-005)
- [x] T012 [US2] Cùng file — **xen kẽ không deadlock**: `ReserveAndRefund_InterleavedInParallel_NoDeadlockAndCorrectFinalCounts` — nhiều user (kể cả nhiều tác vụ cùng một user) chạy song song vòng lặp "reserve; nếu Granted và chẵn thì refund"; timeout cứng 30s cho cả bài; khẳng định không có `PostgresException` mã `40P01` (deadlock) hay `40001`, dòng global = tổng số Granted, mỗi dòng user = Granted − Refunded của user đó. (C, E, SC-004)
- [x] T013 [US2] **Chứng minh test đỏ khi bỏ điều kiện `WHERE`**: tạm xóa `WHERE "AiUsageDaily"."Count" < @limit` khỏi câu giữ chỗ trong `AiQuotaRepository`, chạy T008 và T009 → PHẢI đỏ (số Granted > K); dán kết quả (tên test, số Granted quan sát được) vào mô tả commit và ghi một dòng trong quickstart.md mục "Bằng chứng mutation"; khôi phục câu SQL, chạy lại → xanh. Không commit bản đã xóa điều kiện. (E, SC-003)

**Checkpoint Đợt 1**:
- [x] T013a Build + toàn bộ test xanh (kể cả tích hợp với `QUIZBATTLE_TEST_DB`); commit `feat: bộ đếm lượt AI theo ngày bằng Postgres` + push.

---

## Đợt 2: Backend nối dây — service, 429, endpoint (US1, US2, US3)

**Purpose**: `GenerateAsync` giữ chỗ/hoàn đúng chỗ; lỗi 429 có `code`; endpoint GET.

- [x] T014 [P] [US2] `backend/QuizBattle.Application/Common/AiDailyLimitException.cs` (mang `AiLimitKind` và `ResetAt`) + nhánh 429 trong `backend/QuizBattle.Api/Middleware/GlobalExceptionHandler.cs`: `status 429`, `Extensions["code"]` = `AI_DAILY_LIMIT_USER|GLOBAL`, `Extensions["resetAt"]` (ISO 8601 +07:00), title theo contracts. Test handler (unit): hai `code`, có `resetAt`, không lộ stack. (Q5, FR-006)
- [x] T015 [P] [US1] Cấu hình: `AiQuotaOptions` (`Ai:DailyLimitPerUser` mặc định 20, `Ai:DailyLimitGlobal` mặc định 300) bind trong DI; validate số nguyên > 0 khi khởi động; `ProductionConfigValidator` chỉ kiểm "nếu có đặt thì > 0" (không bắt buộc có mặt). Unit test validator: thiếu → hợp lệ; `0`, `-1`, `abc` → lỗi rõ ràng. Không thêm gói. (Q1, Q7, FR-011)
- [x] T016 [US1] `backend/QuizBattle.Application/AiQuota/` — `IAiQuotaService` + `AiQuotaService`: `ReserveAsync(userId, ct) → AiReservation(UserId, Day)` (lấy `Day` từ `VietnamDay.DayOf(timeProvider.GetUtcNow())`, gọi repository, ném `AiDailyLimitException` khi `Rejected`), `RefundAsync(AiReservation)` (gọi `RefundUserAsync` với `reservation.Day`, KHÔNG tính lại ngày), `GetStatusAsync(userId, ct) → AiQuotaResponse` (`remaining = max(limit − used, 0)`, `globalExhausted`, `resetAt`). Đăng ký DI. Sau T005, T006, T014, T015. (Q1, Q2, A)
- [x] T016a [US2] **Vá lỗ "hủy request bị đổi thành lỗi AI"** trong `backend/QuizBattle.Infrastructure/Ai/GeminiQuestionGenerator.cs`: khối `catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)` hiện bọc cả trường hợp client đóng tab (`RequestAborted` làm `SendAsync` ném `TaskCanceledException`) thành `AiUnavailableException` → T017 sẽ hoàn lượt khi hủy = lỗ lách trần. Sửa: nếu `cancellationToken.IsCancellationRequested` thì ném lại `OperationCanceledException` (không bọc); chỉ timeout của HttpClient (token của request CHƯA bị hủy) và `HttpRequestException` mới thành `AiUnavailableException`. `ReadAsStringAsync(cancellationToken)` nằm ngoài `try` nên hủy ở bước đọc body vốn đã ném thẳng `OperationCanceledException` (giữ nguyên). Test `backend/QuizBattle.Tests/Questions/GeminiQuestionGeneratorCancellationTests.cs` với `HttpMessageHandler` giả viết tay (không Moq; `HttpClient` tạo từ handler đó, `AiOptions` đã cấu hình): (a) `Generate_TokenCancelledDuringSend_ThrowsOperationCanceledNotAiUnavailable` (handler chờ đến khi token bị hủy rồi ném `TaskCanceledException`); (b) `Generate_HandlerThrowsTaskCanceledWhileTokenNotCancelled_ThrowsAiUnavailable` (mô phỏng timeout HttpClient); (c) `Generate_HttpRequestException_ThrowsAiUnavailable`. Làm TRƯỚC T017. (A, E)
- [x] T017 [US2] `backend/QuizBattle.Application/Questions/QuestionService.cs` — `GenerateAsync`: giữ chỗ SAU `GetOwnedDraftRoomAsync`, kiểm `MAX_QUESTIONS_PER_ROOM` và `ListTextsByRoomIdAsync`, TRƯỚC `GenerateValidCandidatesAsync`; bọc đoạn từ giữ chỗ đến hết `GenerateValidCandidatesAsync` bằng `catch (Exception ex) when (ex is BadGatewayException or AiUnavailableException)` → `RefundAsync(reservation)` với `CancellationToken.None` (lỗi hoàn chỉ ghi log, không che lỗi gốc) → `throw;`. Điều kiện hoàn có thêm `&& !cancellationToken.IsCancellationRequested` (phòng thủ lớp hai: dù generator lọt lỗi nào khác khi request đã bị hủy cũng không hoàn). KHÔNG bắt `OperationCanceledException`, KHÔNG hoàn khi `ConflictException` ở bước chèn. Thêm `IAiQuotaService` vào constructor. Sau T016 và T016a. (Q3, Q4, A, C, D) **Lệch so với bản đầu**: lỗi khi hoàn lượt KHÔNG bắt ở QuestionService (tầng Application không có logger); `AiQuotaRepository.RefundUserAsync` là best-effort (bắt lỗi, `LogError`, không ném) nên lỗi AI gốc luôn là lỗi người dùng thấy.
- [x] T018 [US2] Unit test `backend/QuizBattle.Tests/Questions/QuestionServiceQuotaTests.cs` (fake `IAiQuotaService`/repository/generator viết tay, không Moq; ghi lại thứ tự lời gọi) — **thứ tự kiểm tra (D)**: `Generate_RoomNotFound_DoesNotReserve`, `Generate_NotRoomOwner_DoesNotReserve`, `Generate_RoomNotDraft_DoesNotReserve`, `Generate_ExceedsMaxQuestionsPerRoom_DoesNotReserve`; và `Generate_InvalidCount_RejectedByModelValidationBeforeService` (khẳng định thuộc tính validation của `GenerateQuestionsRequest.Count` còn nguyên 5–20; nếu đã có test tương đương thì chỉ dẫn chiếu, không trùng). Mỗi bài khẳng định `Reserve` được gọi 0 lần, `Generator` 0 lần. (D, SC-007)
- [x] T019 [US3] Cùng file — **hoàn lượt**: `Generate_ReserveRejected_ThrowsAiDailyLimitAndNeverCallsGenerator`; `Generate_GeminiBadGateway_RefundsUserOnceWithReservedDay`; `Generate_GeminiUnavailable_RefundsUserOnce`; `Generate_Success_DoesNotRefund`; `Generate_CancelledWhileWaitingForGemini_DoesNotRefund` (generator giả ném `OperationCanceledException` — chỉ kiểm logic của service; việc phân loại hủy/timeout của generator THẬT nằm ở T016a và T020); `Generate_AiUnavailableButRequestCancelled_DoesNotRefund` (generator giả ném `AiUnavailableException` khi token đã hủy → điều kiện `!IsCancellationRequested` chặn hoàn); `Generate_InsertConflictAfterGemini_DoesNotRefund`; `Generate_RefundFails_StillThrowsOriginalAiException`; `Generate_RefundUsesDayFromReservation_EvenIfClockMovedToNextDay`. (A, E)
- [x] T020 [US2] Test tích hợp luồng đầy đủ `backend/QuizBattle.Tests/Integration/AiQuotaGenerateFlowTests.cs` (`QuestionService` + `AiQuotaRepository` thật + generator giả + `TimeProvider` giả có thể dịch giờ): `GenerateFlow_GeminiBadGateway_RefundsUserRowButNotGlobal`; `GenerateFlow_RequestCancelled_KeepsBothCounts` (đếm DB sau khi hủy giữa chừng: user và global đều giữ nguyên +1); `GenerateFlow_ReservedAt235959Vietnam_GeminiFailsAfterMidnight_RefundsOldDayRow` (đồng hồ giả: 16:59:59Z giữ chỗ, dịch sang 17:00:01Z trước khi generator ném lỗi → dòng ngày D về 0, dòng ngày D+1 không tồn tại); `GenerateFlow_ReserveAt000000Vietnam_UsesNewDayRow`; `GenerateFlow_ClientCancelledDuringRealGeminiCall_DoesNotRefund` (dùng `GeminiQuestionGenerator` THẬT + `HttpMessageHandler` giả chờ đến khi token bị hủy rồi ném `TaskCanceledException`, đúng như `RequestAborted` thật; sau khi hủy đếm DB: dòng user và dòng global đều giữ nguyên +1). (A, E, Q2, SC-005, SC-006) **Đã chứng minh đỏ**: bỏ nhánh ném lại của T016a thì test này đỏ (`Expected OperationCanceledException, Actual AiUnavailableException`); bỏ thêm `!IsCancellationRequested` ở T017 thì đếm DB user = 0 thay vì 1 (đã hoàn nhầm). Thêm `GenerateFlow_GeminiTimeoutWithTokenAlive_RefundsUserRow` làm đối chứng.
- [x] T021 [P] [US1] `backend/QuizBattle.Api/Controllers/AiQuotaController.cs`: `GET /api/ai-quota` (`[Authorize]`, chỉ gọi `IAiQuotaService.GetStatusAsync(User.GetUserId())`, trả `AiQuotaResponse`). Thêm `[ProducesResponseType(429)]` cho `QuestionsController.Generate` (không đổi logic). Unit test `GetStatusAsync`: chưa dùng → `used 0, remaining 20`; dùng 20 → `remaining 0`; dùng 25 (do hạ trần giữa ngày) → `remaining 0` (không âm); global ≥ trần → `globalExhausted true`; response không chứa số global. (Q6, FR-008)
- [x] T022 [P] [US2] `Program.cs` `OnRejected`: thêm `Extensions["code"] = "AI_RATE_LIMIT"` CHỈ khi endpoint dùng policy `generate-questions` (đọc `EnableRateLimitingAttribute` từ metadata endpoint; tách thành hàm tĩnh nhỏ để test). Test unit: policy `generate-questions` → `AI_RATE_LIMIT`; policy `auth`/`join-room` → không có `code`. (Q5, FR-007)
- [x] T023 [US2] Test phản chiếu giữ nguyên policy: `Generate_Endpoint_StillUsesGenerateQuestionsRateLimitPolicy` (thuộc tính `EnableRateLimiting("generate-questions")` còn trên `QuestionsController.Generate`) và `GenerateQuestionsPolicy_StillTenPerHourPerUser` nếu cấu hình có thể đọc ra được (nếu không, ghi rõ trong mô tả test là chỉ khẳng định tên policy). (Q5)

**Checkpoint Đợt 2**:
- [x] T023a Build + toàn bộ test xanh; commit `feat: giữ chỗ và hoàn lượt AI trong GenerateAsync, endpoint ai-quota, 429 có code` + push. Sau commit này backend đủ dùng độc lập (frontend cũ vẫn chạy, chỉ thấy 429 chung).

---

## Đợt 3: Frontend — hiện số lượt, tắt nút, thông báo (US1)

**Purpose**: Chủ phòng thấy "Còn X/20 lượt AI hôm nay"; hết lượt thì nút tắt kèm lý do; nhập tay không bị ảnh hưởng.

- [x] T024 [P] [US1] `frontend/src/types/aiQuota.ts` (`AiQuota` khớp `AiQuotaResponse`: `dailyLimit, used, remaining, globalExhausted, resetAt`) và `frontend/src/api/aiQuota.ts` (`getAiQuota()` qua `request`, không `fetch` trực tiếp). Xác nhận `ApiError` (`src/api/apiError.ts`) đọc được `code` VÀ `resetAt` từ ProblemDetails; chưa có `resetAt` thì thêm. (Q6, FR-009)
- [x] T025 [P] [US1] `frontend/src/hooks/useAiQuota.ts` — TanStack Query, key `['ai-quota']`, không sao chép vào `useState`. (Q6)
- [x] T026 [P] [US1] `frontend/src/validation/aiQuota.ts` (logic thuần, test Vitest `aiQuota.test.ts`): `getGenerateBlock(quota) → 'none' | 'user' | 'global'` (global ưu tiên khi cả hai); `describeAiLimit(code, resetAt)` → thông báo cho `AI_DAILY_LIMIT_USER`, `AI_DAILY_LIMIT_GLOBAL`, `AI_RATE_LIMIT` (có "làm mới lúc 00:00 (giờ Việt Nam)" khi có `resetAt`; `AI_RATE_LIMIT` không nói tới ngày). Test: từng nhánh, `remaining 0`, `remaining 3`, `globalExhausted` + `remaining 5` → `'global'`. (Q5, Q6)
- [x] T027 [US1] `frontend/src/pages/RoomPage/AiQuotaBadge.tsx` (mới) hiển thị "Còn {remaining}/{dailyLimit} lượt AI hôm nay" (số dùng `tabular-nums`; có thể `text-accent` vì là dữ liệu), đủ 3 trạng thái: đang tải (khung chờ), lỗi (`ErrorRetry`, KHÔNG chặn nút), rỗng/đủ dữ liệu. Sửa `frontend/src/pages/RoomPage/GenerateQuestionsPanel.tsx`: nút `disabled` khi `mutation.isPending` HOẶC `getGenerateBlock(quota) !== 'none'`, hiện lý do dưới nút (người dùng vs hệ thống), `invalidateQueries(['ai-quota'])` trong `onSuccess` VÀ `onError` (server có thể đã trừ/hoàn lượt), `describeApiError` của 429 dùng `describeAiLimit(code, resetAt)`. Không đụng form thêm câu hỏi nhập tay (`AddQuestionForm`, nếu tên khác thì file tương ứng). Theo hệ thiết kế true-black-flat. Sau T024–T026. (Q6, FR-009, FR-010)
- [x] T028 [US1] Test component `frontend/src/pages/RoomPage/GenerateQuestionsPanel.test.tsx` (đầu file `// @vitest-environment jsdom`, `QueryClientProvider`, API giả): đang tải quota; lỗi quota → vẫn bấm được nút + có nút thử lại; `remaining 17` → "Còn 17/20 lượt AI hôm nay", nút bật; `remaining 0` → nút `disabled` + thông báo hết lượt; `globalExhausted` → nút `disabled` + thông báo hệ thống (khác thông báo cá nhân); submit nhận 429 `AI_DAILY_LIMIT_USER` → thông báo có giờ reset và quota được fetch lại; 429 `AI_RATE_LIMIT` → thông báo dồn dập, không nhắc ngày; **khi hết lượt, form thêm câu hỏi nhập tay vẫn bật** (render cạnh nhau hoặc khẳng định panel không chặn thành phần nhập tay). (Q6, FR-009, FR-010)
- [x] T029 [US1] Rà giao diện: nếu máy có skill `design-taste-frontend`, chạy `lint.mjs --app` trên file đã đổi, sửa hết FAIL; kiểm tay 360px (điện thoại) không cuộn ngang, nút cao 44px; `GenerateQuestionsPanel.tsx` ≤ ~150 dòng (tách nếu vượt). (CLAUDE.md giao diện) **Kết quả**: `lint.mjs --app` 0 FAIL, 0 WARN cho GenerateQuestionsPanel.tsx và AiQuotaBadge.tsx; trình duyệt thật (CDP) ở 360px: scrollWidth = clientWidth = 360 (không cuộn ngang), nút Sinh cao 44px, thông báo hết lượt xuống dòng gọn. Panel 94 dòng.

**Checkpoint Đợt 3**:
- [x] T029a `npm run build` + `npm test` xanh; commit `feat: hiện số lượt AI còn lại và tắt nút khi hết lượt` + push.

---

## Đợt 4: Chốt — migration production, tài liệu, nghiệm thu

- [ ] T030 [P] `.env.example`: thêm `Ai__DailyLimitPerUser=20`, `Ai__DailyLimitGlobal=300` (kèm chú thích: global nên đặt theo RPD của key Gemini); cập nhật tài liệu triển khai nếu có liệt kê biến `Ai__*`. (Q1, Q7)
- [ ] T031 Xuất SQL idempotent: `dotnet ef migrations script --idempotent` → `specs/004-ai-daily-limit/migration.idempotent.sql`; kiểm tra không có `DROP`/`ALTER` bảng cũ; chạy file đó HAI LẦN liên tiếp trên Postgres dev, lần hai không lỗi. Ghi trong mô tả PR bước **bắt buộc**: chủ dự án chạy script lên Supabase qua Session pooler (cổng 5432) TRƯỚC khi merge vào `main`. Claude KHÔNG chạy lên production. (Q7)
- [ ] T032 Chạy trọn quickstart.md (kịch bản tay + checklist), build + test hai phía xanh (`dotnet test`, `npm test`, `npm run build`); đối chiếu bảng quyết định → task bên dưới đủ; commit `docs: hoàn thiện tài liệu giới hạn AI theo ngày` + push; đề xuất commit/PR cuối. (SC-008)

---

## Dependencies & Execution Order

- **Đợt 1**: T001/T002 [P], T003 [P], T005 [P] song song → T004 (sau T003) → T006 (sau T003–T005) → T007 → T008–T012 (cùng file, viết tuần tự hoặc song song rồi merge) → T013 (sau T008, T009 xanh) → T013a.
- **Đợt 2**: cần Đợt 1 đã push. T014, T015, T021 (phần controller/test), T022 [P] song song → T016 (sau T005, T006, T014, T015); T016a [P] độc lập (chỉ cần file generator) → T017 (sau T016 và T016a) → T018/T019 → T020 (cần T006, T017) → T023 → T023a.
- **Đợt 3**: chỉ cần contract (`contracts/ai-quota.md`), có thể làm song song Đợt 2 nếu cần demo sớm. T024–T026 [P] → T027 → T028 → T029 → T029a. Chạy với backend thật chỉ sau T023a.
- **Đợt 4**: sau mọi đợt. T030 [P]; T031 sau T004; T032 cuối cùng.

### Parallel Opportunities

T001–T003, T005; T014, T015, T021, T022; T024–T026; T030.

## Implementation Strategy

- **MVP = Đợt 1 + 2**: chặn đúng ở server, bảo vệ quota Gemini ngay cả khi chưa có giao diện.
- Dừng ở bất kỳ checkpoint nào vẫn có sản phẩm chạy được (frontend cũ chỉ hiện lỗi 429 chung nếu Đợt 3 chưa xong).
- Merge vào `main` chỉ sau T031 (script đã chạy trên Supabase) — nếu không, `generate` trên production trả 500 vì chưa có bảng.

---

## Bảng đối chiếu: quyết định → task

| Quyết định | Nội dung | Task bao phủ |
|---|---|---|
| Q1 | Trần 20/user, 300/global, cấu hình `Ai:DailyLimit*`, validator | T015, T016, T021 (remaining), T030 |
| Q2 | Ngày VN UTC+7, reset 00:00 VN, không phụ thuộc múi giờ máy chủ | T001, T002, T016, T020 (23:59:59 / 00:00) |
| Q3 | Một lượt = một request, bất kể 1 hay 2 lần gọi Gemini | T017 (giữ chỗ đúng 1 lần quanh `GenerateValidCandidatesAsync`), T019 (`Reserve` gọi 1 lần/request), T007 |
| Q4 | Giữ chỗ nguyên tử trước Gemini; global+user cùng nguyên tử, không lệch số | T005, T006, T007–T010, T013 (mutation), T017 |
| Q5 | 429 có `code` + `resetAt`; giữ policy 10/giờ; `AI_RATE_LIMIT` | T014, T022, T023, T026, T028 |
| Q6 | GET quota; frontend "Còn X/20", tắt nút khi hết; nhập tay không giới hạn | T021, T024–T029 |
| Q7 | Migration, SQL idempotent chạy Supabase trước merge, không thêm thư viện | T004, T031, T030, plan.md (mục Migration) |
| A | Không hoàn khi hủy; chỉ hoàn user, không bao giờ global; hoàn dùng đúng `Day`; `GREATEST(…,0)` | T005 (interface không có hoàn global), T006, T011, **T016a (generator không biến hủy request thành lỗi AI)**, T017 (kể cả điều kiện `!IsCancellationRequested`), T019, T020 (kể cả `ClientCancelledDuringRealGeminiCall`) |
| B | `Scope` chuỗi NOT NULL, khóa `(Day, Scope)`, không `UserId` nullable | T003, T004, research.md R2 |
| C | Commit giữ chỗ trước Gemini; hoàn chỉ đụng dòng user; thứ tự global → user | T006, T012 (xen kẽ), T017, research.md R4–R5 |
| D | Kiểm tra rẻ chạy trước giữ chỗ, có test | T017, T018 |
| E | Test Postgres thật: đồng thời user & global, xen kẽ, ranh giới ngày, hủy không hoàn, hoàn khi Gemini lỗi, mutation đỏ | T008, T009, T012, T002, T011, T020, T013, T019 |

Đối chiếu yêu cầu test bắt buộc ban đầu: test đồng thời N song song trần K (T008) + chứng minh đỏ khi bỏ `WHERE` (T013); ranh giới 23:59/00:00 VN (T002, T020); hoàn lượt khi Gemini lỗi (T019, T020).
