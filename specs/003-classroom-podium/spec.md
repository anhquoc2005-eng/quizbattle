# Feature Specification: Màn chơi kiểu lớp học + bục vinh danh

**Feature Branch**: `feat/classroom-podium`

**Created**: 2026-11-16

**Status**: Draft

**Input**: User description: "Màn chơi kiểu lớp học + bục vinh danh (classroom-podium). Cảnh lớp học chỉ ở màn chủ phòng (RoomLivePage: Lobby/Question/Finished); PlayPage giữ nguyên, chỉ thêm bục vinh danh ở PlayFinished. Chủ phòng như giáo viên trước bảng câu hỏi; người chơi ngồi lưới bàn học (nhân vật DiceBear đang đeo + tên), co giãn tới MAX_PLAYERS=50, dùng từ laptop 1366px tới máy chiếu. Trong câu: ai đã trả lời thì nhân vật giơ tay (không lộ đáp án). Sau QuestionEnded: huy hiệu đúng/sai phủ lên nhân vật + nhún nhẹ, không đổi thuộc tính DiceBear. Cuối trận: bục vinh danh top 3 lộ dần hạng 3→2→1, pháo giấy CSS, có trên cả hai màn, đồng hạng theo Rank server, dưới 3 người chỉ hiện bậc có người, tôn trọng prefers-reduced-motion. Backend chỉ: AnswerProgressDto thêm AnsweredPlayerIds (chỉ nhóm host); QuestionEndedDto thêm CorrectPlayerIds (cả phòng); RoomState cho chủ phòng có danh sách người đã trả lời câu hiện tại (người chơi không nhận). Không migration, không thư viện mới. Ngoài phạm vi: trạng thái online ở sảnh, WebApplicationFactory."

## Bối cảnh

Màn chủ phòng (RoomLivePage) hiện hiển thị danh sách người chơi và tiến độ trả lời dạng chữ
("Đã trả lời 3/10"). Tính năng này biến màn chủ phòng — thường được chiếu lên máy chiếu cho cả
lớp xem — thành một "lớp học": mỗi người chơi là một nhân vật DiceBear ngồi bàn học, giơ tay khi
đã trả lời, nhận huy hiệu ✓/✗ khi câu kết thúc. Cuối trận, cả màn chủ phòng lẫn màn người chơi
hiện bục vinh danh top 3 với hiệu ứng lộ dần.

## Quyết định sản phẩm đã chốt *(không tự thay đổi)*

1. Cảnh lớp học **chỉ** hiện ở màn chủ phòng (RoomLivePage: Lobby / Question / Finished).
   Màn người chơi (PlayPage) giữ nguyên, chỉ thêm bục vinh danh ở PlayFinished.
2. Bố cục: chủ phòng như giáo viên đứng trước bảng câu hỏi; người chơi ngồi lưới bàn học,
   mỗi bàn = nhân vật DiceBear đang đeo (AvatarDto sẵn có) + tên. Lưới tự co giãn tới
   `Room.MAX_PLAYERS` = 50, dùng được từ màn laptop 1366px tới máy chiếu.
3. Trong câu: người đã trả lời → nhân vật "giơ tay". Chỉ biết **đã trả lời**, KHÔNG lộ đáp án đã chọn.
4. Sau QuestionEnded: huy hiệu ✓/✗ phủ lên nhân vật + nhún nhẹ. KHÔNG đổi biểu cảm/thuộc tính
   DiceBear (đó là đồ người chơi mua bằng Xu).
5. Bục vinh danh cuối trận: top 3, lộ dần hạng 3→2→1, pháo giấy bằng CSS; có trên cả màn chủ
   phòng và màn người chơi; xếp theo `Rank` server trả (luôn duy nhất, xem mục Mâu thuẫn); dưới
   3 người chỉ hiện số bậc có người; tôn trọng `prefers-reduced-motion`.
6. Không migration, không thêm thư viện. Backend chỉ:
   - a) `AnswerProgressDto` thêm `AnsweredPlayerIds` (vẫn chỉ gửi nhóm `{code}:host`);
   - b) `QuestionEndedDto` thêm `CorrectPlayerIds` (gửi cả phòng; chấp nhận vì câu đã kết thúc);
   - c) `RoomState` gửi cho chủ phòng có danh sách người đã trả lời câu hiện tại (để chủ phòng
     kết nối lại giữa câu vẫn đúng). Người chơi KHÔNG nhận danh sách này.

   Cập nhật types frontend + `src/realtime/events.ts` nếu cần; tên sự kiện giữ nguyên.
