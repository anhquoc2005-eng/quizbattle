# Feature Specification: Chế độ ôn tập

**Branch**: `feat/practice-mode` | **Created**: 2026-11-21 | **Status**: Đã duyệt (đợt 2, chưa có code)

## Bối cảnh

Người chơi đăng nhập muốn ôn lại câu hỏi của các trận đã chơi, một mình, không áp lực thời gian. Server chấm và thưởng
một ít XP (không Xu) để khuyến khích ôn, có trần mỗi ngày để không thể cày XP thay cho việc thi đấu.

## Quyết định sản phẩm đã chốt *(không tự thay đổi)*

1. Chỉ người dùng đăng nhập. Ôn được câu hỏi của phòng `Finished` mà user từng là `Player` (qua `UserId`) hoặc là chủ phòng.
2. Chơi một mình qua REST, không SignalR, không đếm giờ. Server chấm: câu hỏi gửi xuống KHÔNG có `CorrectIndex`;
   đáp án đúng chỉ trả sau khi nộp. Mỗi `(lượt, câu)` được chấm đúng 1 lần; nộp lại chỉ phát lại kết quả đã lưu (idempotent).
3. Chỉ thưởng XP. Không Xu, không `CoinTransaction`.
4. `5` XP mỗi câu đúng, trần `100` XP/ngày VN (dùng lại `VietnamDay`). Lý do ở `research.md` R5.
5. Trần an toàn đồng thời; tăng bộ đếm ngày, ghi dòng XP và cộng `User.TotalXp` nằm trong MỘT transaction.
6. Frontend: trang liệt kê phòng ôn được, màn từng câu (đúng/sai ngay), màn tổng kết "+X XP, còn Y hôm nay".

### Các điểm đã chốt ở vòng duyệt (ký hiệu Ch1–Ch10, dùng trong `tasks.md`)

| # | Chốt |
|---|---|
| Ch1 | Bảng mới `PracticeAnswers.XpAwarded`; bất biến `TotalXp = SUM(MatchRewards.Xp) + SUM(PracticeAnswers.XpAwarded)` |
| Ch2 | `XpPerCorrect = 5`, `DailyXpCap = 100` |
| Ch3 | Phần còn lại < XP của câu: cộng phần còn lại (`LEAST`) |
| Ch4 | XP một lần cho mỗi (user, câu, ngày VN) |
| Ch5 | Phòng phải có `MatchReward` `Awarded` mới ôn được |
| Ch6 | `VietnamDay` giữ nguyên ở `Domain.AiQuota` |
| Ch7 | Mỗi (user, phòng) chỉ một lượt đang mở; `POST /sessions` khi đã có lượt dở trả lại lượt đó (200, cùng `sessionId`) |
| Ch8 | Câu bị xóa giữa lượt: lượt kết thúc, tổng kết theo số câu đã nộp |
| Ch9 | Cấu hình ôn tập không vào `ProductionConfigValidator`, nhưng kiểm `XpPerCorrect >= 0`, `DailyXpCap >= 0` lúc khởi động (`ValidateOnStart`) |
| Ch10 | XP ôn tập tính vào `TotalXp`, cấp và bảng xếp hạng; ghi vào "Hạn chế đã biết" |

## Đối chiếu với code thật

Xem `research.md` (mỗi quyết định kèm file/dòng). Tóm tắt:

- XP trận = `Score / 10` (`RewardCalculator.cs:44`); `TotalXp` chỉ tăng ở `RewardSettlement.cs:64`.
- `QuestionStartedDto` không có thuộc tính `CorrectIndex`; màn ôn tập dùng DTO riêng cùng nguyên tắc.
- Câu "đã chơi" = `Approved` và `EndedAt != null`.

## User Scenarios & Testing

### User Story 1 — Xem danh sách phòng ôn được (P1)
Người dùng mở `/practice` thấy các phòng đã kết thúc họ từng chơi hoặc làm chủ, kèm số câu và "hôm nay đã nhận X/100 XP".
**Kiểm thử**: user A chơi phòng 1, làm chủ phòng 2, không liên quan phòng 3 → thấy 1 và 2. Phòng chưa `Finished` hoặc không có `Awarded` không hiện. Đủ 3 trạng thái tải/lỗi/rỗng.

### User Story 2 — Ôn từng câu, biết đúng/sai ngay, F5 không mất đáp án (P1)
Bắt đầu hoặc tiếp tục lượt ôn, trả lời lần lượt; sau mỗi lần nộp thấy đúng/sai, đáp án đúng, XP. Tải lại trang sau khi nộp vẫn thấy kết quả câu vừa nộp (`lastResult`).
**Kiểm thử**: payload câu hỏi không có `correctIndex`; nộp lại cùng câu (kể cả `selectedIndex` khác) → 200 với kết quả lần đầu, XP không cộng thêm; nộp câu chưa chấm mà không phải câu hiện tại → 409 `not_current_question`.

### User Story 3 — Thưởng XP có trần an toàn (P1)
Câu đúng cộng XP vào `TotalXp` kèm một dòng ghi nhận; chạm trần ngày thì không cộng thêm.
**Kiểm thử**: nhiều request đồng thời không vượt trần; XP một lần cho mỗi (user, câu, ngày VN); không có `CoinTransaction`; bất biến XP đúng.

### User Story 4 — Một lượt dở cho mỗi phòng (P2)
Bấm "Ôn" khi đã có lượt dở của phòng đó thì tiếp tục lượt ấy, không tạo lượt mới.
**Kiểm thử**: hai POST đồng thời → cùng một `sessionId`.

