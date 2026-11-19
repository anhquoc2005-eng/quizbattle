# Feature Specification: Giới hạn AI theo ngày

**Feature Branch**: `feat/ai-daily-limit`

**Created**: 2026-11-19

**Status**: Draft

**Input**: User description: "Giới hạn AI theo ngày (ai-daily-limit). Trần mỗi user 20 lượt generate/ngày, trần toàn hệ thống 300 lượt/ngày, đếm bằng Postgres (không dùng RAM), ngày tính theo giờ Việt Nam. Giữ chỗ lượt TRƯỚC khi gọi Gemini bằng câu lệnh nguyên tử; hoàn lượt user khi sinh thất bại hoàn toàn. Vượt trần → 429 ProblemDetails có `code` và `resetAt`. API GET số lượt còn lại; frontend hiện 'Còn X/20 lượt AI hôm nay'. Câu hỏi nhập tay không bị giới hạn. Giữ nguyên policy 10/giờ. Có migration, không thêm thư viện."

## Bối cảnh

Endpoint `POST /api/rooms/{code}/questions/generate` gọi Gemini (trả phí / có quota theo ngày). Hiện chỉ có
policy `generate-questions` ([Program.cs:114](../../backend/QuizBattle.Api/Program.cs#L114)):
FixedWindow 10 request/giờ theo user, lưu trong RAM của tiến trình. Ba lỗ hổng:

1. Bộ đếm nằm trong RAM → mất khi Render ngủ hoặc deploy lại, nên trần thực tế có thể bị làm mới bất cứ lúc nào.
2. Không có trần theo ngày cho mỗi user, cũng không có trần toàn hệ thống. Đăng ký miễn phí nên một người
   tạo nhiều tài khoản vẫn đốt quota Gemini của cả dự án.
3. Mỗi request có thể gọi Gemini tối đa 2 lần ([QuestionService.cs:13](../../backend/QuizBattle.Application/Questions/QuestionService.cs#L13),
   `MaxGenerateAttempts`), nên "10 request/giờ" thực chất là tối đa 20 lần gọi/giờ.

Tính năng này thêm bộ đếm bền trong PostgreSQL (qua EF Core migration), giữ chỗ lượt nguyên tử trước khi gọi
Gemini, và cho chủ phòng thấy mình còn bao nhiêu lượt.

## Quyết định sản phẩm đã chốt *(không tự thay đổi)*

| # | Quyết định |
|---|---|
| Q1 | Trần mỗi user: **20** lượt generate/ngày. Trần toàn hệ thống: **300** lượt/ngày. Cấu hình `Ai:DailyLimitPerUser`, `Ai:DailyLimitGlobal` (có mặc định 20/300; `ProductionConfigValidator` chỉ kiểm "nếu có đặt thì là số nguyên > 0", không bắt buộc phải có). |
| Q2 | "Ngày" tính theo giờ Việt Nam (UTC+7), reset 00:00 VN. Tính bằng độ lệch cố định +7h trên UTC, KHÔNG phụ thuộc múi giờ máy chủ. |
| Q3 | Một lượt = một request `POST generate` được nhận và đã qua các kiểm tra rẻ, bất kể bên trong gọi Gemini 1 hay 2 lần. |
| Q4 | Giữ chỗ lượt TRƯỚC khi gọi Gemini bằng câu lệnh nguyên tử trong Postgres. Trần toàn hệ thống và trần user cùng nằm trong MỘT transaction: không thể trừ bên này mà bên kia từ chối. |
| Q5 | Vượt trần → `429` ProblemDetails có `code` (`AI_DAILY_LIMIT_USER` / `AI_DAILY_LIMIT_GLOBAL`) và `resetAt`. Giữ nguyên policy 10/giờ; 429 của policy đó thêm `code = AI_RATE_LIMIT` để client phân biệt. |
| Q6 | API GET trả số lượt còn lại hôm nay. Frontend hiện "Còn X/20 lượt AI hôm nay" cạnh nút sinh câu hỏi; hết lượt thì tắt nút kèm thông báo rõ. Câu hỏi nhập tay KHÔNG bị giới hạn. |
| Q7 | Có migration; plan ghi bước xuất SQL idempotent và chạy lên Supabase TRƯỚC khi merge. Không thêm thư viện. |

### Chỉnh sửa theo cố vấn (thay thế/bổ sung Q3–Q4)

| # | Quyết định |
|---|---|
| A | **Hoàn lượt**: chỉ hoàn dòng USER, và chỉ khi sinh thất bại hoàn toàn (`BadGatewayException` hoặc `AiUnavailableException`). KHÔNG hoàn khi request bị hủy (client đóng tab / `CancellationToken`): Gemini đã tốn tiền, hoàn sẽ thành lỗ hổng lách trần vô hạn. KHÔNG BAO GIỜ hoàn dòng GLOBAL: trần global đo số lần đã thực sự gọi Gemini, lần thất bại vẫn tốn quota. Hoàn dùng đúng `Day` đã giữ chỗ (lấy từ kết quả reserve, không tính lại), `Count = GREATEST(Count - 1, 0)`. |
| B | **Data model**: cột `Scope` kiểu chuỗi NOT NULL (`'global'` hoặc `'user:{guid}'`), khóa chính `(Day, Scope)`. Không dùng `UserId` nullable (NULL trong unique index của Postgres không xung đột → `ON CONFLICT` hỏng với dòng global). Lý do ghi ở research.md. |
| C | **Khóa/transaction**: transaction giữ chỗ (global trước, user sau) COMMIT TRƯỚC khi gọi Gemini, không giữ khóa trong lúc chờ AI. Hoàn lượt chỉ đụng dòng user nên không có nguy cơ khóa chéo. Nếu sau này cần đụng cả hai dòng thì phải đúng thứ tự global → user. |
| D | **Thứ tự kiểm tra**: mọi kiểm tra rẻ (phòng tồn tại, đúng chủ phòng, trạng thái Draft, vượt `MAX_QUESTIONS_PER_ROOM`, validation request) chạy TRƯỚC khi giữ chỗ. Request bị từ chối vì các lý do này không mất lượt. |
| E | **Test bắt buộc** (Postgres thật): đồng thời trên dòng user, đồng thời trên dòng global, reserve/refund xen kẽ không deadlock, ranh giới ngày 23:59:59/00:00 VN, hủy request không hoàn lượt, hoàn lượt khi Gemini lỗi, và bằng chứng test đỏ khi bỏ điều kiện `WHERE Count < limit`. |

## Đối chiếu với code thật

- Trình tự `GenerateAsync` hiện tại ([QuestionService.cs:21-31](../../backend/QuizBattle.Application/Questions/QuestionService.cs#L21)):
  lấy phòng (đúng chủ + Draft) → đếm câu → lấy danh sách câu hiện có → gọi Gemini → chèn có khóa phòng. Điểm giữ
  chỗ nằm giữa "lấy danh sách câu hiện có" và "gọi Gemini" (đúng yêu cầu D). Validation `Count` chạy ở model
  binding, trước cả controller.
- `GenerateValidCandidatesAsync` ném `BadGatewayException` (không có câu hợp lệ sau 2 lần) hoặc để lọt
  `AiUnavailableException` (Gemini không gọi được, thiếu cấu hình) — hai điểm kích hoạt hoàn lượt user.
- Sau khi Gemini trả về, `InsertWithRoomLockAsync` vẫn có thể ném `ConflictException` (phòng vừa rời Draft, đầy chỗ).
  Lúc đó Gemini đã được gọi thành công → KHÔNG hoàn (xem Điểm cần chốt 1).
- `GlobalExceptionHandler` đã có cơ chế `Extensions["code"]`; thêm một nhánh 429 cho `AiDailyLimitException`.
- `OnRejected` của rate limiter ([Program.cs:127](../../backend/QuizBattle.Api/Program.cs#L127)) dùng chung cho mọi
  policy và chưa có `code`; chỉ policy `generate-questions` sẽ thêm `AI_RATE_LIMIT`.
- Dự án đã dùng `TimeProvider` (Program, RoomService, GameService...) → giữ nguyên cách lấy giờ, test dùng đồng hồ giả.
- Mẫu test tích hợp: `Integration/SubmitAnswerSnapshotTests.cs` + `DatabaseFixture` (database tạm
  `qb_test_<guid>`, migrate bằng migration thật, `RequiresDatabaseFact` tự bỏ qua khi thiếu `QUIZBATTLE_TEST_DB`).
- `ApiError` ở frontend đã có `code`; cần thêm đọc `resetAt` từ ProblemDetails.

## User Scenarios & Testing

### User Story 1 — Chủ phòng thấy mình còn bao nhiêu lượt AI (Priority: P1)

Chủ phòng mở trang phòng (Draft), cạnh nút "Sinh câu hỏi" thấy "Còn 17/20 lượt AI hôm nay".

**Why this priority**: Giới hạn mà người dùng không nhìn thấy sẽ trông như lỗi ngẫu nhiên.

**Independent Test**: Sinh một lần → số còn lại giảm 1 sau khi request xong; mở trang bằng tài khoản khác → số riêng.

**Acceptance Scenarios**:

1. **Given** user chưa dùng lượt nào hôm nay, **When** mở trang phòng, **Then** thấy "Còn 20/20 lượt AI hôm nay".
2. **Given** user đã dùng 20 lượt, **When** mở trang phòng, **Then** nút "Sinh câu hỏi" bị tắt và có thông báo "đã dùng hết lượt hôm nay, làm mới lúc 00:00 (giờ Việt Nam)"; form thêm câu hỏi nhập tay vẫn dùng bình thường.
3. **Given** trần toàn hệ thống đã hết, **When** mở trang phòng, **Then** nút tắt kèm thông báo khác ("hệ thống AI hôm nay đã hết lượt"), không nhầm với hết lượt cá nhân.
4. **Given** đang tải hoặc lấy số lượt lỗi, **Then** hiện trạng thái tải / lỗi có nút thử lại; nút Sinh KHÔNG bị chặn (server là nguồn sự thật).

### User Story 2 — Server chặn đúng và nguyên tử khi vượt trần (Priority: P1)

**Independent Test**: N request song song với trần K → đúng K được chấp nhận, bộ đếm trong DB = K.

**Acceptance Scenarios**:

1. **Given** user còn 1 lượt, **When** gửi 5 request generate đồng thời, **Then** đúng 1 request gọi Gemini, 4 request nhận `429 AI_DAILY_LIMIT_USER`.
2. **Given** trần global còn 3 lượt và 10 user khác nhau gọi đồng thời, **Then** đúng 3 được chấp nhận, 7 nhận `429 AI_DAILY_LIMIT_GLOBAL`, và không user nào bị trừ lượt mà không có lượt global tương ứng.
3. **Given** `23:59:59` giờ VN, **When** giữ chỗ rồi Gemini lỗi sau `00:00`, **Then** lượt hoàn về đúng dòng của ngày cũ; ngày mới không bị cộng bù.
4. **Given** request bị hủy giữa lúc chờ Gemini, **Then** lượt vẫn bị tính.

### User Story 3 — Lỗi hệ thống không ăn mất lượt của người dùng (Priority: P2)

**Acceptance Scenarios**:

1. **Given** Gemini lỗi hoặc không có câu hợp lệ nào sau 2 lần thử, **When** request trả 502/503, **Then** lượt của user được hoàn (số còn lại về như trước), còn lượt global thì không.
2. **Given** request bị từ chối vì phòng không phải Draft / không phải chủ phòng / vượt 50 câu, **Then** số lượt không đổi.

### Edge Cases

- Đổi `Ai:DailyLimitPerUser` xuống thấp hơn số đã dùng giữa ngày: user bị từ chối đến hết ngày, không báo lỗi.
- Hai tab cùng gửi: tab nào đến sau nhận 429; số lượt trên giao diện làm mới sau mỗi lần generate (kể cả khi lỗi).
- Tiến trình chết giữa lúc giữ chỗ và hoàn lượt: lượt mất (chấp nhận, xem Rủi ro).
- Hai lần hoàn liên tiếp cho cùng một lần giữ chỗ không xảy ra (mỗi request hoàn tối đa một lần); `GREATEST(..., 0)` là lưới an toàn, không phải cơ chế chính.

## Requirements

### Functional Requirements

- **FR-001**: Hệ thống MUST đếm lượt generate theo (ngày VN, user) và (ngày VN, toàn hệ thống) trong PostgreSQL, không dùng RAM.
- **FR-002**: Ngày MUST được tính bằng `UTC + 7h` bằng hàm thuần nhận thời gian làm tham số; `resetAt` là 00:00 +07:00 của ngày kế tiếp.
- **FR-003**: Giữ chỗ MUST là một transaction: tăng dòng global (có điều kiện `Count < limit`), rồi tăng dòng user (có điều kiện); câu nào bị từ chối thì toàn bộ rollback. Transaction MUST commit trước khi gọi Gemini.
- **FR-004**: Mọi kiểm tra rẻ (D) MUST chạy trước giữ chỗ.
- **FR-005**: Khi `BadGatewayException` hoặc `AiUnavailableException`, hệ thống MUST hoàn đúng 1 lượt cho dòng user của đúng `Day` đã giữ chỗ; MUST NOT hoàn dòng global; MUST NOT hoàn khi bị hủy hoặc khi lỗi xảy ra sau khi Gemini đã trả kết quả hợp lệ.
- **FR-006**: Vượt trần MUST trả `429` ProblemDetails `{ type, title, status, detail?, code, resetAt }`, `code` ∈ {`AI_DAILY_LIMIT_USER`, `AI_DAILY_LIMIT_GLOBAL`}; khi cả hai cùng hết, `AI_DAILY_LIMIT_GLOBAL` được ưu tiên (kiểm global trước).
- **FR-007**: Policy 10 request/giờ MUST giữ nguyên; 429 của nó MUST có `code = AI_RATE_LIMIT`.
- **FR-008**: `GET /api/ai-quota` (yêu cầu đăng nhập) MUST trả `{ dailyLimit, used, remaining, globalExhausted, resetAt }`; MUST NOT lộ con số global.
- **FR-009**: Frontend MUST hiện "Còn X/Y lượt AI hôm nay", tắt nút khi hết lượt (user hoặc global), xử lý đủ trạng thái tải/lỗi/rỗng, và làm mới số lượt sau mỗi lần generate.
- **FR-010**: Thêm/sửa/xóa/duyệt câu hỏi nhập tay MUST NOT bị giới hạn bởi tính năng này.
- **FR-011**: `Ai:DailyLimitPerUser`, `Ai:DailyLimitGlobal` MUST là số nguyên > 0 khi được đặt; giá trị sai khiến backend không khởi động.

### Key Entities

- **AiUsage** (bảng `AiUsageDaily`): `Day` (date, ngày VN), `Scope` (`'global'` | `'user:{guid}'`), `Count` (int ≥ 0). Khóa `(Day, Scope)`. Chi tiết ở data-model.md.

## Success Criteria

- **SC-001**: N request song song, trần K → đúng K thành công, `Count` trong DB = K (test Postgres thật, cả dòng user lẫn global).
- **SC-002**: Với trần global K và N user khác nhau song song: dòng global = K và tổng các dòng user = K.
- **SC-003**: Xóa điều kiện `WHERE Count < limit` khỏi câu SQL làm SC-001/SC-002 đỏ (đã chạy thử, ghi kết quả).
- **SC-004**: Reserve và refund chạy xen kẽ song song hoàn tất không deadlock, số cuối đúng.
- **SC-005**: Giữ chỗ lúc 23:59:59 VN, hoàn sau 00:00 VN → dòng ngày cũ giảm 1, dòng ngày mới không đổi.
- **SC-006**: Request bị hủy giữa chừng → `Count` user và global đều không giảm; Gemini lỗi → user giảm 1, global không giảm.
- **SC-007**: Request bị từ chối bởi kiểm tra rẻ → không có dòng nào trong `AiUsageDaily` đổi.
- **SC-008**: Build + test hai phía xanh; SQL idempotent chạy hai lần liên tiếp không lỗi trên Postgres dev.

## Rủi ro đã chấp nhận

- **Mất lượt khi tiến trình chết** giữa giữ chỗ và hoàn lượt: chấp nhận (hiếm, lệch về phía an toàn cho chi phí).
- **Một nhóm nhỏ dùng hết trần global**: 15 user × 20 lượt = 300, người còn lại bị chặn đến 00:00 VN. Giữ 300/20 theo quyết định; ghi nhận rủi ro.
- **Lượt ≠ lần gọi Gemini**: 1 lượt có thể là 2 lần gọi, nên 300 lượt/ngày có thể là 600 request Gemini. Trần global nên đặt theo RPD (requests per day) của key Gemini đang dùng — sẽ kiểm sau khi biết gói; hiện chưa kiểm.
- **Điểm nóng một dòng**: mọi giữ chỗ khóa dòng global trong vài ms (đến COMMIT). Với tối đa 300 lượt/ngày thì không đáng kể.

## Điểm cần chốt còn lại

1. **Lỗi SAU khi Gemini trả kết quả hợp lệ** (`ConflictException` ở bước chèn: phòng vừa rời Draft hoặc vừa đủ 50 câu do request khác): đề xuất KHÔNG hoàn (Gemini đã tốn tiền, câu hỏi hợp lệ đã sinh). Ngược lại, `AiUnavailableException` do thiếu cấu hình (`Ai:ApiKey` rỗng) vẫn hoàn user nhưng global vẫn bị trừ dù thực tế không có lần gọi nào — chấp nhận vì production đã bắt buộc có `Ai:ApiKey`.
2. **`GET /api/ai-quota` gắn theo user (không theo phòng)** và chỉ trả `globalExhausted` (boolean), không trả con số global để tránh lộ mức sử dụng toàn hệ thống.
3. **Policy 10/giờ vẫn đếm mọi request** kể cả request sau đó bị từ chối vì hết lượt ngày hoặc kiểm tra rẻ (vì nó chạy trước controller). Chấp nhận: nó chỉ là lớp chống dồn dập.

## Ngoài phạm vi

Job dọn bảng `AiUsageDaily` (mỗi user mỗi ngày một dòng, rất nhỏ), giới hạn câu hỏi nhập tay, trang quản trị xem mức dùng toàn hệ thống, đổi nhà cung cấp AI, thêm thư viện, WebApplicationFactory.