7. Ngoài phạm vi: trạng thái online ở sảnh, WebApplicationFactory.

## Mâu thuẫn giữa quyết định và code thật

Đối chiếu `RoomStateDto`, `AnswerProgressDto`, `QuestionEndedDto`, `LeaderboardEntryDto`,
`PlayerSummaryDto`, `GameService`, `GameRepository`, `GameHub`, `gameReducer`, `RewardCalculator`,
`Room.MAX_PLAYERS`, `src/avatar/avatarRenderer.ts`, RoomLivePage/PlayPage. Sau review cố vấn
(bản sửa này), hai điểm quyết định/giả định lệch với code thật, đã xử lý như sau:

1. **Quyết định 5 nói "đồng hạng theo Rank server", nhưng `Rank` của bảng xếp hạng luôn duy nhất.**
   `BuildLeaderboard` (GameService.cs:383-385) gán `Rank = i + 1` sau
   `OrderByDescending(Score).ThenBy(TotalCorrectMs)` → không bao giờ có hai `LeaderboardEntryDto`
   cùng `Rank`. Xử lý: mỗi bậc đúng một người (rank 1/2/3); người cùng điểm vẫn ở hai bậc khác
   nhau theo thứ tự server. (Ngoại lệ có thật nằm ở thưởng, xem Assumptions về RankBonus.)
2. **Người chơi rời phòng / rớt mạng KHÔNG bị xóa.** `LeaveRoom` và `OnDisconnectedAsync` chỉ phát
   `PlayerLeft`; `Player` còn trong DB/snapshot và reducer giữ người đó với `online: false`.
   Nên bàn học của người đã rời **vẫn còn** (chỉ trạng thái online khác, và hiển thị trạng thái
   online nằm ngoài phạm vi theo quyết định 7). Xử lý: US3, Edge Cases, FR-003 sửa theo đó.

Các điểm đã khớp:

- `PlayerSummaryDto`/`LeaderboardEntryDto` đã mang `Avatar` đầy đủ → lưới lớp học và bục vinh
  danh không cần dữ liệu avatar mới.
- `GameEnded.finalLeaderboard` và `RoomState.leaderboard` (khi Finished) đã có `rank` → bục vinh
  danh không cần dữ liệu backend mới.
- `AnswerProgress` hiện chỉ gửi nhóm host; `QuestionEnded` gửi cả phòng → khớp quyết định 6.
- `SubmitAnswerSnapshot` chỉ có số đếm (`AnsweredCountForCurrentQuestion`), không có danh sách
  answers → backend phải đổi truy vấn đếm thành truy vấn lấy `PlayerId` (xem plan/research R7).
- `avatarRenderer.ts` đã có cache Map (giới hạn MAX_CACHED) → rủi ro vẽ 50 SVG đã có sẵn lớp đệm.
- `RoomState.hasAnsweredCurrentQuestion` là cờ cá nhân của người chơi, không đụng quyết định 6c
  (danh sách cho host là trường mới, bổ sung chứ không thay thế).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Lớp học trên màn chủ phòng khi đang chơi (Priority: P1)

Chủ phòng chiếu màn hình lên máy chiếu. Trong lúc câu hỏi đang chạy, mỗi người chơi là một nhân
vật ngồi bàn học; ai bấm trả lời xong thì nhân vật đó giơ tay ngay. Cả lớp nhìn máy chiếu biết ai
đã xong mà không lộ ai chọn đáp án nào. Khi câu kết thúc, nhân vật trả lời đúng nhận huy hiệu ✓,
trả lời sai hoặc không trả lời nhận ✗, kèm cái nhún nhẹ.

**Why this priority**: Đây là giá trị cốt lõi của tính năng — biến tiến độ dạng chữ thành cảnh
lớp học sống động; các phần còn lại (sảnh, bục) xây trên cùng lưới này.

