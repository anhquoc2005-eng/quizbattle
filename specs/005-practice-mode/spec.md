# Feature Specification: Chế độ ôn tập

**Branch**: `feat/practice-mode` | **Created**: 2026-11-21 | **Status**: Draft (chờ duyệt, CHƯA có code)

## Bối cảnh

Người chơi đăng nhập muốn ôn lại câu hỏi của các trận đã chơi, một mình, không áp lực thời gian. Server chấm và thưởng
một ít XP (không Xu) để khuyến khích ôn, có trần mỗi ngày để không thể cày XP thay cho việc thi đấu.

## Quyết định sản phẩm đã chốt *(không tự thay đổi)*

1. Chỉ người dùng đăng nhập. Ôn được câu hỏi của phòng `Finished` mà user từng là `Player` (qua `UserId`) hoặc là chủ phòng.
2. Chơi một mình qua REST, không SignalR, không đếm giờ. Server chấm: câu hỏi gửi xuống KHÔNG có `CorrectIndex`;
   đáp án đúng chỉ trả sau khi nộp. Mỗi câu chấm tối đa 1 lần trong một lượt ôn.
3. Chỉ thưởng XP. Không Xu, không `CoinTransaction`.
4. Ít XP mỗi câu đúng + trần XP/ngày theo ngày Việt Nam (dùng lại `VietnamDay`). Con số: xem `research.md` mục R5.
5. Trần phải an toàn đồng thời; tăng bộ đếm, ghi dòng XP và cộng `User.TotalXp` nằm trong MỘT transaction.
6. Frontend: trang liệt kê phòng ôn được, màn từng câu (đúng/sai ngay), màn tổng kết "+X XP, còn Y hôm nay".

## Đối chiếu với code thật

Xem `research.md` (mỗi quyết định kèm file/dòng). Tóm tắt các điểm ảnh hưởng thiết kế:

- XP trận = `Score / 10` (`RewardCalculator.cs:44`); `TotalXp` chỉ tăng ở `RewardSettlement.cs:64` và bất biến `TotalXp = SUM(MatchRewards.Xp)`
  đang được test ở 2 chỗ. XP ôn tập làm bất biến này đổi dạng (mục "Điểm cần chốt" #1 của plan).
- `QuestionStartedDto` không có thuộc tính `CorrectIndex` (không chỉ để null). Màn ôn tập dùng DTO riêng theo cùng nguyên tắc.
- Câu "đã chơi" = `Approved` và `EndedAt != null` (đáp án đã được công bố cho cả phòng lúc `QuestionEnded`).

## User Scenarios & Testing

### User Story 1 — Xem danh sách phòng ôn được (P1)
Người dùng mở `/practice` thấy các phòng đã kết thúc họ từng chơi hoặc làm chủ, kèm số câu và "hôm nay đã nhận X/100 XP".
**Kiểm thử**: user A chơi phòng 1, làm chủ phòng 2, không liên quan phòng 3 → thấy đúng 1 và 2. Phòng chưa `Finished` không hiện.
Có đủ 3 trạng thái tải/lỗi/rỗng.

### User Story 2 — Ôn từng câu, biết đúng/sai ngay (P1)
Bắt đầu lượt ôn, trả lời lần lượt; sau mỗi lần nộp thấy đúng/sai, đáp án đúng và XP nhận được.
**Kiểm thử**: payload câu hỏi không chứa `correctIndex`; nộp lần 2 cùng câu trong lượt → 409; nộp câu không phải câu hiện tại → 409.

### User Story 3 — Thưởng XP có trần an toàn (P1)
Câu đúng cộng XP vào `TotalXp`, kèm một dòng ghi nhận; chạm trần ngày thì không cộng thêm.
**Kiểm thử**: 2 request đồng thời không vượt trần; XP chỉ tính 1 lần cho mỗi (user, câu, ngày VN); không có `CoinTransaction`.

### User Story 4 — Màn tổng kết (P2)
Xong lượt: "+X XP trong lượt này, còn Y XP hôm nay", số câu đúng.

### Edge Cases
- Phòng bị xóa sau khi có lượt ôn: dòng XP đã ghi vẫn còn (khóa ngoại SET NULL, không cascade) để bất biến không vỡ.
- Phần trần còn lại nhỏ hơn XP của câu: cộng phần còn lại (`LEAST`), xem plan mục "Xử lý phần còn lại".
- Qua 00:00 giờ VN giữa lượt: câu nộp sau mốc dùng ngày mới (ngày tính theo thời điểm nộp, server).
- User bị xóa: cascade xóa lượt ôn và XP đi cùng tài khoản.

## Requirements

### Functional Requirements
- **FR-001**: Chỉ JWT người dùng; khách (token phiên) không gọi được.
- **FR-002**: Danh sách phòng ôn được = `Finished` AND (chủ phòng OR có `Player.UserId = me`) AND có ít nhất một `MatchReward` `Awarded` của phòng (trận hợp lệ; chặn phòng tự tạo một mình) AND có câu đã chơi.
- **FR-003**: Lượt ôn chứa các câu đã chơi của phòng theo thứ tự ngẫu nhiên do server chọn lúc tạo và lưu lại.
- **FR-004**: DTO câu hỏi không có `CorrectIndex`. Đáp án đúng chỉ có trong response nộp (và khi đọc lại câu đã nộp).
- **FR-005**: Mỗi `(session, question)` chấm tối đa 1 lần, chặn ở DB bằng khóa chính.
- **FR-006**: Câu đúng được `XpPerCorrect` XP nếu (a) chưa nhận XP cho `(user, question)` trong ngày VN đó và (b) ngày đó còn quota; ngoài ra 0 XP và response nêu lý do.
- **FR-007**: Cộng `Users.TotalXp`, ghi dòng XP, tăng bộ đếm ngày: một transaction, khóa `Users` `FOR UPDATE`.
- **FR-008**: Không ghi `CoinTransaction`, không đổi `CoinBalance`.
- **FR-009**: Rate limit theo user cho các endpoint ôn tập.
- **FR-010**: Số liệu cấu hình (`Practice:XpPerCorrect`, `Practice:DailyXpCap`) đọc từ configuration, không hardcode.

### Key Entities
`PracticeSession`, `PracticeAnswer` (mang `XpAwarded`), `PracticeXpDaily` — xem `data-model.md`.

## Success Criteria
- Đọc payload mọi endpoint ôn tập không thấy đáp án đúng của câu chưa nộp.
- Test tích hợp đồng thời: N request song song, tổng XP ngày ≤ trần, `TotalXp` khớp tổng bản ghi.
- Mutation (tắt `WHERE`, bỏ `LEAST`, bỏ khóa Users) làm test tương ứng đỏ.

## Rủi ro đã chấp nhận
Xem `plan.md` mục "Lối cày và cách chặn".

## Điểm cần chốt
Xem cuối `plan.md`.

## Ngoài phạm vi
Xu, bảng xếp hạng ôn tập, đồng hồ đếm giờ, Admin, ôn tập cho khách, xáo trộn đáp án theo lượt, chia sẻ lượt ôn.
