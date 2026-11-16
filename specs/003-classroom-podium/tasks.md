# Tasks: Màn chơi kiểu lớp học + bục vinh danh

**Input**: Design documents from `/specs/003-classroom-podium/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/signalr-payloads.md, quickstart.md

**Tests**: Có — danh sách test bắt buộc do cố vấn yêu cầu (spec SC-002/005/006/007, plan, quickstart).

**Organization**: Theo 5 ĐỢT trong plan.md. Mỗi đợt kết thúc bằng checkpoint build + test xanh
và push độc lập (commit nhỏ Conventional Commits trong đợt vẫn khuyến khích). Nhãn [US1/US2/US3]
ánh xạ về user story trong spec.md để truy vết.

**Tổng: 39 task / 5 đợt** (Đợt 1: T001–T015, Đợt 2: T016–T021, Đợt 3: T022–T028, Đợt 4: T029–T035, Đợt 5: T036–T039).

## Format: `[ID] [P?] [Story] Mô tả + đường dẫn file`

- **[P]**: chạy song song được (khác file, không phụ thuộc task chưa xong)
- **[Story]**: US1 = lớp học khi đang chơi, US2 = bục vinh danh, US3 = sảnh lớp học

## Path Conventions

Web app theo plan.md: `backend/QuizBattle.*`, `frontend/src/`.

---

## Đợt 1: Backend DTO + test (Foundational — chặn mọi đợt sau)

**Purpose**: Mở rộng 3 payload theo contracts/signalr-payloads.md, đổi nguồn dữ liệu đường
SubmitAnswer và ổn định thứ tự `Players`. Không migration, không sự kiện mới, KHÔNG sửa ORDER BY
trong GameRepository.

- [ ] T001 [P] Thêm `IReadOnlyList<Guid> AnsweredPlayerIds` vào record `AnswerProgressDto` trong backend/QuizBattle.Application/Game/AnswerProgressDto.cs (giữ XML doc "chỉ gửi nhóm {code}:host"; ghi thêm: có thể thiếu người nộp đồng thời, client gộp hợp — R7)
- [ ] T002 [P] Thêm `IReadOnlyList<Guid> CorrectPlayerIds` vào record `QuestionEndedDto` trong backend/QuizBattle.Application/Game/QuestionEndedDto.cs
- [ ] T003 [P] Thêm `IReadOnlyList<Guid>? AnsweredPlayerIds` vào record `RoomStateDto` trong backend/QuizBattle.Application/Game/RoomStateDto.cs, kèm comment: chỉ điền cho host khi InProgress và câu hiện tại chưa ended; null cho người chơi ở MỌI trạng thái
- [ ] T004 [P] Trong backend/QuizBattle.Application/Game/SubmitAnswerSnapshot.cs thay `int AnsweredCountForCurrentQuestion` bằng `IReadOnlyList<Guid> AnsweredPlayerIds` (PlayerId các answer của câu hiện tại; số đếm = `.Count`); không đụng `GameSnapshot.AnsweredCountForCurrentQuestion` (class khác, đã có `Answers`)
- [ ] T005 Trong backend/QuizBattle.Infrastructure/Persistence/GameRepository.cs `SubmitAnswerAsync` (~dòng 139) thay `CountAsync` bằng MỘT truy vấn `Select(a => a.PlayerId).ToListAsync` cho câu hiện tại (rỗng nếu `question` null), gán vào `SubmitAnswerSnapshot.AnsweredPlayerIds` (phụ thuộc T004); KHÔNG sửa `LoadSnapshotAsync`/ORDER BY
- [ ] T006 Trong backend/QuizBattle.Application/Game/GameService.cs `SubmitAnswerAsync` (~dòng 275) dựng `AnswerProgressDto`: `AnsweredPlayerIds` = `snapshot.AnsweredPlayerIds` + `playerId` vừa nộp, `AnsweredCount` = `snapshot.AnsweredPlayerIds.Count + 1` (giá trị y như trước) (phụ thuộc T001, T004)
- [ ] T007 Trong backend/QuizBattle.Application/Game/GameService.cs `EndCurrentQuestion` (~dòng 279) điền `CorrectPlayerIds` = `PlayerId` của `snapshot.Answers` thuộc câu đang kết thúc có `IsCorrect` (người rời phòng/rớt mạng vẫn nằm trong snapshot nên vẫn có mặt) (phụ thuộc T002)
- [ ] T008 Trong backend/QuizBattle.Application/Game/GameService.cs `BuildRoomState` (~dòng 353) điền `RoomStateDto.AnsweredPlayerIds` từ `snapshot.Answers`: danh sách khi `isHost` && `Status == InProgress` && câu hiện tại chưa ended; mọi trường hợp khác `null` (phụ thuộc T003)
- [ ] T009 Trong backend/QuizBattle.Application/Game/GameService.cs `BuildRoomState` (dòng ~357) sắp `snapshot.Players` theo `JoinedAt` rồi `Id` trước khi map sang `PlayerSummaryDto` (OrderBy(JoinedAt).ThenBy(Id)); đây là chỗ DUY NHẤT dựng RoomState; không đổi `BuildLeaderboard`
- [ ] T010 Sửa các chỗ dựng DTO/snapshot trong test cho build xanh: backend/QuizBattle.Tests/TestHelpers/FakeGameRepository.cs (~dòng 96, dựng `SubmitAnswerSnapshot` bằng list PlayerId từ `Answers` của câu hiện tại) và mọi `new AnswerProgressDto(`/`new QuestionEndedDto(`/`new RoomStateDto(` trong backend/QuizBattle.Tests/ (phụ thuộc T001–T004)
- [ ] T011 [P] Test xUnit chống rò trong backend/QuizBattle.Tests/Game/GameServiceTests.cs: RoomState trả cho NGƯỜI CHƠI có `AnsweredPlayerIds == null` ở Lobby / InProgress (đã có người trả lời) / Finished
- [ ] T012 [P] Test xUnit `CorrectPlayerIds` trong backend/QuizBattle.Tests/Game/GameServiceTests.cs: 3 ca — có người không trả lời (vắng mặt), trả lời sai (vắng mặt), người trả lời đúng rồi RỜI PHÒNG/rớt mạng trước khi câu kết thúc (Player không bị xóa → vẫn có mặt trong `CorrectPlayerIds`)
- [ ] T013 [P] Test xUnit host reconnect trong backend/QuizBattle.Tests/Game/GameServiceTests.cs: giữa câu có A đã trả lời, RoomState trả cho HOST chứa đúng {A}; câu đã ended → null; `AnswerProgress` sau mỗi lần `SubmitAnswerAsync` chứa danh sách tăng dần gồm cả người vừa nộp
- [ ] T014 [P] Test xUnit thứ tự ổn định trong backend/QuizBattle.Tests/Game/GameServiceTests.cs: snapshot có `Players` lộn xộn (JoinedAt không tăng dần, có hai người cùng `JoinedAt` khác `Id`, điểm khác nhau) → `RoomState.Players` luôn theo `JoinedAt` rồi `Id`, giống nhau cho host và người chơi, không đổi khi điểm đổi

**Checkpoint Đợt 1**:
- [ ] T015 `dotnet build` + `dotnet test` xanh (backend); `npm run build` + `npm test` xanh (frontend — chưa đụng); commit `feat: thêm AnsweredPlayerIds/CorrectPlayerIds vào payload game và ổn định thứ tự người chơi` + push

---

## Đợt 2: Lưới lớp học (US3 sảnh + nền cho US1)

**Purpose**: Lưới bàn học hiển thị ở cả ba màn chủ phòng, chưa có trạng thái giơ tay/✓✗.

**Independent Test**: Sảnh chủ phòng hiện bàn học khi người vào; người rời giữ bàn; 50 người vừa màn 1366px (US3).

- [ ] T016 [US3] Tạo frontend/src/components/Classroom.tsx: nhận `players` (kiểu của `state.players`, có `online`) + props trạng thái để trống cho đợt 3; render CSS grid `repeat(auto-fill, minmax(var(--desk-min), 1fr))` với bậc `--desk-min` theo số người (≤12 / ≤30 / ≤50 — research R1); mỗi desk = component `Avatar` sẵn có + tên cắt ellipsis; hiển thị ĐÚNG THỨ TỰ `players` nhận được, KHÔNG sắp theo điểm/leaderboard (FR-022); người `online:false` vẫn giữ bàn (không hiển thị online — ngoài phạm vi); desk bọc `React.memo` (R2); ≤150 dòng, tách file con nếu vượt
- [ ] T017 [US3] Thêm class/token CSS cho lớp học vào frontend/src/index.css (`@layer components`, dùng token màu sẵn có — không hex mới, không gradient/bóng đổ)
- [ ] T018 [US3] Dùng `Classroom` thay `PlayerList` trong frontend/src/pages/RoomLivePage/LiveLobby.tsx (PlayPage không đổi — FR-004; `PlayerList` giữ nguyên cho nơi khác đang dùng)
- [ ] T019 [US1] Thêm `Classroom` vào frontend/src/pages/RoomLivePage/LiveQuestion.tsx (dưới khối câu hỏi; dòng "Đã trả lời x/y" giữ nguyên)
- [ ] T020 [P] [US3] Test jsdom frontend/src/components/Classroom.test.tsx (`// @vitest-environment jsdom`): render đúng số desk + tên; GIỮ NGUYÊN thứ tự đầu vào dù điểm đảo ngược; người mới (cuối mảng) xuất hiện cuối; người `online:false` vẫn có desk; 50 players không ném lỗi

**Checkpoint Đợt 2**:
- [ ] T021 Build + test hai phía xanh; soát nhanh sảnh theo quickstart Kịch bản 1 bước 2; commit `feat: lưới lớp học trên màn chủ phòng` + push

---

## Đợt 3: Giơ tay + huy hiệu ✓/✗ (US1)

**Purpose**: Desk phản ánh trạng thái server: giơ tay khi đã trả lời, ✓/✗ khi câu kết thúc.

**Independent Test**: Quickstart Kịch bản 1 (bước 3-6) + Kịch bản 2.

- [ ] T022 [US1] Khớp types trong frontend/src/types/game.ts: `AnswerProgress.answeredPlayerIds: string[]`, `QuestionEnded.correctPlayerIds: string[]`, `RoomState.answeredPlayerIds: string[] | null` (đúng data-model.md); cập nhật chú thích `AnswerProgress`: danh sách có thể thiếu người nộp đồng thời
- [ ] T023 [US1] Sửa frontend/src/realtime/gameReducer.ts: thêm `answeredPlayerIds: string[]` và `correctPlayerIds: string[] | null` vào GameState; `ANSWER_PROGRESS` GỘP HỢP (union, chỉ thêm, không ghi đè) vào `answeredPlayerIds` hiện có của cùng câu và BỎ QUA khi `currentQuestion?.ended` (R3, R7); `QUESTION_ENDED` ghi `correctPlayerIds`; `QUESTION_STARTED` reset `answeredPlayerIds = []`, `correctPlayerIds = null`; `ROOM_STATE` THAY THẾ `answeredPlayerIds` bằng `payload ?? []` và reset `correctPlayerIds`; `GAME_ENDED` reset; `PLAYER_JOINED`/`PLAYER_LEFT` giữ nguyên hành vi (nối cuối / `online:false`)
- [ ] T024 [US1] Truyền trạng thái vào `Classroom` trong frontend/src/pages/RoomLivePage/LiveQuestion.tsx và frontend/src/components/Classroom.tsx: desk "giơ tay" khi id ∈ answeredPlayerIds và câu chưa ended; khi ended: ✓ nếu id ∈ correctPlayerIds, ✗ nếu không (correctPlayerIds null → desk thường — data-model.md); huy hiệu là LỚP PHỦ, không đổi props Avatar/DiceBear (FR-011)
- [ ] T025 [US1] Thêm keyframes giơ tay + nhún nhẹ + huy hiệu vào frontend/src/index.css (dùng text-positive/text-negative; không emoji — vẽ ✓/✗ bằng SVG/ký tự trong CSS content theo hệ thiết kế)
- [ ] T026 [P] [US1] Test reducer trong frontend/src/realtime/gameReducer.test.ts: {A} rồi {B} → {A,B}; {A,B} rồi {A} (đến trễ) → vẫn {A,B}; ANSWER_PROGRESS sau QUESTION_ENDED bị bỏ qua; QUESTION_STARTED xóa answered/correct; ROOM_STATE thay thế answeredPlayerIds (host) và ra [] khi null; QUESTION_ENDED ghi correctPlayerIds; PLAYER_JOINED nối cuối và PLAYER_LEFT giữ người đó (online:false) cùng thứ tự
- [ ] T027 [P] [US1] Mở rộng frontend/src/components/Classroom.test.tsx: đúng desk giơ tay; khi ended thì người đúng có ✓, người sai VÀ người không trả lời có ✗; correctPlayerIds null → không huy hiệu; người `online:false` đã trả lời đúng vẫn có ✓

**Checkpoint Đợt 3**:
- [ ] T028 Build + test hai phía xanh; soát tay quickstart Kịch bản 1 + 2; commit `feat: giơ tay và huy hiệu đúng sai trên lưới lớp học` + push

---

## Đợt 4: Bục vinh danh (US2)

**Purpose**: Top 3 lộ dần 3→2→1 + pháo giấy CSS trên cả hai màn Finished.

**Independent Test**: Quickstart Kịch bản 3 (4 bước).

- [ ] T029 [P] [US2] Tạo frontend/src/components/podiumSteps.ts: hàm thuần `podiumSteps(entries: LeaderboardEntry[])` → `{ rank, entry }[]`: lọc `rank <= 3`, sắp tăng dần theo `rank`; mỗi bậc đúng MỘT người vì `rank` bảng xếp hạng luôn duy nhất (GameService.cs:383-385); không nhóm, không tính lại hạng, không gọi mạng/DOM (data-model.md)
- [ ] T030 [P] [US2] Test Vitest frontend/src/components/podiumSteps.test.ts: ≥5 người → đúng 3 bậc rank 1/2/3 theo thứ tự; hai người CÙNG ĐIỂM nhưng rank 1 và 2 → hai bậc khác nhau đúng thứ tự rank; đầu vào sắp lộn xộn → vẫn ra 1,2,3; 1 người → 1 bậc; 2 người → 2 bậc; rỗng → []
- [ ] T031 [US2] Tạo frontend/src/components/Podium.tsx: nhận `entries`, dựng bậc từ `podiumSteps`, mỗi bậc hiện nhân vật (`Avatar`) + tên + điểm; lộ dần 3→2→1 bằng animation-delay CSS; pháo giấy span thuần CSS (R5); ≤150 dòng
- [ ] T032 [US2] Thêm keyframes lộ dần + pháo giấy vào frontend/src/index.css (màu từ token; accent chỉ trên dữ liệu)
- [ ] T033 [US2] Thêm `Podium` (nguồn `finalLeaderboard ?? leaderboard`) vào frontend/src/pages/RoomLivePage/LiveFinished.tsx và frontend/src/pages/PlayPage/PlayFinished.tsx; RewardSummary + Leaderboard + nút điều hướng giữ nguyên (FR-018, FR-019)
- [ ] T034 [P] [US2] Test jsdom frontend/src/components/Podium.test.tsx: ≥3 người hiện 3 bậc đúng người đúng hạng; 2 người → 2 bậc không bậc trống; hai người cùng điểm khác rank → hai bậc khác nhau

**Checkpoint Đợt 4**:
- [ ] T035 Build + test hai phía xanh; soát tay quickstart Kịch bản 3; commit `feat: bục vinh danh cuối trận` + push

---

## Đợt 5: Hoàn thiện responsive + reduced-motion (Polish)

**Purpose**: SC-004, SC-006; khớp hệ thiết kế.

- [ ] T036 Gom mọi animation của tính năng dưới `@media (prefers-reduced-motion: reduce)` trong frontend/src/index.css: tắt lộ dần/pháo giấy/nhún/giơ tay chuyển động, nội dung hiện ngay đầy đủ (FR-017); soát bằng DevTools emulation (quickstart Kịch bản 4)
- [ ] T037 Soát 1366px với 50 người giả lập (quickstart Kịch bản 4 bước 2): không cuộn ngang, tên đọc được, chuyển câu không khựng; chỉnh bậc `--desk-min` trong frontend/src/index.css hoặc frontend/src/components/Classroom.tsx nếu cần; soát Podium trên bề rộng điện thoại (PlayFinished)
- [ ] T038 Nếu máy có skill `design-taste-frontend`: chạy `node <skill>/scripts/lint.mjs --app` cho các file đã đổi và `node <skill>/scripts/inspect.mjs --app <url build>`; sửa hết FAIL (CLAUDE.md)

**Checkpoint Đợt 5**:
- [ ] T039 Build + test hai phía xanh; chạy trọn quickstart.md; commit `feat: hoàn thiện responsive và reduced-motion cho lớp học` + push

---

## Dependencies & Execution Order

- **Đợt 1** (T001→T015): chặn tất cả — payload là nền của mọi hiển thị. T001–T004 song song được (4 file khác nhau) → T005 (sau T004) → T006 (sau T001, T004), T007 (sau T002), T008 (sau T003), T009 → T010 (sau T001–T004) → T011–T014 [P] → T015.
- **Đợt 2** (T016→T021): cần Đợt 1 đã push (thứ tự `Players` ổn định là điều kiện để lưới không đổi chỗ khi reconnect). T016→T017→T018/T019; T020 sau T016.
- **Đợt 3** (T022→T028): cần Đợt 1 (payload) + Đợt 2 (lưới). T022→T023→T024→T025; T026/T027 [P] sau T023/T024.
- **Đợt 4** (T029→T035): chỉ cần dữ liệu leaderboard SẴN CÓ — độc lập với Đợt 2/3, làm song song được nếu cần demo sớm; T029/T030 [P] → T031→T032→T033; T034 sau T031.
- **Đợt 5** (T036→T039): sau mọi đợt có animation (3, 4).

### Parallel Opportunities

- T001–T004 (4 file riêng); T011–T014 (các test độc lập trong cùng file — viết song song, merge tuần tự); T029/T030 song song với nhau; Đợt 4 song song với Đợt 2+3 nếu có hai người.

## Implementation Strategy

- **MVP = Đợt 1 + 2 + 3** (User Story 1): lớp học sống động khi đang chơi — demo được ngay.
- Mỗi đợt một lần push; dừng ở bất kỳ checkpoint nào vẫn có sản phẩm chạy được.
- Không còn điểm chờ cố vấn: host reconnect trong 6 giây kết quả không có ✓/✗ là hành vi **đã chốt**.