**Independent Test**: Mở phòng với vài người chơi, bắt đầu trận; lần lượt trả lời từ từng thiết
bị và quan sát màn chủ phòng: tay giơ đúng người, đúng lúc; kết thúc câu thấy ✓/✗ đúng với đáp án
từng người.

**Acceptance Scenarios**:

1. **Given** câu hỏi đang chạy và chưa ai trả lời, **When** người chơi A gửi đáp án, **Then**
   màn chủ phòng hiện nhân vật A giơ tay trong khi các nhân vật khác giữ nguyên, và không nơi nào
   trên màn hình lộ đáp án A đã chọn.
2. **Given** câu hỏi đang chạy, **When** nhìn màn của một người chơi khác (PlayPage), **Then**
   người đó KHÔNG thấy danh sách ai đã trả lời (chỉ thấy giao diện trả lời như hiện tại).
3. **Given** câu vừa kết thúc (QuestionEnded), **When** màn chủ phòng cập nhật, **Then** mỗi nhân
   vật trả lời đúng có huy hiệu ✓, trả lời sai hoặc không trả lời có huy hiệu ✗, nhân vật nhún
   nhẹ, và hình dạng/biểu cảm DiceBear của nhân vật không đổi.
4. **Given** chủ phòng rớt mạng giữa câu khi đã có người trả lời, **When** chủ phòng kết nối lại,
   **Then** các nhân vật đã trả lời vẫn giơ tay đúng (trạng thái khôi phục từ RoomState, không
   chờ sự kiện kế tiếp).

---

### User Story 2 - Bục vinh danh cuối trận trên cả hai màn (Priority: P2)

Trận kết thúc. Trên cả màn chủ phòng và màn người chơi, bục vinh danh hiện ra: hạng 3 lộ trước,
rồi hạng 2, rồi hạng 1, kèm pháo giấy. Người chơi vẫn thấy phần thưởng cá nhân của mình như cũ.

**Why this priority**: Khoảnh khắc "trao giải" là điểm nhấn cảm xúc, nhưng trận vẫn chơi được
trọn vẹn nếu thiếu nó (đã có bảng xếp hạng).

**Independent Test**: Chơi hết một trận với ≥3 người có điểm khác nhau; quan sát cả hai màn:
thứ tự lộ 3→2→1, đúng người đúng hạng theo bảng xếp hạng server.

**Acceptance Scenarios**:

1. **Given** trận kết thúc với ≥3 người chơi điểm khác nhau, **When** màn Finished hiện,
   **Then** bục 3 bậc lộ dần theo thứ tự hạng 3 → hạng 2 → hạng 1 (nhân vật + tên + điểm),
   kèm hiệu ứng pháo giấy, trên cả màn chủ phòng lẫn màn người chơi.
2. **Given** hai người có cùng điểm (server vẫn gán `rank` khác nhau, ví dụ 1 và 2 nhờ tổng thời
   gian trả lời đúng), **When** bục hiện, **Then** họ đứng ở hai bậc khác nhau đúng theo `rank`
   server trả (mỗi bậc đúng một người); client không tự gộp hay tự xếp lại theo điểm.
3. **Given** trận chỉ có 1 hoặc 2 người chơi, **When** bục hiện, **Then** chỉ các bậc có người
   xuất hiện, không có bậc trống.
4. **Given** người dùng bật `prefers-reduced-motion`, **When** màn Finished hiện, **Then** bục và
   kết quả hiện ngay không hiệu ứng lộ dần, không pháo giấy chuyển động.

---

### User Story 3 - Sảnh chờ kiểu lớp học (Priority: P3)

Trước khi bắt đầu, màn chủ phòng hiện lớp học: người chơi vào phòng là "ngồi vào bàn" (nhân vật +
tên xuất hiện trong lưới); người rời phòng/rớt mạng vẫn giữ bàn (họ chưa bị xóa khỏi trận, giữ
điểm — xem Mâu thuẫn mục 2). Lưới dùng được từ 1 tới 50 người, nhìn rõ trên
laptop 1366px lẫn máy chiếu.

**Why this priority**: Làm sảnh đồng bộ với cảnh trong trận; giá trị thấp hơn vì sảnh hiện tại đã
có danh sách người chơi dùng được.

