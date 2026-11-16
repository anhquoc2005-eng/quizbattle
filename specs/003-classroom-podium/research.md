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
  `ROOM_STATE` / `GAME_ENDED`.
- **Rationale**: Một chỗ chặn duy nhất tại nguồn state; UI không cần biết thứ tự mạng. Khớp hành
  vi sẵn có: `QUESTION_ENDED` đã tự chốt answerProgress cuối (gameReducer.ts:156).
- **Alternatives considered**: so timestamp sự kiện — phức tạp hơn, payload AnswerProgress không
  mang mốc thời gian và không cần thêm.

## R4 — Chủ phòng kết nối lại giữa câu

- **Decision**: `RoomStateDto` thêm `AnsweredPlayerIds` (null với người chơi; chỉ điền cho host
  khi phòng InProgress và câu hiện tại chưa ended). Reducer nạp vào cùng chỗ với
  `ANSWER_PROGRESS`. Reconnect trong 6 giây hiển thị kết quả: không khôi phục ✓/✗ — ghi nhận là
  hành vi chấp nhận (Assumption trong spec, chờ cố vấn xác nhận nếu muốn hơn).
- **Rationale**: Đúng quyết định 6c; dữ liệu lấy từ `snapshot.Answers` đã nạp sẵn trong
  transaction (GameService đã dùng nó cho `hasAnswered` từng người — không thêm truy vấn).
- **Alternatives considered**: đưa `CorrectPlayerIds` vào RoomState khi câu ended — mở rộng
  ngoài quyết định 6, để cố vấn quyết.

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
