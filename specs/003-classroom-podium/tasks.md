# Tasks: Màn chơi kiểu lớp học + bục vinh danh

**Input**: Design documents from `/specs/003-classroom-podium/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/signalr-payloads.md, quickstart.md

**Tests**: Có — danh sách test bắt buộc do cố vấn yêu cầu (spec SC-002/005/006/007, plan, quickstart).

**Organization**: Theo 5 ĐỢT trong plan.md. Mỗi đợt kết thúc bằng checkpoint build + test xanh
và push độc lập (commit nhỏ Conventional Commits trong đợt vẫn khuyến khích). Nhãn [US1/US2/US3]
ánh xạ về user story trong spec.md để truy vết.

## Format: `[ID] [P?] [Story] Mô tả + đường dẫn file`

- **[P]**: chạy song song được (khác file, không phụ thuộc task chưa xong)
- **[Story]**: US1 = lớp học khi đang chơi, US2 = bục vinh danh, US3 = sảnh lớp học

## Path Conventions

Web app theo plan.md: `backend/QuizBattle.*`, `frontend/src/`.

---

## Đợt 1: Backend DTO + test (Foundational — chặn mọi đợt sau)

**Purpose**: Mở rộng 3 payload theo contracts/signalr-payloads.md; dữ liệu giơ tay/✓✗ sẵn sàng
cho frontend. Không migration, không sự kiện mới.

- [ ] T001 Thêm `IReadOnlyList<Guid> AnsweredPlayerIds` vào record `AnswerProgressDto` trong backend/QuizBattle.Application/Game/AnswerProgressDto.cs (giữ XML doc "chỉ gửi nhóm {code}:host")
- [ ] T002 Thêm `IReadOnlyList<Guid> CorrectPlayerIds` vào record `QuestionEndedDto` trong backend/QuizBattle.Application/Game/QuestionEndedDto.cs
- [ ] T003 Thêm `IReadOnlyList<Guid>? AnsweredPlayerIds` vào record `RoomStateDto` trong backend/QuizBattle.Application/Game/RoomStateDto.cs, kèm comment: chỉ điền cho host khi InProgress và câu hiện tại chưa ended; null cho người chơi ở MỌI trạng thái
- [ ] T004 Điền 3 trường mới trong backend/QuizBattle.Application/Game/GameService.cs: `SubmitAnswerAsync` (~dòng 275) dựng AnsweredPlayerIds từ `snapshot.Answers` của câu hiện tại + người vừa gửi; `EndCurrentQuestion` (~dòng 279) dựng CorrectPlayerIds từ answers có `IsCorrect` của câu đó; `BuildRoomState` (~dòng 353) điền AnsweredPlayerIds theo quy tắc T003 (dữ liệu đã nạp sẵn trong snapshot — không thêm truy vấn)
- [ ] T005 Sửa các chỗ khởi tạo DTO trong test giả lập hiện có cho build xanh (tìm mọi `new AnswerProgressDto(`/`new QuestionEndedDto(`/`new RoomStateDto(` trong backend/QuizBattle.Tests/)
- [ ] T006 [P] Test xUnit chống rò trong backend/QuizBattle.Tests/Game/GameServiceTests.cs: RoomState trả cho NGƯỜI CHƠI có `AnsweredPlayerIds == null` ở cả Lobby / InProgress (đã có người trả lời) / Finished
- [ ] T007 [P] Test xUnit `CorrectPlayerIds` trong backend/QuizBattle.Tests/Game/GameServiceTests.cs: 3 ca — có người không trả lời (vắng mặt trong danh sách), trả lời sai (vắng mặt), người trả lời đúng rồi RỜI PHÒNG trước khi câu kết thúc (hành vi thực tế được khẳng định bằng test, đối chiếu contract)
- [ ] T008 [P] Test xUnit host reconnect trong backend/QuizBattle.Tests/Game/GameServiceTests.cs: giữa câu có A đã trả lời, RoomState trả cho HOST chứa đúng {A}; câu đã ended → null; AnswerProgress sau mỗi lần SubmitAnswer chứa đúng danh sách tăng dần

**Checkpoint Đợt 1**:
- [ ] T009 `dotnet build` + `dotnet test` xanh (backend); `npm run build` + `npm test` xanh (frontend — chưa đụng); commit `feat: thêm AnsweredPlayerIds/CorrectPlayerIds vào payload game` + push

---

## Đợt 2: Lưới lớp học (US3 sảnh + nền cho US1)

**Purpose**: Lưới bàn học hiển thị ở cả ba màn chủ phòng, chưa có trạng thái giơ tay/✓✗.

**Independent Test**: Sảnh chủ phòng hiện bàn học khi người vào/ra; 50 người vừa màn 1366px (US3).

- [ ] T010 [US3] Tạo frontend/src/components/Classroom.tsx: nhận `players: PlayerSummary[]` (+ props trạng thái để trống cho đợt 3), render CSS grid `repeat(auto-fill, minmax(var(--desk-min), 1fr))` với bậc `--desk-min` theo số người (≤12 / ≤30 / ≤50 — research R1); mỗi desk = component `Avatar` sẵn có + tên cắt ellipsis; desk bọc `React.memo` (R2); ≤150 dòng, tách file con nếu vượt
- [ ] T011 [US3] Thêm class/token CSS cho lớp học vào frontend/src/index.css (`@layer components`, dùng token màu sẵn có — không hex mới, không gradient/bóng đổ)
- [ ] T012 [US3] Dùng `Classroom` thay `PlayerList` trong frontend/src/pages/RoomLivePage/LiveLobby.tsx (PlayPage không đổi — FR-004; `PlayerList` giữ nguyên cho nơi khác đang dùng)
- [ ] T013 [US1] Thêm `Classroom` vào frontend/src/pages/RoomLivePage/LiveQuestion.tsx (dưới khối câu hỏi; dòng "Đã trả lời x/y" giữ nguyên)
- [ ] T014 [P] [US3] Test jsdom frontend/src/components/Classroom.test.tsx (`// @vitest-environment jsdom`): render đúng số desk + tên; players đổi (vào/rời) thì lưới cập nhật; 50 players không ném lỗi

**Checkpoint Đợt 2**:
- [ ] T015 Build + test hai phía xanh; soát nhanh sảnh theo quickstart Kịch bản 1 bước 2; commit `feat: lưới lớp học trên màn chủ phòng` + push

---

## Đợt 3: Giơ tay + huy hiệu ✓/✗ (US1)

**Purpose**: Desk phản ánh trạng thái server: giơ tay khi đã trả lời, ✓/✗ khi câu kết thúc.

**Independent Test**: Quickstart Kịch bản 1 (bước 3-6) + Kịch bản 2.

- [ ] T016 [US1] Khớp types trong frontend/src/types/game.ts: `AnswerProgress.answeredPlayerIds: string[]`, `QuestionEnded.correctPlayerIds: string[]`, `RoomState.answeredPlayerIds: string[] | null` (đúng data-model.md)
- [ ] T017 [US1] Sửa frontend/src/realtime/gameReducer.ts: thêm `answeredPlayerIds: string[]` và `correctPlayerIds: string[] | null` vào GameState; `ANSWER_PROGRESS` ghi danh sách nhưng BỎ QUA khi `currentQuestion?.ended` (R3); `QUESTION_ENDED` ghi correctPlayerIds; `QUESTION_STARTED` reset cả hai; `ROOM_STATE` nạp answeredPlayerIds (?? []) và reset correctPlayerIds; `GAME_ENDED` reset
- [ ] T018 [US1] Truyền trạng thái vào `Classroom` trong frontend/src/pages/RoomLivePage/LiveQuestion.tsx và frontend/src/components/Classroom.tsx: desk "giơ tay" khi id ∈ answeredPlayerIds và câu chưa ended; khi ended: ✓ nếu id ∈ correctPlayerIds, ✗ nếu không (correctPlayerIds null → desk thường — data-model.md); huy hiệu là LỚP PHỦ, không đổi props Avatar/DiceBear (FR-011)
- [ ] T019 [US1] Thêm keyframes giơ tay + nhún nhẹ + huy hiệu vào frontend/src/index.css (dùng text-positive/text-negative; không emoji — vẽ ✓/✗ bằng SVG/ký tự trong CSS content theo hệ thiết kế)
- [ ] T020 [P] [US1] Test reducer trong frontend/src/realtime/gameReducer.test.ts: ANSWER_PROGRESS sau QUESTION_ENDED bị bỏ qua; QUESTION_STARTED xóa answered/correct; ROOM_STATE nạp answeredPlayerIds cho host và [] khi null; QUESTION_ENDED ghi correctPlayerIds
- [ ] T021 [P] [US1] Mở rộng frontend/src/components/Classroom.test.tsx: đúng desk giơ tay; khi ended thì người đúng có ✓, người sai VÀ người không trả lời có ✗; correctPlayerIds null → không huy hiệu

**Checkpoint Đợt 3**:
- [ ] T022 Build + test hai phía xanh; soát tay quickstart Kịch bản 1 + 2; commit `feat: giơ tay và huy hiệu đúng sai trên lưới lớp học` + push

---

## Đợt 4: Bục vinh danh (US2)

**Purpose**: Top 3 lộ dần 3→2→1 + pháo giấy CSS trên cả hai màn Finished.

**Independent Test**: Quickstart Kịch bản 3 (4 bước).

- [ ] T023 [P] [US2] Tạo frontend/src/components/podiumSteps.ts: hàm thuần `podiumSteps(entries: LeaderboardEntry[])` → `{ rank, entries }[]` nhóm theo `rank` server, tối đa các bậc chứa 3 người đứng đầu; đồng hạng chung bậc, dãy bậc theo `rank` thực tế (hai rank 1 → bậc 1 và 3, không bậc 2 — FR-015); không gọi mạng/DOM
- [ ] T024 [P] [US2] Test Vitest frontend/src/components/podiumSteps.test.ts: điểm phân biệt 5 người → 3 bậc; đồng hạng 1 (2 người) → bậc 1+3; 3 người cùng rank 1 → một bậc; 1 người → 1 bậc; 2 người → 2 bậc; rỗng → []
- [ ] T025 [US2] Tạo frontend/src/components/Podium.tsx: nhận `entries`, dựng bậc từ `podiumSteps`, mỗi bậc hiện nhân vật (`Avatar`) + tên + điểm; lộ dần 3→2→1 bằng animation-delay CSS; pháo giấy span thuần CSS (R5); ≤150 dòng
- [ ] T026 [US2] Thêm keyframes lộ dần + pháo giấy vào frontend/src/index.css (màu từ token; accent chỉ trên dữ liệu)
- [ ] T027 [US2] Thêm `Podium` (nguồn `finalLeaderboard ?? leaderboard`) vào frontend/src/pages/RoomLivePage/LiveFinished.tsx và frontend/src/pages/PlayPage/PlayFinished.tsx; RewardSummary + Leaderboard + nút điều hướng giữ nguyên (FR-018, FR-019)
- [ ] T028 [P] [US2] Test jsdom frontend/src/components/Podium.test.tsx: ≥3 người hiện 3 bậc đúng người đúng hạng; 2 người → 2 bậc không bậc trống; đồng hạng chung bậc

**Checkpoint Đợt 4**:
- [ ] T029 Build + test hai phía xanh; soát tay quickstart Kịch bản 3; commit `feat: bục vinh danh cuối trận` + push

---

## Đợt 5: Hoàn thiện responsive + reduced-motion (Polish)

**Purpose**: SC-004, SC-006; khớp hệ thiết kế.

- [ ] T030 Gom mọi animation của tính năng dưới `@media (prefers-reduced-motion: reduce)` trong frontend/src/index.css: tắt lộ dần/pháo giấy/nhún/giơ tay chuyển động, nội dung hiện ngay đầy đủ (FR-017); soát bằng DevTools emulation (quickstart Kịch bản 4)
- [ ] T031 Soát 1366px với 50 người giả lập (quickstart Kịch bản 4 bước 2): không cuộn ngang, tên đọc được, chuyển câu không khựng; chỉnh bậc `--desk-min` trong frontend/src/index.css hoặc frontend/src/components/Classroom.tsx nếu cần; soát Podium trên bề rộng điện thoại (PlayFinished)
- [ ] T032 Nếu máy có skill `design-taste-frontend`: chạy `node <skill>/scripts/lint.mjs --app` cho các file đã đổi và `node <skill>/scripts/inspect.mjs --app <url build>`; sửa hết FAIL (CLAUDE.md)

**Checkpoint Đợt 5**:
- [ ] T033 Build + test hai phía xanh; chạy trọn quickstart.md; commit `feat: hoàn thiện responsive và reduced-motion cho lớp học` + push

---

## Dependencies & Execution Order

- **Đợt 1** (T001→T009): chặn tất cả — payload là nền của mọi hiển thị. Trong đợt: T001-T003 song song được → T004 → T005 → T006-T008 [P] → T009.
- **Đợt 2** (T010→T015): cần Đợt 1 đã push (chỉ để types khớp về sau; bản thân lưới chưa đọc trường mới nên có thể làm song song nếu muốn, nhưng giữ tuần tự cho mỗi đợt xanh độc lập). T010→T011→T012/T013; T014 sau T010.
- **Đợt 3** (T016→T022): cần Đợt 1 (payload) + Đợt 2 (lưới). T016→T017→T018→T019; T020/T021 [P] sau T017/T018.
- **Đợt 4** (T023→T029): chỉ cần dữ liệu leaderboard SẴN CÓ — độc lập với Đợt 2/3, có thể làm trước nếu cần demo sớm; T023/T024 [P] → T025→T026→T027; T028 sau T025.
- **Đợt 5** (T030→T033): sau mọi đợt có animation (3, 4).

### Parallel Opportunities

- T001, T002, T003 (3 file DTO khác nhau); T006, T007, T008 (các test độc lập trong cùng file — viết song song, merge tuần tự); T014/T020/T021/T024/T028 song song với task triển khai kế tiếp của người khác; Đợt 4 song song với Đợt 2+3 nếu có hai người.

## Implementation Strategy

- **MVP = Đợt 1 + 2 + 3** (User Story 1): lớp học sống động khi đang chơi — demo được ngay.
- Mỗi đợt một lần push; dừng ở bất kỳ checkpoint nào vẫn có sản phẩm chạy được.
- Điểm chờ cố vấn (không chặn Đợt 1-2): hành vi host reconnect trong 6 giây kết quả (không ✓/✗ — Assumption trong spec).