**Independent Test**: Mở sảnh chủ phòng, cho người vào/ra và quan sát lưới cập nhật; thử với số
lượng lớn (giả lập 50 người) xem lưới còn đọc được.

**Acceptance Scenarios**:

1. **Given** sảnh chủ phòng đang mở, **When** một người chơi vào phòng, **Then** nhân vật + tên
   của họ xuất hiện trong lưới, nối vào cuối dãy bàn hiện có; khi họ rời phòng hoặc rớt mạng, bàn
   của họ vẫn còn đúng chỗ cũ (không biến mất, không xô lệch bàn khác).
2. **Given** phòng có 50 người chơi, **When** nhìn màn chủ phòng ở bề rộng 1366px, **Then** toàn
   bộ 50 nhân vật + tên hiển thị không tràn ngang, tên vẫn đọc được.

---

### Edge Cases

- AnswerProgress đến **sau** QuestionEnded (mạng trễ): màn chủ phòng đang ở pha ✓/✗ không được
  quay lại pha giơ tay; dữ liệu giơ tay của câu đã kết thúc bị bỏ qua.
- Người chơi rời phòng hoặc rớt mạng giữa câu (Player không bị xóa): bàn vẫn còn; nếu đã trả
  lời trước đó thì tay vẫn giơ, và khi câu kết thúc vẫn nhận ✓/✗ như mọi người (`CorrectPlayerIds`
  chứa họ nếu đã trả lời đúng). Hiển thị trạng thái online/offline của bàn nằm ngoài phạm vi.
- Hai người nộp đáp án gần như cùng lúc: mỗi `AnswerProgress` có thể thiếu người kia (cả hai
  giao dịch chưa thấy answer chưa commit của nhau) → màn chủ phòng gộp hợp các danh sách nhận
  được trong cùng một câu nên cả hai đều giơ tay; không bao giờ có người đã giơ tay mà bị hạ
  xuống trước khi câu kết thúc (research R7).
- Chủ phòng kết nối lại đúng khoảng 6 giây hiển thị kết quả (câu đã ended, chưa sang câu mới):
  RoomState có `currentQuestion.ended = true` + `correctIndex`, nhưng không có `CorrectPlayerIds`
  của sự kiện đã bỏ lỡ → màn chủ phòng hiện lưới không huy hiệu. **Đã chốt (cố vấn): chấp nhận**,
  vì trạng thái ✓/✗ chỉ tồn tại trong 6 giây và sẽ sạch khi sang câu mới.
- Chủ phòng kết nối lại: thứ tự bàn không được đổi so với trước khi rớt (thứ tự cố định theo
  thời điểm vào phòng, không phụ thuộc điểm hay thứ tự Postgres trả về).
- Trận kết thúc khi người chơi đang ở màn câu hỏi: chuyển sang Finished vẫn phải hiện bục (dữ
  liệu từ GameEnded), kể cả khi người chơi vào lại sau (dữ liệu từ RoomState khi Finished).
- Tên quá dài trên bàn học / trên bục: cắt bớt có dấu "…", không vỡ lưới.

## Requirements *(mandatory)*

### Functional Requirements

**Lưới lớp học (màn chủ phòng)**

- **FR-001**: Màn chủ phòng ở cả ba trạng thái Lobby / Question / Finished hiển thị người chơi
  dưới dạng lưới bàn học: mỗi bàn gồm nhân vật đang đeo + tên hiển thị.
- **FR-002**: Lưới tự co giãn từ 1 tới 50 người, hiển thị đủ không cuộn ngang ở bề rộng từ
  1366px trở lên; dưới 1366px (chủ phòng mở bằng máy nhỏ) được phép cuộn dọc nhưng không vỡ bố cục.
- **FR-003**: Người chơi vào phòng thì bàn mới xuất hiện ngay theo sự kiện hiện có
  (PlayerJoined/RoomState), không gọi thêm API. Người rời phòng/rớt mạng (PlayerLeft) giữ nguyên
  bàn; không hiển thị trạng thái online (ngoài phạm vi).
- **FR-004**: Màn người chơi (PlayPage) không đổi ở Lobby và Question.

**Giơ tay trong câu**

- **FR-005**: Khi đang trong câu, nhân vật của người đã trả lời hiển thị trạng thái "giơ tay";
  người chưa trả lời giữ trạng thái thường.
