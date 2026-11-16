# Implementation Plan: Màn chơi kiểu lớp học + bục vinh danh

**Branch**: `feat/classroom-podium` | **Date**: 2026-11-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/003-classroom-podium/spec.md`

## Summary

Biến màn chủ phòng (RoomLivePage) thành cảnh lớp học: lưới bàn học hiển thị nhân vật DiceBear
của tối đa 50 người chơi, giơ tay khi đã trả lời, huy hiệu ✓/✗ khi câu kết thúc; cuối trận bục
vinh danh top 3 lộ dần 3→2→1 kèm pháo giấy CSS trên cả màn chủ phòng lẫn màn người chơi.
Backend chỉ mở rộng payload 3 chỗ (AnsweredPlayerIds cho host, CorrectPlayerIds cả phòng,
RoomState host khi reconnect) — không migration, không thư viện mới, không sự kiện SignalR mới.

## Technical Context

**Language/Version**: C# / .NET 8 (backend), TypeScript 5 + React 18 (frontend)

**Primary Dependencies**: ASP.NET Core 8, EF Core, SignalR; Vite, Tailwind CSS, @microsoft/signalr, @dicebear/core + @dicebear/styles (đã ghim, chỉ nạp qua `avatarRenderer.ts`)

**Storage**: PostgreSQL — KHÔNG thay đổi schema (dữ liệu cần có sẵn trong bảng `Answers`)

**Testing**: xUnit (backend), Vitest + jsdom/@testing-library/react (frontend)

**Target Platform**: Web; màn chủ phòng tối ưu cho laptop ≥1366px / máy chiếu, màn người chơi là điện thoại

**Project Type**: Web app (backend + frontend)

**Performance Goals**: 50 nhân vật SVG hiển thị cùng lúc không khựng khi đổi trạng thái (giơ tay/✓✗); không re-render 50 SVG mỗi sự kiện

**Constraints**: Không migration, không thư viện mới, không đổi tên sự kiện SignalR, không đổi thuộc tính DiceBear của nhân vật, tôn trọng `prefers-reduced-motion`, theo hệ thiết kế true-black-flat (accent chỉ cho dữ liệu, cấm gradient/bóng đổ/emoji)

**Scale/Scope**: 1 phòng ≤ 50 người chơi; 2 màn hình sửa (RoomLivePage, PlayFinished); 3 payload mở rộng

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Nguyên tắc | Đánh giá |
|---|---|
| 1. Server là nguồn sự thật | ĐẠT — giơ tay/✓✗/bục đều hiển thị từ dữ liệu server (`AnsweredPlayerIds`, `CorrectPlayerIds`, `rank`); client không tự suy đoán đúng/sai. |
| 2. Kiến trúc phân lớp | ĐẠT — DTO mở rộng ở Application, dữ liệu lấy qua snapshot (đường SubmitAnswer đổi truy vấn đếm thành truy vấn lấy `PlayerId` ở Repository); Hub/Broadcaster chỉ chuyển tiếp. Logic xếp bục phía client là hàm thuần có test. |
| 3. Bảo mật | ĐẠT — điểm mấu chốt: `AnsweredPlayerIds` chỉ đi nhóm `{code}:host` + RoomState của host; có test chống rò sang người chơi. Không secret mới. |
| 4. Quy trình tính năng | ĐẠT — nhánh `feat/classroom-podium`, 5 đợt nhỏ, mỗi đợt build + test xanh và push độc lập. |
| 5. Không thêm thư viện | ĐẠT — pháo giấy/hiệu ứng thuần CSS, không cài gì mới. |

**Re-check sau Phase 1**: không phát sinh vi phạm; Complexity Tracking bỏ trống.

## Project Structure

### Documentation (this feature)

```text
specs/003-classroom-podium/
├── spec.md
├── plan.md              # File này
├── research.md          # Phase 0
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1
├── contracts/
│   └── signalr-payloads.md
├── checklists/
│   └── requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
backend/
  QuizBattle.Application/Game/
    AnswerProgressDto.cs        # SỬA: + AnsweredPlayerIds
    QuestionEndedDto.cs         # SỬA: + CorrectPlayerIds
    RoomStateDto.cs             # SỬA: + AnsweredPlayerIds (chỉ host, null với người chơi)
    SubmitAnswerSnapshot.cs     # SỬA: AnsweredCountForCurrentQuestion (int) -> AnsweredPlayerIds (list, count = .Count)
    GameService.cs              # SỬA: điền 3 trường; BuildRoomState sắp Players theo JoinedAt rồi Id
  QuizBattle.Infrastructure/Persistence/
    GameRepository.cs           # SỬA: SubmitAnswerAsync (dòng ~139) CountAsync -> lấy list PlayerId (vẫn 1 truy vấn). KHÔNG đụng ORDER BY ở LoadSnapshotAsync
  QuizBattle.Tests/
    Game/GameServiceTests.cs    # SỬA: test rò rỉ / CorrectPlayerIds / RoomState reconnect / thứ tự ổn định
    TestHelpers/FakeGameRepository.cs # SỬA: dựng SubmitAnswerSnapshot theo list (dòng ~96)

