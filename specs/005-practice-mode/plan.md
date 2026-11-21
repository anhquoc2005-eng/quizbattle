# Implementation Plan: Chế độ ôn tập

**Branch**: `feat/practice-mode` | **Date**: 2026-11-21 | **Spec**: [spec.md](./spec.md)

## Summary

Thêm 3 bảng (`PracticeSessions`, `PracticeAnswers`, `PracticeXpDaily`) và 4 endpoint REST dưới `/api/practice`. Server chọn và xáo các câu đã chơi của phòng `Finished`, gửi từng câu KHÔNG kèm đáp án, chấm khi nộp và thưởng `5` XP mỗi câu đúng với trần `100` XP/ngày VN. Mỗi lần nộp là MỘT transaction: khóa `Users` (`FOR UPDATE`) → (đã chấm thì phát lại kết quả đã lưu) → chấm → tăng bộ đếm ngày bằng `INSERT ... ON CONFLICT DO UPDATE ... WHERE ... RETURNING` → ghi `PracticeAnswers` → cộng `Users.TotalXp`. Nộp lại là idempotent. Mỗi (user, phòng) một lượt đang mở. Không Xu, không SignalR, không thêm thư viện. Một migration (chưa viết ở đợt spec).

## Technical Context

**Language/Version**: C# / .NET 8; TypeScript 5 + React 18
**Dependencies**: ASP.NET Core 8, EF Core + Npgsql, TanStack Query (đã có). KHÔNG thêm gói mới.
**Storage**: PostgreSQL, 1 migration `AddPracticeMode`
**Testing**: xUnit (unit, tích hợp Postgres thật qua `DatabaseFixture`/`RequiresDatabaseFact`, HTTP qua `ApiFactory`); Vitest + jsdom
**Cấu hình mới**: `Practice:XpPerCorrect` (5), `Practice:DailyXpCap` (100); `PracticeOptions` + `ValidateOnStart` (`>= 0`); thêm `.env.example`; KHÔNG vào `ProductionConfigValidator` (Ch9)

## Constitution Check (CLAUDE.md)

| Quy tắc | Đáp ứng |
|---|---|
| Controller → Service → Repository | `PracticeController` → `IPracticeService` → `IPracticeRepository` |
| Logic thuần ở Domain, `now` truyền vào | `PracticeRewardCalculator` (hàm thuần), `VietnamDay` tái dùng |
| Không trả Entity, DTO riêng | `PracticeQuestionDto` không có `CorrectIndex`; `PracticeAnswerResultDto`/`PracticeResultDto` có |
| Khóa Users theo cùng thứ tự | Chỉ một dòng Users của chính user, không khóa Room (R4) |
| Bất biến XP | Thành tổng 2 nguồn; sửa `CLAUDE.md:60` cùng PR migration (Ch1) |
| Rate limit | Policy `practice` theo user |
| Async, AsNoTracking cho đọc, tránh N+1 | Danh sách phòng bằng một truy vấn projection |
| Frontend: TanStack Query, 3 trạng thái, không tự lưu bản sao server vào `useState` | Có |
| Thiết kế true-black-flat | Dùng token và class có sẵn; `accent` chỉ cho dữ liệu (thanh tiến độ XP) |

## Project Structure

### Documentation
`spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/practice-api.md`, `quickstart.md`, `tasks.md`.

### Source Code (dự kiến, chưa tạo)
```
backend/
  QuizBattle.Domain/
    Entities/PracticeSession.cs, PracticeAnswer.cs, PracticeXpDaily.cs
    PracticeRewardCalculator.cs        # hàm thuần: được thưởng không, bao nhiêu (trước khi cắt trần), lý do
  QuizBattle.Application/Practice/
    IPracticeService.cs, PracticeService.cs, IPracticeRepository.cs, PracticeOptions.cs
    DTO: PracticeRoomsResponse, PracticeSessionDto, PracticeQuestionDto, PracticeResultDto (lastResult),
         PracticeAnswerResultDto, PracticeSummaryDto, PracticeSessionRef
  QuizBattle.Infrastructure/Persistence/
    PracticeRepository.cs              # transaction + SQL thô; cờ internal chỉ test dùng (mutation)
    Configurations/Practice*Configuration.cs
    Migrations/…AddPracticeMode
  QuizBattle.Api/Controllers/PracticeController.cs   # + policy rate limit "practice" ở Program.cs
frontend/src/
  api/practice.ts, types/practice.ts
  pages/Practice/ (PracticeRooms, PracticeQuestion, PracticeSummary, practiceMessages.ts)
  App.tsx (route /practice, /practice/:sessionId, ProtectedRoute)
```