- **FR-006**: Trạng thái giơ tay chỉ cho biết ĐÃ trả lời; không có bất kỳ dấu hiệu nào về đáp án
  được chọn (kể cả trong dữ liệu gửi xuống màn chủ phòng trước khi câu kết thúc).
- **FR-007**: Danh sách người đã trả lời chỉ được gửi cho chủ phòng (nhóm `{code}:host` và
  RoomState của chủ phòng); payload gửi người chơi không chứa danh sách này ở mọi thời điểm
  trước khi câu kết thúc.
- **FR-008**: Chủ phòng kết nối lại giữa câu nhận được danh sách người đã trả lời câu hiện tại
  trong RoomState, khôi phục đúng trạng thái giơ tay.
- **FR-009**: Sang câu mới, mọi tay giơ và huy hiệu của câu trước được xóa.

**Huy hiệu ✓/✗ sau khi câu kết thúc**

- **FR-010**: Khi câu kết thúc, mọi client trong phòng nhận danh sách người trả lời đúng
  (`CorrectPlayerIds`); màn chủ phòng phủ huy hiệu ✓ lên nhân vật trả lời đúng, ✗ lên nhân vật
  trả lời sai hoặc không trả lời, kèm chuyển động nhún nhẹ.
- **FR-011**: Huy hiệu là lớp phủ; thuộc tính/biểu cảm DiceBear của nhân vật không bị thay đổi.
- **FR-012**: Sự kiện tiến độ trả lời đến sau khi câu đã kết thúc không làm màn hình quay lại
  trạng thái giơ tay.

**Bục vinh danh cuối trận**

- **FR-013**: Khi trận kết thúc, cả màn chủ phòng và màn người chơi hiển thị bục vinh danh gồm
  tối đa 3 bậc (nhân vật + tên + điểm), xếp theo `rank` trong bảng xếp hạng cuối server trả về.
- **FR-014**: Các bậc lộ dần theo thứ tự hạng 3 → 2 → 1, kết thúc bằng hiệu ứng pháo giấy dựng
  thuần bằng CSS.
- **FR-015**: Mỗi bậc đúng MỘT người: bậc 1/2/3 ứng với `rank` 1/2/3 server trả (`rank` của bảng
  xếp hạng luôn duy nhất — GameService.cs:383-385). Client chỉ lấy các entry có `rank` ≤ 3 và
  sắp theo `rank`; không tự gộp theo điểm, không tự tính lại hạng.
- **FR-016**: Dưới 3 người chơi: chỉ hiển thị các bậc có người.
- **FR-017**: Khi `prefers-reduced-motion` bật: bỏ lộ dần, bỏ pháo giấy chuyển động, bỏ nhún —
  nội dung hiện ngay, đầy đủ.
- **FR-018**: Người chơi vào lại phòng sau khi trận đã kết thúc vẫn thấy bục (dựng từ trạng thái
  phòng, không phụ thuộc đã nhận sự kiện kết thúc hay chưa).
- **FR-019**: Phần thưởng cá nhân (RewardSummary) trên màn người chơi giữ nguyên như hiện tại.

**Ràng buộc dữ liệu**

- **FR-020**: Không thay đổi schema database, không migration, không thư viện mới.
- **FR-021**: Không thêm/đổi tên sự kiện SignalR; chỉ mở rộng payload sự kiện hiện có theo
  quyết định 6.

**Thứ tự chỗ ngồi và gộp tiến độ**

- **FR-022**: Danh sách `Players` trong RoomState được server sắp theo `JoinedAt` rồi `Id` (ổn
  định, không phụ thuộc thứ tự Postgres trả về hay điểm số), nên chủ phòng kết nối lại thấy
  nguyên thứ tự bàn. `PLAYER_JOINED` ở client nối người mới vào cuối như hiện tại. Lưới lớp học
  hiển thị đúng thứ tự `players` nhận được, KHÔNG sắp theo điểm hay bảng xếp hạng.
- **FR-023**: Client gộp HỢP (chỉ thêm, không ghi đè) các `AnsweredPlayerIds` nhận được trong
  cùng một câu; `AnsweredPlayerIds` trong RoomState là nguồn đầy đủ khi vào lại và thay thế
  danh sách hiện có. Đổi câu thì xóa sạch.