frontend/src/
  types/game.ts                 # SỬA: khớp 3 DTO mở rộng
  realtime/gameReducer.ts       # SỬA: answeredPlayerIds (gộp hợp)/correctPlayerIds, chặn AnswerProgress trễ
  realtime/gameReducer.test.ts  # SỬA: test reducer cho trạng thái mới
  components/Classroom.tsx      # THÊM: lưới bàn học (desk = Avatar + tên + trạng thái)
  components/Classroom.test.tsx # THÊM: jsdom test
  components/Podium.tsx         # THÊM: bục vinh danh + pháo giấy CSS
  components/Podium.test.tsx    # THÊM: jsdom test
  components/podiumSteps.ts     # THÊM: hàm thuần xếp bục từ leaderboard (lọc rank<=3, sắp theo rank)
  components/podiumSteps.test.ts# THÊM: Vitest (cùng điểm khác rank, 1-2 người)
  pages/RoomLivePage/LiveLobby.tsx    # SỬA: dùng Classroom thay PlayerList
  pages/RoomLivePage/LiveQuestion.tsx # SỬA: thêm Classroom (giơ tay, ✓/✗)
  pages/RoomLivePage/LiveFinished.tsx # SỬA: thêm Podium
  pages/PlayPage/PlayFinished.tsx     # SỬA: thêm Podium
  index.css                     # SỬA: keyframes giơ tay/nhún/lộ dần/pháo giấy + reduced-motion