## Luồng nộp đáp án (một transaction)

```
BEGIN
 1. SELECT * FROM "Users" WHERE "Id" = @me FOR UPDATE            -- tuần tự hóa mọi ghi Users của user này
 2. Đọc session (phải thuộc @me); questionId phải thuộc session.QuestionIds, không thì 404
 3. Đã có PracticeAnswers (SessionId, QuestionId)?
      CÓ  -> trả 200 kết quả đã lưu (IsCorrect, XpAwarded, XpReason, CorrectIndex); không ghi gì; COMMIT/kết thúc   [replay]
 4. questionId != câu hiện tại -> 409 not_current_question
 5. isCorrect = selectedIndex == Questions.CorrectIndex           -- chỉ ở server
 6. now -> day = VietnamDay.DayOf(now)
    desired, reason = PracticeRewardCalculator(isCorrect, alreadyRewardedTodayForThisQuestion, XpPerCorrect)
 7. desired > 0 && cap > 0 ? reserve SQL (LEAST) : granted = 0
    reason cuối = granted == desired ? reason : (granted > 0 ? daily_cap_partial : daily_cap)
 8. INSERT PracticeAnswers (XpAwarded = granted, XpReason = reason cuối, Day = day, ...)
 9. Users.TotalXp += granted  (entity đã khóa & theo dõi)
10. Nếu không còn câu nào còn tồn tại chưa nộp: Session.CompletedAt = now
11. SaveChanges; COMMIT      -- lỗi ở bất kỳ bước nào: rollback cả bộ đếm lẫn dòng XP
```

- Khác `AiQuotaRepository` (`AiQuotaRepository.cs:25-39`): bên đó commit giữ chỗ trước khi gọi Gemini nên cần `RefundUserAsync`; ở đây không có bước gọi ngoài, không tách, không cần hoàn.
- KHÔNG có nhánh bắt unique-violation (research R12): khóa `Users` tuần tự hóa nên nhánh đó không chạy được. Hai khóa duy nhất của `PracticeAnswers` là lưới DB; vi phạm = 500.
- Câu bị xóa giữa lượt (Ch8): bước 10 tính "còn tồn tại" theo `Questions` hiện có; `GET` cũng tự đóng lượt khi không còn câu nào để làm (chỉ ghi `CompletedAt`, không đụng XP).

### Câu lệnh giữ chỗ XP (mẫu `AiQuotaRepository.cs:13-20`)
```sql
INSERT INTO "PracticeXpDaily" ("UserId", "Day", "Xp")
VALUES (@user, @day, LEAST(@xp, @cap))
ON CONFLICT ("UserId", "Day")
DO UPDATE SET "Xp" = "PracticeXpDaily"."Xp" + LEAST(@xp, @cap - "PracticeXpDaily"."Xp")
WHERE "PracticeXpDaily"."Xp" < @cap
RETURNING "Xp"
```
`granted` = `Xp` trả về − `Xp` đọc trước đó trong cùng transaction (0 nếu chưa có dòng). Không có dòng trả về = đã chạm trần, `granted = 0`. Dùng `DbCommand` thô (EF bọc `SqlQuery` trong `SELECT`, xem `AiQuotaRepository.cs:74-86`).