### Key Entities

- **Bàn học (desk)**: một ô trong lưới lớp học = người chơi (nhân vật + tên) + trạng thái
  (thường / giơ tay / ✓ / ✗). Dữ liệu gốc: danh sách người chơi hiện có của phòng.
- **Danh sách đã trả lời (AnsweredPlayerIds)**: tập Id người chơi đã trả lời câu hiện tại;
  chỉ chủ phòng nhận; reset mỗi câu.
- **Danh sách trả lời đúng (CorrectPlayerIds)**: tập Id người chơi trả lời đúng câu vừa kết
  thúc; cả phòng nhận cùng sự kiện câu kết thúc.
- **Bậc vinh danh**: hạng (`rank` 1/2/3) + đúng một người đứng bậc đó; suy ra thuần túy từ bảng
  xếp hạng cuối, không có dữ liệu mới.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Trong một trận 5 người, người xem màn chủ phòng xác định được ai đã trả lời trong
  vòng 1 giây sau khi người đó bấm, chỉ bằng cách nhìn lưới lớp học.
- **SC-002**: Không tồn tại thời điểm nào trước khi câu kết thúc mà dữ liệu gửi tới client
  người chơi chứa danh sách ai đã trả lời (kiểm chứng bằng test payload).
- **SC-003**: Sau khi câu kết thúc, huy hiệu ✓/✗ trên màn chủ phòng khớp 100% với kết quả chấm
  của server, kể cả người không trả lời hoặc đã rời phòng.
- **SC-004**: Màn chủ phòng với 50 người chơi hiển thị đủ 50 bàn ở bề rộng 1366px, không cuộn
  ngang, thao tác chuyển câu không bị khựng thấy rõ (không re-render toàn bộ 50 SVG mỗi lần
  đổi trạng thái).
- **SC-005**: Bục vinh danh hiển thị đúng ở mọi tổ hợp: 1 người, 2 người, ≥3 người, hai người
  cùng điểm nhưng khác `rank` (có unit test cho hàm xếp bục thuần).
- **SC-006**: Bật `prefers-reduced-motion` thì không còn chuyển động nào trong màn Finished và
  trong huy hiệu/giơ tay.
- **SC-007**: Chủ phòng tắt tab rồi mở lại giữa câu: trạng thái giơ tay khôi phục đúng 100%
  theo server.

## Assumptions

- Hình "giơ tay" và huy hiệu ✓/✗ là đồ họa phủ ngoài (CSS/SVG tự vẽ trong repo), không phải
  thuộc tính DiceBear — vì quyết định 4 cấm đổi thuộc tính nhân vật.
- Bục vinh danh dùng dữ liệu bảng xếp hạng cuối đã có (`GameEnded.finalLeaderboard`, và
  `RoomState.leaderboard` khi phòng Finished) — không cần payload backend mới cho bục.
- **Đã chốt (cố vấn)**: chủ phòng kết nối lại trong 6 giây hiển thị kết quả của một câu (đã
  ended, chưa sang câu mới) thấy lưới KHÔNG có huy hiệu ✓/✗ (RoomState không chở
  `CorrectPlayerIds` của sự kiện đã lỡ); chấp nhận vì trạng thái chỉ sống 6 giây.
- "Màn máy chiếu" không phải breakpoint mới: máy chiếu nhận cùng bố cục ≥1366px.
- Chỗ ngồi theo thứ tự vào phòng (FR-022); không có cơ chế chọn chỗ.
- `RankBonus` tính kiểu competition ranking (`RewardCalculator`: 1 + số người điểm cao hơn →
  1-1-3), khác `Rank` của bảng xếp hạng (luôn duy nhất, có tie-break theo thời gian). Hệ quả đã
  biết: hai người cùng điểm đứng bậc 1 và bậc 2 trên bục vẫn đều có thể nhận thưởng hạng 1.
  UI không hiển thị hạng thưởng nên bục không mâu thuẫn trực tiếp; không xử lý trong tính năng này.
- Người rời phòng/rớt mạng không bị xóa khỏi trận (chỉ phát `PlayerLeft`): họ vẫn là một bàn trong
  lưới và vẫn được tính ✓/✗, điểm, xếp hạng.