```

**Structure Decision**: Giữ nguyên cấu trúc web app hiện có (backend layered + frontend
pages/components/realtime). Component mới đặt ở `src/components/` vì Podium dùng chung cho hai
trang; logic xếp bục tách hàm thuần `podiumSteps.ts` để Vitest không cần DOM.

## Các đợt triển khai (mỗi đợt build + test xanh, push độc lập)

### Đợt 1 — Backend DTO + test

- Sửa: `AnswerProgressDto.cs`, `QuestionEndedDto.cs`, `RoomStateDto.cs`, `SubmitAnswerSnapshot.cs`,
  `GameRepository.cs`, `GameService.cs`, `FakeGameRepository.cs`, `GameServiceTests.cs`.
- Đường SubmitAnswer KHÔNG có `Answers` trong snapshot (chỉ `AnsweredCountForCurrentQuestion` từ
  `CountAsync`, GameRepository.cs:139): đổi thành MỘT truy vấn lấy `PlayerId` của câu hiện tại,
  count = `.Count`. `SubmitAnswerAsync` (GameService.cs:275): `AnsweredPlayerIds` = danh sách đó +
  người vừa trả lời (R7). Đường `GameSnapshot` (EndCurrentQuestion, BuildRoomState) đã có
  `snapshot.Answers`: `EndCurrentQuestion` (GameService.cs:279) dựng `CorrectPlayerIds` = answers
  của câu có `IsCorrect`; `BuildRoomState` (GameService.cs:353) host + InProgress + câu chưa ended
  → điền `AnsweredPlayerIds`, mọi trường hợp khác null.
- Thứ tự chỗ ngồi: `BuildRoomState` tự sắp `Players` theo `JoinedAt` rồi `Id` (một chỗ dựng
  RoomState duy nhất; test bằng snapshot lộn xộn, không cần Postgres). KHÔNG sửa ORDER BY trong
  `GameRepository.LoadSnapshotAsync` — các nơi khác dùng `snapshot.Players` giữ nguyên hành vi.
- Test bắt buộc (xUnit):
  - RoomState của NGƯỜI CHƠI không chứa `AnsweredPlayerIds` ở mọi trạng thái (chống rò).
  - `CorrectPlayerIds` đúng khi: có người không trả lời, trả lời sai, đã rời phòng (Player còn
    nguyên trong trận).
  - RoomState của chủ phòng kết nối lại giữa câu chứa đúng danh sách đã trả lời.
  - `RoomState.Players` sắp ổn định theo `JoinedAt` rồi `Id` với snapshot lộn xộn, kể cả khi điểm
    đổi.
- Test tích hợp Postgres thật (`Integration/SubmitAnswerSnapshotTests.cs`, task T010a): truy vấn
  thật của `GameRepository.SubmitAnswerAsync` chỉ trả người đã trả lời câu ĐANG CHẠY (không lẫn
  câu trước; chưa ai trả lời → rỗng) — vì các test ở trên chạy qua `FakeGameRepository`.
- Frontend chưa đụng (types sửa ở đợt dùng tới) → build + test hai phía vẫn xanh.

### Đợt 2 — Lưới lớp học (sảnh + trong câu, chưa có trạng thái)

- Thêm: `Classroom.tsx` (+ test jsdom). Sửa: `LiveLobby.tsx`, `LiveQuestion.tsx`, `index.css`.
- Lưới CSS grid tự co giãn theo số người (xem research.md R1); desk = `Avatar` (component sẵn
  có, đã đi qua cache avatarRenderer) + tên cắt bằng ellipsis.
- Mỗi desk bọc `React.memo` — 50 SVG chỉ render lại khi props desk đó đổi (R2).
- Lưới hiển thị đúng thứ tự `players` nhận được, KHÔNG sắp theo điểm/leaderboard (FR-022);
  `PLAYER_JOINED` nối cuối như reducer hiện tại; người rời (`online:false`) vẫn giữ bàn.
- Test jsdom: render đúng số desk, tên hiển thị, giữ nguyên thứ tự đầu vào (kể cả khi điểm đổi),
  người mới nối cuối, cập nhật khi players đổi.

### Đợt 3 — Giơ tay + huy hiệu ✓/✗

- Sửa: `types/game.ts`, `gameReducer.ts` (+ test), `Classroom.tsx` (+ test), `LiveQuestion.tsx`,
  `index.css`.
- Reducer: `ANSWER_PROGRESS` GỘP HỢP vào `answeredPlayerIds` (R7: hai người nộp đồng thời mỗi
  payload có thể thiếu người kia); bị BỎ QUA khi `currentQuestion.ended` (R3 — sự kiện đến trễ);
  `QUESTION_ENDED` lưu `correctPlayerIds`; `QUESTION_STARTED` xóa cả hai; `ROOM_STATE` thay thế
  `answeredPlayerIds` (host reconnect, R4).
- Desk nhận trạng thái: thường / giơ tay / ✓ / ✗ (lớp phủ CSS, không đổi thuộc tính DiceBear).
- Test reducer: {A} rồi {B} → {A,B}; {A,B} rồi {A} đến trễ → vẫn {A,B}; thứ tự sự kiện trễ sau
  QUESTION_ENDED, reset khi sang câu, nạp từ RoomState. Test jsdom: đúng desk giơ tay, ✓/✗ phủ
  đúng người (sai + không trả lời đều ✗).

### Đợt 4 — Bục vinh danh

- Thêm: `podiumSteps.ts` (+ Vitest), `Podium.tsx` (+ jsdom). Sửa: `LiveFinished.tsx`,
  `PlayFinished.tsx`, `index.css`.
- `podiumSteps(leaderboard)`: hàm thuần lọc `rank <= 3` và sắp theo `rank` (rank bảng xếp hạng
  luôn duy nhất → mỗi bậc một người); test cùng điểm nhưng rank khác → hai bậc khác nhau, 1 người,
  2 người, rỗng.
- Lộ dần 3→2→1 bằng CSS animation-delay; pháo giấy CSS thuần (R5).
- Nguồn dữ liệu: `finalLeaderboard ?? leaderboard` (đã có sẵn ở cả hai màn, kể cả vào lại sau
  khi trận kết thúc — FR-018).

### Đợt 5 — Hoàn thiện responsive + reduced-motion

- Sửa: `index.css`, `Classroom.tsx`, `Podium.tsx` (nếu cần).
- `@media (prefers-reduced-motion: reduce)`: tắt lộ dần/pháo giấy/nhún/giơ tay chuyển động —
  nội dung hiện ngay (FR-017, SC-006).
- Soát 1366px với 50 người (SC-004) và điện thoại cho Podium ở PlayFinished.
- Nếu máy có skill `design-taste-frontend`: chạy lint.mjs + inspect.mjs theo CLAUDE.md, sửa hết FAIL.

## Rủi ro và cách xử lý

| Rủi ro | Xử lý |
|---|---|
| Vẽ 50 SVG DiceBear chậm / re-render hàng loạt | Cache data-URI đã có sẵn trong `avatarRenderer.ts` (Map + MAX_CACHED) nên mỗi avatar chỉ dựng SVG một lần; thêm `React.memo` cho từng desk để đổi trạng thái 1 người không render lại 49 người. Đo bằng mắt ở quickstart với 50 người giả lập. |
| `AnswerProgress` đến sau `QuestionEnded` | Chặn ở reducer: bỏ qua `ANSWER_PROGRESS` khi câu hiện tại `ended` (có test reducer riêng). |
| Hai người nộp đồng thời (phòng chỉ khóa `FOR SHARE`): mỗi `AnswerProgress` thiếu người kia | Reducer gộp HỢP `answeredPlayerIds` trong cùng câu, không ghi đè (R7); test {A}+{B}→{A,B} và gói trễ {A} sau {A,B}. |
| Chủ phòng kết nối lại giữa câu | `RoomState` host mang `AnsweredPlayerIds` (đợt 1) → reducer nạp lại tay giơ; `Players` sắp theo `JoinedAt` rồi `Id` nên bàn không đổi chỗ. Reconnect trong 6 giây kết quả: không khôi phục ✓/✗ — **đã chốt (cố vấn)**. |
| Thứ tự `Players` không ổn định (Postgres không đảm bảo thứ tự khi không ORDER BY, đổi sau UPDATE điểm) | Sắp trong `BuildRoomState` theo `JoinedAt` rồi `Id`; unit test với snapshot lộn xộn, không cần Postgres; không sửa repository. |
| Rank bục vs RankBonus lệch nhau với người cùng điểm | Bục dùng `rank` bảng xếp hạng (duy nhất); RankBonus kiểu 1-1-3 là hạn chế đã biết, UI không hiện hạng thưởng — không xử lý ở đây (spec Assumptions). |
| Rò danh sách đã trả lời sang người chơi | Trường chỉ điền khi `IsHost`; test xUnit khẳng định payload người chơi null ở mọi trạng thái. |
| `QuestionEnded` phát cả phòng mang `CorrectPlayerIds` | Chấp nhận theo quyết định 6b (câu đã kết thúc, đúng/sai không còn là bí mật). |
