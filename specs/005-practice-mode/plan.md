# Implementation Plan: Chế độ ôn tập

**Branch**: `feat/practice-mode` | **Date**: 2026-11-21 | **Spec**: [spec.md](./spec.md)

## Summary

Thêm 3 bảng (`PracticeSessions`, `PracticeAnswers`, `PracticeXpDaily`) và 4 endpoint REST dưới `/api/practice`. Server chọn và xáo các câu đã chơi của phòng `Finished`, gửi từng câu KHÔNG kèm đáp án, chấm khi nộp, và thưởng `5` XP mỗi câu đúng với trần `100` XP/ngày VN. Mỗi lần nộp là MỘT transaction: khóa `Users` (`FOR UPDATE`) → chấm → tăng bộ đếm ngày bằng `INSERT ... ON CONFLICT DO UPDATE ... WHERE ... RETURNING` → ghi `PracticeAnswers.XpAwarded` → cộng `Users.TotalXp`. Không Xu, không SignalR. Không thêm thư viện. Một migration (chưa viết ở đợt này).

## Technical Context

**Language/Version**: C# / .NET 8; TypeScript 5 + React 18
**Dependencies**: ASP.NET Core 8, EF Core + Npgsql, TanStack Query (đã có). KHÔNG thêm gói mới.
**Storage**: PostgreSQL, 1 migration `AddPracticeMode`
**Testing**: xUnit (unit, tích hợp Postgres thật qua `DatabaseFixture`/`RequiresDatabaseFact`, HTTP qua `ApiFactory`); Vitest + jsdom
**Cấu hình mới**: `Practice:XpPerCorrect` (5), `Practice:DailyXpCap` (100); vào `.env.example` và `ProductionConfigValidator` nếu validator liệt kê cấu hình bắt buộc (kiểm tra khi code)

## Constitution Check (CLAUDE.md)