### User Story 5 — Màn tổng kết (P2)
Xong lượt: "+X XP trong lượt này, còn Y XP hôm nay", số câu đúng.

### Edge Cases
- Phòng bị xóa sau khi có lượt ôn: dòng XP đã ghi vẫn còn (FK SET NULL) nên bất biến không vỡ.
- Câu bị xóa giữa lượt (xóa phòng): lượt kết thúc, tổng kết theo số câu đã nộp (Ch8).
- Phần trần còn lại < XP của câu: cộng phần còn lại (Ch3).
- Qua 00:00 giờ VN giữa lượt: câu nộp sau mốc dùng ngày mới (ngày tính theo thời điểm nộp ở server).
- `XpPerCorrect = 0` hoặc `DailyXpCap = 0`: hợp lệ, ôn tập không thưởng XP.
- User bị xóa: cascade xóa lượt ôn và XP đi cùng tài khoản.

## Requirements

### Functional Requirements
- **FR-001**: Chỉ JWT người dùng; khách (token phiên) không gọi được.
- **FR-002**: Phòng ôn được = `Finished` AND (chủ phòng OR có `Player.UserId = me`) AND có ít nhất một `MatchReward` `Awarded` của phòng AND có câu đã chơi.
- **FR-003**: Lượt ôn chứa các câu đã chơi của phòng theo thứ tự ngẫu nhiên do server chọn lúc tạo và lưu lại.
- **FR-004**: DTO câu hỏi không có `CorrectIndex`. Đáp án đúng chỉ có trong kết quả đã nộp (response nộp, `lastResult`).
- **FR-005**: Nộp đáp án idempotent: mỗi `(lượt, câu)` chấm đúng 1 lần; nộp lại trả 200 với kết quả đã lưu (`isCorrect`, `correctIndex`, `xpAwarded`, `xpReason`, `completed`), không chấm lại, không cộng XP, bất kể `selectedIndex` mới. Không có 409 `already_answered`.
- **FR-006**: Câu đúng được `XpPerCorrect` XP nếu chưa nhận XP cho `(user, câu)` trong ngày VN đó và ngày đó còn quota (cộng phần còn lại nếu quota < `XpPerCorrect`); ngoài ra 0 XP kèm lý do.
- **FR-007**: Cộng `Users.TotalXp`, ghi dòng XP, tăng bộ đếm ngày: một transaction, khóa `Users` `FOR UPDATE`.
- **FR-008**: Không ghi `CoinTransaction`, không đổi `CoinBalance`.
- **FR-009**: Rate limit theo user cho các endpoint ôn tập.
- **FR-010**: `Practice:XpPerCorrect`, `Practice:DailyXpCap` đọc từ configuration (mặc định 5/100), kiểm `>= 0` lúc khởi động.
- **FR-011**: Mỗi `(user, phòng)` tối đa một lượt đang mở (DB bảo đảm); `POST /sessions` khi đã có lượt dở trả 200 + cùng `sessionId`.
- **FR-012**: `GET /sessions/{id}` trả `lastResult` (kết quả câu nộp gần nhất) để tải lại trang vẫn thấy đáp án.
- **FR-013**: Lượt kết thúc khi hết câu còn tồn tại (kể cả khi câu bị xóa giữa lượt).
- **FR-014**: Lý do XP lưu ở `PracticeAnswers.XpReason`, DB ràng buộc giá trị và quan hệ với `IsCorrect`/`XpAwarded`.
- **FR-015**: Bất biến `TotalXp = SUM(MatchRewards.Xp) + SUM(PracticeAnswers.XpAwarded)` và `PracticeXpDaily.Xp = SUM(PracticeAnswers.XpAwarded)` theo `(user, ngày)` luôn đúng.

### Key Entities
`PracticeSession`, `PracticeAnswer` (mang `XpAwarded`, `XpReason`), `PracticeXpDaily` — xem `data-model.md`.

## Success Criteria
- **SC-001**: Payload thô mọi endpoint không chứa `correctIndex` của câu chưa nộp.
- **SC-002**: N request đồng thời: tổng XP ngày ≤ trần, `TotalXp` khớp bất biến.
- **SC-003**: Hai request đồng thời cùng câu: cả hai 200, cùng kết quả, XP cộng 1 lần.
- **SC-004**: Hai `POST /sessions` đồng thời cùng phòng: cùng `sessionId`.
- **SC-005**: Mỗi mutation M1–M7 làm test tương ứng đỏ (xem `plan.md`).

## Hạn chế đã biết (Ch10)
- Người chỉ ôn (không chơi trận nào) có thể thêm tối đa `100 XP x 30 ngày = ~3000 XP/tháng` vào `TotalXp`, cấp và bảng xếp hạng XP. Chấp nhận vì trần ngày; muốn tách riêng phải bỏ nguyên tắc "TotalXp là tổng".
- Luật "XP một lần cho mỗi (user, câu, ngày)" KHÔNG chặn việc học thuộc đáp án: chủ phòng biết sẵn đáp án các câu mình viết, và người chơi thấy đáp án sau mỗi câu. Kiểm soát thật là trần ngày, không phải luật này.
- Tạo vài tài khoản phụ để dựng phòng hợp lệ (cần >= 2 tài khoản không phải chủ phòng trả lời) vẫn qua được; chặn bằng trần 100 XP/ngày/tài khoản, và cách này đã tồn tại với XP trận.

## Ngoài phạm vi
Xu, bảng xếp hạng ôn tập, đồng hồ đếm giờ, Admin, ôn tập cho khách, xáo trộn đáp án theo lượt, chia sẻ lượt ôn, hết hạn lượt dở.
