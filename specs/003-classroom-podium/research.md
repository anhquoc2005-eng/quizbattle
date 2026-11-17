# Research: Màn chơi kiểu lớp học + bục vinh danh

Không còn NEEDS CLARIFICATION trong Technical Context; các mục dưới chốt cách làm cho những
điểm có nhiều phương án.

## R1 — Lưới bàn học co giãn 1→50 người

- **Decision**: CSS Grid `repeat(auto-fill, minmax(var(--desk-min), 1fr))`, biến `--desk-min`
  thu nhỏ theo bậc số người (ví dụ ≤12 người desk to, ≤30 trung bình, ≤50 nhỏ) bằng class trên
  container; không đo DOM, không JS layout.
- **Rationale**: Thuần CSS, không thư viện, không reflow thủ công; 50 ô ở 1366px vừa màn không
  cuộn ngang (SC-004). Bậc theo số người giữ chữ đọc được thay vì co vô cấp.
- **Alternatives considered**: (a) JS đo container + tính cột — thêm code, thêm resize listener,
  không cần; (b) virtualize danh sách — vô nghĩa với tối đa 50 phần tử và phá cảm giác "cả lớp
  cùng hiện".

## R2 — Hiệu năng 50 SVG DiceBear

- **Decision**: Dùng nguyên component `Avatar` hiện có (đi qua cache Map trong
  `avatarRenderer.ts`, mỗi chữ ký avatar chỉ dựng SVG/data-URI một lần); bọc mỗi desk trong
  `React.memo` với props phẳng (player + trạng thái) để sự kiện giơ tay của 1 người không
  re-render 49 desk còn lại.
- **Rationale**: Cache đã tồn tại — không viết lớp cache mới; `React.memo` là công cụ chuẩn của
  React, đủ cho 50 phần tử.
- **Alternatives considered**: (a) tự cache thêm ở Classroom — trùng với cache sẵn có;
  (b) canvas/sprite — phá cách vẽ SVG đã chốt trong CLAUDE.md.

## R3 — AnswerProgress đến sau QuestionEnded

- **Decision**: Reducer bỏ qua action `ANSWER_PROGRESS` khi `state.currentQuestion?.ended` là
  true (hoặc không có câu hiện tại). Trạng thái ✓/✗ chỉ bị xóa bởi `QUESTION_STARTED` /
  `ROOM_STATE` / `GAME_ENDED`. Khi KHÔNG bị bỏ qua, `ANSWER_PROGRESS` gộp hợp vào
  `answeredPlayerIds` (R7), không ghi đè.
- **Rationale**: Một chỗ chặn duy nhất tại nguồn state; UI không cần biết thứ tự mạng. Khớp hành
  vi sẵn có: `QUESTION_ENDED` đã tự chốt answerProgress cuối (gameReducer.ts:156).
- **Alternatives considered**: so timestamp sự kiện — phức tạp hơn, payload AnswerProgress không
  mang mốc thời gian và không cần thêm.

## R4 — Chủ phòng kết nối lại giữa câu

- **Decision**: `RoomStateDto` thêm `AnsweredPlayerIds` (null với người chơi; chỉ điền cho host
  khi phòng InProgress và câu hiện tại chưa ended). Reducer nạp vào cùng chỗ với
  `ANSWER_PROGRESS` (nhưng `ROOM_STATE` thay thế hẳn, vì là nguồn đầy đủ khi vào lại).
  Reconnect trong 6 giây hiển thị kết quả: không khôi phục ✓/✗ — **đã chốt (cố vấn): chấp nhận**.
- **Rationale**: Đúng quyết định 6c; ở đường dựng RoomState dữ liệu lấy từ `snapshot.Answers` đã
  nạp sẵn trong `GameSnapshot` (GameService đã dùng nó cho `hasAnswered` từng người — không thêm
  truy vấn). Lưu ý đường `SubmitAnswer` khác: xem R7.
- **Alternatives considered**: đưa `CorrectPlayerIds` vào RoomState khi câu ended — mở rộng
  ngoài quyết định 6, cố vấn đã chọn không làm.

## R5 — Lộ dần bục + pháo giấy thuần CSS