### Xử lý phần còn lại < XP của câu: `LEAST` (Ch3)
- Lý do: trần là "tối đa X XP/ngày", không phải "chỉ nhận theo nấc"; bộ đếm đạt đúng trần, không kẹt ở trần−1 vì bước nhảy không khớp. Câu đúng không bị phạt 0 XP chỉ vì vừa chạm ngưỡng. Dễ giải thích: "còn 3 XP thì nhận 3".
- Phương án "từ chối cả câu" bị loại: cùng một câu đúng có/không có XP tùy thứ tự, và cấu hình không chia hết (cap 100, 7 XP/câu) không bao giờ chạm trần đúng bằng 100.
- Ở cấu hình mặc định (5 và 100) nhánh này không xảy ra, nên test và mutation BẮT BUỘC dùng cấu hình không chia hết (`XpPerCorrect = 7`, `DailyXpCap = 10`: 7, rồi 3, rồi 0).

## Luồng `POST /sessions` (Ch7)

```
Service: kiểm điều kiện phòng, chọn câu đã chơi, xáo -> QuestionIds     (ngoài transaction, chỉ đọc)
BEGIN  (READ COMMITTED, đặt tường minh)
 1. INSERT INTO "PracticeSessions" (...) VALUES (...)
      ON CONFLICT ("UserId","RoomId") WHERE "CompletedAt" IS NULL DO NOTHING RETURNING "Id"
      có dòng -> 201 + Id mới
 2. không có dòng -> SELECT "Id" WHERE UserId=@u AND RoomId=@r AND CompletedAt IS NULL   (câu lệnh RIÊNG) -> 200
COMMIT
```
Lý do READ COMMITTED và SELECT riêng: research R11.

## Lối cày và cách chặn

| # | Lối cày/gian lận | Cách chặn hoặc lý do chấp nhận |
|---|---|---|
| C1 | Mở nhiều lượt ôn để dò đáp án rồi trả lời đúng ở lượt sau | Đáp án luôn lộ sau khi nộp (bản chất ôn tập) nên dò không tránh được. Chặn phần lợi: XP một lần cho mỗi (user, câu, ngày VN) + trần 100/ngày. Học thuộc rồi ôn lại hôm sau vẫn được XP: chấp nhận (xem "Hạn chế đã biết"). Mỗi (user, phòng) chỉ một lượt mở nên không mở hàng loạt trên một phòng. Có rate limit. |
| C2 | Nộp đồng thời cùng một câu để cộng XP hai lần | Khóa `Users FOR UPDATE` tuần tự hóa; request sau thấy dòng đã lưu và phát lại (200, XP một lần). Hai khóa duy nhất ở DB là lưới cuối (500 nếu lỗi lập trình). |
| C3 | Nộp đồng thời nhiều câu khác nhau để vượt trần | Khóa Users tuần tự hóa; câu SQL `WHERE Xp < cap` + `LEAST` tự nó không cho vượt trần dù thiếu khóa (chứng minh bằng M1/M4). |
| C4 | Phòng tự tạo một mình, tự viết câu dễ, tự chơi | Phòng phải có `MatchReward` `Awarded` (cần >= 2 tài khoản không phải chủ phòng đã trả lời, `RewardCalculator.cs:25,29-33`). Dùng vài tài khoản phụ vẫn qua được: chấp nhận vì trần 100 XP/ngày/tài khoản. |
| C5 | Đọc đáp án từ payload | DTO câu hỏi không có thuộc tính `CorrectIndex`; test phản chiếu và test payload thô. |
| C6 | Lượt của người khác (đoán `sessionId`) | Mọi truy vấn ràng buộc `UserId`; không thuộc user thì 404. |
| C7 | Khách gọi API | `[Authorize]` theo JWT người dùng; token khách → 401. |
| C8 | Đổi đồng hồ/múi giờ để lấy thêm ngày | `now` và ngày VN tính ở server; client không gửi thời gian. |
| C9 | Spam endpoint | Rate limit policy `practice` theo user. |
| C10 | Mở hàng loạt lượt dở để làm phình bảng | Mỗi (user, phòng) một lượt mở (unique index một phần); số lượt mở tối đa = số phòng của user. |
| C11 | Gửi lại cùng đáp án (hoặc đáp án khác) để kiếm lại XP hoặc đổi từ sai thành đúng | Idempotent: trả kết quả đã lưu, bỏ qua `selectedIndex` mới, không ghi gì. |