| Quy tắc | Đáp ứng |
|---|---|
| Controller → Service → Repository | `PracticeController` → `IPracticeService` → `IPracticeRepository` |
| Logic thuần ở Domain, `now` truyền vào | `PracticeRewardCalculator` (hàm thuần), `VietnamDay` tái dùng |
| Không trả Entity, DTO riêng | `PracticeQuestionDto` không có `CorrectIndex`, `PracticeAnswerResultDto` có |
| Khóa Users theo cùng thứ tự | Chỉ một dòng Users của chính user, không khóa Room (R4) |
| Bất biến XP | Đổi thành tổng 2 nguồn, sửa `CLAUDE.md:60` (điểm cần chốt #1) |
| Rate limit | Policy `practice` theo user |
| Async, AsNoTracking cho đọc, tránh N+1 | Danh sách phòng bằng một truy vấn projection |
| Frontend: TanStack Query, 3 trạng thái, không tự lưu bản sao server vào `useState` | Có |
| Thiết kế true-black-flat | Dùng token và class có sẵn; `accent` chỉ cho dữ liệu (thanh tiến độ XP) |

## Project Structure

### Documentation
`spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/practice-api.md`, `quickstart.md`. (`tasks.md` do `/speckit-tasks` tạo sau khi duyệt.)

### Source Code (dự kiến, chưa tạo)
```
backend/
  QuizBattle.Domain/
    Entities/PracticeSession.cs, PracticeAnswer.cs, PracticeXpDaily.cs
    PracticeRewardCalculator.cs        # hàm thuần: có thưởng không, bao nhiêu (trước khi cắt trần), lý do
  QuizBattle.Application/Practice/
    IPracticeService.cs, PracticeService.cs, IPracticeRepository.cs
    PracticeOptions.cs                 # XpPerCorrect, DailyXpCap
    DTO: PracticeRoomsResponse, PracticeSessionDto, PracticeQuestionDto, PracticeAnswerResultDto, PracticeSummaryDto
  QuizBattle.Infrastructure/Persistence/
    PracticeRepository.cs              # transaction + SQL thô; cờ internal UseUserRowLock/BeforeXpWrite cho test độ nhạy
    Configurations/Practice*Configuration.cs
    Migrations/…AddPracticeMode
  QuizBattle.Api/Controllers/PracticeController.cs   # + policy rate limit "practice" ở Program.cs
frontend/src/
  api/practice.ts, types/practice.ts
  pages/Practice/ (PracticeRooms, PracticeQuestion, PracticeSummary)
  components/ (tái dùng ErrorRetry, LevelBadge…)
  App.tsx (route /practice, /practice/:sessionId, ProtectedRoute)
```

## Luồng nộp đáp án (một transaction)

```
BEGIN
 1. SELECT * FROM "Users" WHERE "Id" = @me FOR UPDATE            -- tuần tự hóa mọi ghi Users của user này
 2. Đọc session (phải thuộc @me), câu hiện tại = QuestionIds[answeredCount]
    - questionId != câu hiện tại            -> 409 not_current_question
    - đã có PracticeAnswers (SessionId, QuestionId) -> 409 already_answered
 3. isCorrect = selectedIndex == Questions.CorrectIndex           -- chỉ ở server
 4. now -> day = VietnamDay.DayOf(now)
    desired = PracticeRewardCalculator(isCorrect, alreadyRewardedTodayForThisQuestion, XpPerCorrect)
 5. desired > 0 ? reserve SQL (LEAST) : granted = 0
 6. INSERT PracticeAnswers (XpAwarded = granted, Day = day, ...)
 7. Users.TotalXp += granted  (entity đã khóa & theo dõi)
 8. Nếu là câu cuối: Session.CompletedAt = now
 9. SaveChanges; COMMIT      -- lỗi ở bất kỳ bước nào: rollback cả bộ đếm lẫn dòng XP
```

Khác `AiQuotaRepository` (`AiQuotaRepository.cs:25-39`): bên đó commit giữ chỗ trước khi gọi Gemini nên cần `RefundUserAsync`; ở đây không có bước gọi ngoài, không tách, không cần hoàn.

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

### Xử lý phần còn lại < XP của câu: CHỌN `LEAST` (cộng phần còn lại)
- Lý do: trần là "tối đa X XP/ngày", không phải "chỉ nhận theo nấc"; bộ đếm đạt đúng trần, không bao giờ kẹt ở trần−1 vì bước nhảy không khớp. Câu trả lời đúng không bị phạt 0 XP chỉ vì vừa chạm ngưỡng. Hành vi đơn giản để giải thích: "còn 3 XP thì nhận 3".
- Phương án từ chối cả câu bị loại: gây hiện tượng cùng một câu đúng có/không có XP tùy thứ tự, và cấu hình không chia hết (vd. cap 100, 7 XP/câu) sẽ không bao giờ chạm trần đúng bằng 100.
- Ở cấu hình mặc định (5 và 100) nhánh này không xảy ra. Vì vậy test và mutation BẮT BUỘC dùng cấu hình không chia hết (vd. `XpPerCorrect = 7`, `DailyXpCap = 10`: 7, rồi 3, rồi 0) để phủ nhánh `LEAST`.
- Phản hồi: `xpAwarded = phần còn lại`, `xpReason = "daily_cap_partial"`; hết hẳn: `0` + `daily_cap`.

## Lối cày và cách chặn

| # | Lối cày/gian lận | Cách chặn hoặc lý do chấp nhận |
|---|---|---|
| 1 | Mở nhiều lượt ôn để dò đáp án rồi trả lời đúng ở lượt sau | Đáp án luôn lộ sau khi nộp (bản chất ôn tập) nên dò là không tránh được. Chặn phần lợi: XP chỉ một lần cho mỗi (user, câu, ngày VN) và trần 100/ngày. Dò xong để mai ôn lại vẫn được XP, chấp nhận: người đó đã học xong câu. Có rate limit `practice`. |
| 2 | Nộp đồng thời cùng một câu (cộng XP hai lần) | Khóa `Users FOR UPDATE` tuần tự hóa; khóa duy nhất `(SessionId, QuestionId)` và unique một phần `(UserId, QuestionId, Day) WHERE XpAwarded > 0` là lưới cuối; unique-violation thành 409, rollback cả bộ đếm. |
| 3 | Nộp đồng thời nhiều câu khác nhau để vượt trần | Khóa Users tuần tự hóa; câu SQL `WHERE Xp < cap` + `LEAST` tự nó cũng không cho vượt trần ngay cả khi thiếu khóa (chứng minh bằng test độ nhạy). |
| 4 | Phòng tự tạo một mình, tự tạo câu hỏi dễ, tự chơi | Phòng phải có `MatchReward` `Awarded` (cần >= 2 tài khoản không phải chủ phòng đã trả lời, `RewardCalculator.cs:25,29-33`). Tạo 2-3 tài khoản phụ vẫn qua được, chấp nhận vì trần 100 XP/ngày/tài khoản, và cách đó đã có sẵn với XP trận. |
| 5 | Đọc đáp án trực tiếp từ payload | DTO câu hỏi không có thuộc tính `CorrectIndex` (kiểu riêng, như `QuestionStartedDto`); test kiểm payload thô. |
| 6 | Lượt của người khác (đoán `sessionId`) | Mọi truy vấn ràng buộc `UserId`; không thuộc user thì 404 (không lộ tồn tại). |
| 7 | Khách (token phiên) gọi API | `[Authorize]` theo JWT người dùng; token khách không hợp lệ ở scheme này → 401. |
| 8 | Đổi đồng hồ/múi giờ để lấy thêm ngày | `now` và ngày VN tính ở server (`VietnamDay`), client không gửi thời gian. |
| 9 | Spam endpoint | Rate limit policy `practice` theo user. |

## Kế hoạch test

**Unit (xUnit)**
- `PracticeRewardCalculator`: đúng/sai, đã nhận hôm nay, `xpPerCorrect`.
- Logic cắt trần mirror (hàm thuần tính `granted`, `reason`) để giải thích nhánh `LEAST`.
- Tên theo mẫu: `Grant_WrongAnswer_ReturnsZero`.

**Tích hợp Postgres thật (`DatabaseFixture`)**
- Chọn câu: chỉ `Approved` + `EndedAt != null`; phòng không `Finished`, phòng không có `Awarded` reward, phòng không liên quan: không ôn được.
- Bất biến: sau chuỗi trận + ôn, `TotalXp = SUM(MatchRewards.Xp) + SUM(PracticeAnswers.XpAwarded)` và `PracticeXpDaily.Xp = SUM(...)` theo ngày; `CoinBalance` và `CoinTransactions` không đổi.
- Trần: N request đồng thời (task song song, mỗi request một `DbContext`) → `Xp <= cap` và `TotalXp` khớp tổng.
- Hai request đồng thời cùng câu cùng lượt: một 200, một 409; XP một lần.
- Xóa phòng sau khi ôn: dòng XP còn (SET NULL), bất biến còn đúng.
- Qua nửa đêm VN: hai `now` hai bên mốc cho hai ngày khác nhau, bộ đếm riêng.
- Rollback: ép lỗi sau bước tăng bộ đếm (qua hook `BeforeXpWrite`) → bộ đếm không đổi.

**Mutation (test độ nhạy, theo `ShopPurchaseSensitivityTests.cs:63`, không thêm thư viện)**
Mỗi mutation là một cờ `internal` chỉ test mới đặt, và test tương ứng PHẢI đỏ khi bật cờ:
- M1 bỏ `WHERE Xp < cap` khỏi câu SQL → test đồng thời vượt trần.
- M2 thay `LEAST(@xp, cap - Xp)` bằng `@xp` → test cấu hình không chia hết (7/10) báo `Xp = 14 > 10`. Đây là mutation phủ đúng nhánh "phần còn lại".
- M3 thay `LEAST` bằng "từ chối cả câu" khi `cap - Xp < xp` → test cấu hình 7/10 báo sai (kỳ vọng 7 + 3).
- M4 `UseUserRowLock = false` + hook ép hai giao dịch cùng đọc `TotalXp` → test bất biến phát hiện mất cập nhật `TotalXp` (trần vẫn giữ nhờ câu SQL, và test khẳng định cả hai điều này).
- M5 bỏ kiểm tra "đã nhận XP hôm nay cho câu này" → test cày lại cùng câu đỏ.
- Mỗi mutation đi kèm một test "bản không bị mutate" xanh để chứng minh test không đỏ vì lý do khác.

**HTTP qua `ApiFactory`**
- 401 không token và token khách; 404 phòng không đủ điều kiện và lượt của người khác.
- Payload thô của `GET .../sessions/{id}` và danh sách không chứa `correctIndex`; response nộp có.
- 409 `already_answered`, 409 `not_current_question`, 400 `selectedIndex` ngoài 0..3.
- 429 từ policy `practice` (đứng sau `UseAuthorization`).
- Luồng đầy đủ tạo lượt → nộp hết → `summary` đúng, `remainingToday` đúng.

**Frontend (Vitest + jsdom)**
- `PracticeRooms`: tải / lỗi (retry) / rỗng / có dữ liệu.
- `PracticeQuestion`: nút disabled khi đang gửi, hiện đúng/sai và đáp án đúng sau nộp, 409 xử lý không vỡ.
- `PracticeSummary`: hiện "+X XP, còn Y hôm nay". Test logic thuần (định dạng XP còn lại).

## Migration và triển khai
- Migration `AddPracticeMode` tạo 3 bảng; chạy thử ở dev (Docker) trước. Production chạy thủ công có chủ đích (CLAUDE.md), kèm script SQL idempotent như đợt 004.
- Cấu hình `Practice__XpPerCorrect`, `Practice__DailyXpCap` thêm vào `.env.example`.
- Sửa `CLAUDE.md:60` và hai helper test bất biến cùng PR với migration.

## Rủi ro kỹ thuật cần để mắt
- `granted` = `Xp` trả về trừ `Xp` đọc trước: chính xác vì khóa Users đã tuần tự hóa; nếu sau này bỏ khóa thì số báo cáo `granted` có thể sai dù trần vẫn đúng (M4 nêu rõ).
- `QuestionIds uuid[]` không có FK: câu bị xóa (vì xóa phòng) làm câu "biến mất" khỏi lượt đang dở; xử lý: bỏ qua câu không còn, hoặc lượt coi như hết.
- Bảng xếp hạng XP nay gồm XP ôn tập (tối đa 100/ngày/người).

## Điểm cần chốt

1. **Bất biến XP và nơi lưu**: đề xuất BẢNG MỚI (`PracticeAnswers.XpAwarded`) với bất biến đổi thành `TotalXp = SUM(MatchRewards.Xp) + SUM(PracticeAnswers.XpAwarded)`. Giá: sửa `CLAUDE.md:60`, hai helper test, thêm test bất biến mới. Phương án B (mở rộng `MatchReward`) giữ nguyên chữ bất biến nhưng nới ràng buộc và dễ lọt dòng ôn vào lịch sử trận (xem research R8). Cần bạn duyệt A hay B.
2. **Con số XP**: `5` XP/câu đúng, trần `100`/ngày (research R5). Cần xác nhận hoặc đổi.
3. **Phần còn lại < XP câu**: đề xuất `LEAST`. Cần xác nhận (chỉ khác cấu hình không chia hết).
4. **XP một lần cho mỗi (user, câu, ngày)**: đề xuất giữ để chặn cày lại cùng câu trong ngày. Hệ quả: ôn lại cùng phòng lần hai trong ngày không có XP. Có chấp nhận không?
5. **Điều kiện phòng**: bắt buộc phòng có `MatchReward` `Awarded` (chặn phòng một mình). Hệ quả: phòng Finished nhưng không ai được thưởng thì không ôn được, dù user từng chơi. Có chấp nhận?
6. **`VietnamDay` đang ở `Domain.AiQuota`**: dùng lại nguyên chỗ cho gọn diff, hay chuyển sang namespace chung (ảnh hưởng `AiQuotaService`, `VietnamDayTests`)? Đề xuất giữ nguyên chỗ, không chuyển ở đợt này.
7. **Lượt ôn dở dang**: hiện không hết hạn và không giới hạn số lượt mở. Có cần giới hạn số lượt đang mở/user hoặc tự đóng sau N giờ không? (Chưa cần để chặn cày vì đã có #4 và trần; cần nếu lo bảng phình.)
8. **Câu bị xóa giữa lượt** (phòng bị xóa): bỏ qua câu hay coi lượt kết thúc? Đề xuất: lượt kết thúc, tổng kết theo số câu đã nộp.
9. **Cấu hình có nên bắt buộc trong `ProductionConfigValidator`?** Đề xuất: không, có mặc định 5/100.
10. **XP ôn tập tính vào bảng xếp hạng XP và cấp** (không tách riêng). Đề xuất chấp nhận nhờ trần; nếu không muốn thì phải thêm cột/bảng tách, đi ngược quyết định "TotalXp là tổng".