- **Decision**: Mỗi bậc là phần tử có `animation` (translateY + opacity) với `animation-delay`
  cố định theo thứ tự 3→2→1; pháo giấy là các mảnh `span` tuyệt đối với keyframes rơi/xoay,
  màu lấy từ token hệ thiết kế; toàn bộ tắt trong `@media (prefers-reduced-motion: reduce)`
  (hiện ngay, đủ nội dung).
- **Rationale**: Không thư viện (ràng buộc cứng); animation-delay thuần CSS không cần state máy
  đếm bước; reduced-motion xử lý một chỗ trong CSS.
- **Alternatives considered**: (a) canvas-confetti — thư viện mới, bị cấm; (b) điều khiển từng
  bước bằng setTimeout/state React — nhiều code hơn CSS delay mà không thêm giá trị, chỉ cần
  nếu muốn dừng/tua (không yêu cầu).

## R6 — Vị trí dữ liệu mới trong payload

- **Decision**: Giữ tên và kiểu: `AnswerProgressDto.AnsweredPlayerIds: IReadOnlyList<Guid>`,
  `QuestionEndedDto.CorrectPlayerIds: IReadOnlyList<Guid>`, `RoomStateDto.AnsweredPlayerIds:
  IReadOnlyList<Guid>?` (đặt ở gốc RoomState, không lồng vào CurrentQuestionStateDto).
- **Rationale**: Đúng nguyên văn quyết định 6; đặt ở gốc RoomState song song với
  `HasAnsweredCurrentQuestion` (cờ cá nhân) — cùng một kiểu "trạng thái câu hiện tại theo góc
  nhìn người nhận", dễ null cho người chơi.
- **Alternatives considered**: lồng vào `CurrentQuestionStateDto` — DTO đó dùng chung cho mọi
  người nhận, muốn null riêng cho người chơi sẽ phải tách hai bản dựng, phức tạp hơn.

## R7 — Danh sách đã trả lời: nguồn dữ liệu ở SubmitAnswer và gộp hợp ở client

- **Decision**:
  - Backend: `SubmitAnswerSnapshot` (đường `GameRepository.SubmitAnswerAsync`) không có
    `Answers`, chỉ có `AnsweredCountForCurrentQuestion` từ `CountAsync` (GameRepository.cs:139).
    Thay bằng MỘT truy vấn lấy danh sách `PlayerId` của câu hiện tại
    (`Select(a => a.PlayerId).ToListAsync`), số đếm suy ra từ `.Count` — vẫn một truy vấn như cũ.
    `AnswerProgressDto.AnsweredPlayerIds` = danh sách đó + người vừa nộp.
  - Client: reducer `ANSWER_PROGRESS` GỘP HỢP với `answeredPlayerIds` hiện có của cùng câu
    (chỉ thêm, không ghi đè); `QUESTION_STARTED` xóa sạch; `ROOM_STATE` thay thế.
- **Rationale**: Đường SubmitAnswer chỉ khóa phòng `FOR SHARE` (GameRepository.cs:129-134) để nhiều
  người nộp song song. Hai người A và B nộp đồng thời: mỗi giao dịch không thấy answer chưa commit
  của người kia, nên payload của A là `{A}` và của B là `{B}`. Nếu client ghi đè, tay của người đến
  trước bị hạ xuống. Gộp hợp biến hai payload thành `{A,B}` và cũng làm gói đến trễ (`{A}` sau
  `{A,B}`) vô hại. Danh sách chỉ ĐÚNG khi đã gộp, vì vậy contract không hứa
  `answeredPlayerIds.length == answeredCount` ở phía client.
- **Alternatives considered**: (a) khóa phòng độc quyền khi nộp để mọi người thấy nhau — phá thiết
  kế chủ ý "không xếp hàng chờ nhau"; (b) thêm số thứ tự/phiên bản vào payload để client chọn gói
  mới nhất — vẫn thiếu người ở cả hai gói, không giải quyết gốc; (c) gửi lại toàn bộ danh sách
  bằng truy vấn riêng sau commit — thêm một truy vấn mỗi lần nộp.
- **Giới hạn đã biết (có từ trước, ngoài phạm vi)**: trong cùng race đó `AnsweredCount` (đếm
  trước + 1) của hai payload có thể thấp hơn thực tế; chữ "Đã trả lời x/y" hiện có dựa vào nó. Nếu
  muốn, hiển thị `max(answeredCount, answeredPlayerIds.length)` — không đưa vào tính năng này.