## Kế hoạch test

**Unit (xUnit)**
- `PracticeRewardCalculator`: đúng/sai, đã nhận hôm nay, `xpPerCorrect`, `xpPerCorrect = 0`.
- Phản chiếu: `PracticeQuestionDto` không có thuộc tính `CorrectIndex`; JSON serialize cũng không có.
- `PracticeOptions`: âm → khởi động thất bại; 0 → hợp lệ; thiếu → mặc định 5/100.
- Tên theo mẫu: `Grant_WrongAnswer_ReturnsZero`.

**Tích hợp Postgres thật (`DatabaseFixture`)**
- Chọn câu: chỉ `Approved` + `EndedAt != null`; phòng không `Finished`, không có `Awarded`, hoặc không liên quan: không ôn được.
- Schema: các nhánh của `CK_PracticeAnswers_XpReason` (hợp lệ và không hợp lệ), unique `(SessionId, QuestionId)`, unique một phần XP/(user, câu, ngày), unique một phần lượt mở, SET NULL khi xóa phòng.
- Bất biến: sau chuỗi trận + ôn, `TotalXp = SUM(MatchRewards.Xp) + SUM(PracticeAnswers.XpAwarded)`, `PracticeXpDaily.Xp = SUM(...)` theo ngày, `CoinBalance`/`CoinTransactions` không đổi.
- Trần đồng thời: N request đồng thời (mỗi request một `DbContext`) → `Xp <= cap` và `TotalXp` khớp.
- Phần còn lại: `XpPerCorrect = 7`, `DailyXpCap = 10` → 7, rồi 3 (`daily_cap_partial`), rồi 0 (`daily_cap`); `DailyXpCap = 0` và `XpPerCorrect = 0` hợp lệ.
- **Idempotent**: nộp lại cùng câu → 200, kết quả đã lưu, XP không đổi; nộp lại với `selectedIndex` khác → vẫn kết quả lần đầu; 2 request đồng thời cùng câu → cả hai 200, cùng kết quả, XP cộng 1 lần, đúng 1 dòng `PracticeAnswers`.
- **Một lượt mở**: 2 `POST /sessions` đồng thời → cùng một `sessionId` (một 201, một 200); lượt đã `Completed` thì POST sau tạo lượt mới; nhiều phòng khác nhau → nhiều lượt mở được.
- `lastResult`: đọc lại sau khi nộp thấy đáp án; chưa nộp thì null.
- Câu bị xóa giữa lượt: lượt kết thúc, tổng kết theo số câu đã nộp (Ch8).
- Xóa phòng sau khi ôn: dòng XP còn, bất biến đúng.
- Qua nửa đêm VN: hai `now` hai bên mốc, bộ đếm riêng.
- Rollback: ép lỗi sau bước tăng bộ đếm (hook `BeforeXpWrite`) → bộ đếm không đổi.

**Mutation (test độ nhạy, theo `ShopPurchaseSensitivityTests.cs:63`; không thêm thư viện)**
Mỗi mutation là một cờ `internal` chỉ test đặt; test tương ứng PHẢI đỏ khi bật cờ và xanh khi tắt (bản không mutate xanh chứng minh test không đỏ vì lý do khác):

| M | Cách mutate | Test phải đỏ |
|---|---|---|
| M1 | Bỏ `WHERE Xp < cap` khỏi câu SQL | Trần đồng thời: `Xp > cap` |
| M2 | Thay `LEAST(@xp, cap - Xp)` bằng `@xp` | Cấu hình 7/10: `Xp = 14 > 10` (phủ đúng nhánh "phần còn lại", Ch3) |
| M3 | "Từ chối cả câu" khi `cap - Xp < xp` thay vì `LEAST` | Cấu hình 7/10: kỳ vọng 7 + 3, quan sát 7 + 0 |
| M4 | `UseUserRowLock = false` + hook ép hai giao dịch đọc cùng lúc | Bất biến `TotalXp` (mất cập nhật) và "2 request cùng câu cả hai 200" (một bên 500 vì unique index); trần vẫn giữ nhờ câu SQL, test khẳng định cả hai điều |
| M5 | Bỏ kiểm tra "đã nhận XP hôm nay cho câu này" | Test cày lại cùng câu trong ngày |
| M6 | Bỏ nhánh phát lại (bước 3) | Test idempotent: nộp lại cộng XP lần hai hoặc đổi kết quả |
| M7 | `POST /sessions` bỏ `ON CONFLICT` (INSERT thường) | Test 2 POST đồng thời cùng `sessionId` (một bên 500 do unique index) |

**HTTP qua `ApiFactory`**
- 401 không token và token khách; 404 phòng không đủ điều kiện, lượt của người khác, câu không thuộc lượt.
- Payload thô `GET .../rooms` và `GET .../sessions/{id}` không chứa `correctIndex` của câu chưa nộp; response nộp và `lastResult` có.
- Idempotent qua HTTP: nộp lần hai cùng body và khác `selectedIndex` đều 200, cùng kết quả; hai request song song cùng câu → cả hai 200.
- 409 `not_current_question` (chỉ cho câu chưa chấm); 400 `selectedIndex` ngoài 0..3.
- `POST /sessions`: 201 lần đầu, 200 + cùng `sessionId` lần sau; hai request song song cùng `sessionId`.
- `GET /sessions/{id}` sau khi nộp có `lastResult`.
- 429 từ policy `practice` (đứng sau `UseAuthorization`).
- Luồng đầy đủ tạo lượt → nộp hết → `summary` đúng, `remainingToday` đúng.

**Frontend (Vitest + jsdom)**
- `PracticeRooms`: tải / lỗi (retry) / rỗng / có dữ liệu; "Tiếp tục" khi có `openSessionId`.
- `PracticeQuestion`: nút disabled khi đang gửi; hiện đúng/sai và đáp án đúng sau nộp; tải lại có `lastResult` vẫn hiện đáp án; gửi lại không vỡ.
- `PracticeSummary`: hiện "+X XP, còn Y hôm nay". Logic thuần (thông điệp theo `xpReason`) test riêng.

## Migration và triển khai
- Migration `AddPracticeMode` tạo 3 bảng; chạy thử ở dev (Docker) trước. Production chạy thủ công có chủ đích, kèm script SQL idempotent như đợt 004.
- `Practice__XpPerCorrect`, `Practice__DailyXpCap` thêm vào `.env.example`.
- Sửa `CLAUDE.md:60` và helper test bất biến cùng PR với migration.

## Rủi ro kỹ thuật cần để mắt
- `granted` = `Xp` trả về trừ `Xp` đọc trước: đúng vì khóa Users đã tuần tự hóa; nếu bỏ khóa thì số báo cáo có thể sai dù trần vẫn đúng (M4 nêu rõ).
- `QuestionIds uuid[]` không có FK: câu bị xóa (xóa phòng) biến mất khỏi lượt đang dở; xử lý theo Ch8.
- `completed`, `xpGainedToday`, `remainingToday` trong response phát lại là số hiện tại (contract), khác với các trường đã lưu.

## Hạn chế đã biết
Xem `spec.md` mục "Hạn chế đã biết" (Ch10: ~3000 XP/tháng cho người chỉ ôn; luật XP một lần/câu/ngày không chặn học thuộc; tài khoản phụ).

## Các điểm đã chốt
Ch1–Ch10 (bảng ở `spec.md`). Không còn điểm cần chốt mở. Hai ghi chú triển khai không cần duyệt thêm: (1) `XpPerCorrect = 0` cho câu đúng ra `XpReason = NULL, XpAwarded = 0` (CHECK cho phép); (2) `DailyXpCap = 0` bỏ qua câu SQL giữ chỗ, ra `daily_cap`.
