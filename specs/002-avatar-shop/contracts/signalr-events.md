# Contract: SignalR (`GameHub`, `/hubs/game`) - nhân vật

Danh sách tên sự kiện **không đổi** (CLAUDE.md). Chỉ thêm trường `avatar` vào payload hiện có; **không** thêm sự kiện, **không** gửi ảnh (chỉ cặp ô → `variantKey`). Cập nhật `src/types/game.ts`, `src/realtime/gameReducer.ts` và DTO backend tương ứng.

## Client → Server

Không đổi.

## Server → Client: payload thay đổi (thêm trường, không bỏ trường nào)

`AvatarDto` có dạng giống [rest-api.md](rest-api.md) (11 ô; sáu ô bắt buộc luôn có giá trị, năm ô tùy chọn có thể `null`).

| DTO | Thêm | Ghi chú |
|---|---|---|
| `PlayerSummaryDto` | `avatar: AvatarDto` | Trong `RoomState.players` |
| `LeaderboardEntryDto` | `avatar: AvatarDto` | Trong `RoomState.leaderboard`, `LeaderboardUpdated`, `GameEnded.finalLeaderboard` |
| `PlayerJoinedDto` | `avatar: AvatarDto` | Sự kiện `PlayerJoined` |

`PlayerLeftDto`, `PlayerRewardDto`, `QuestionStarted`, `QuestionEnded`, `AnswerProgress`, `Error` không đổi. Người chơi khách và tài khoản chưa lưu nhân vật cũng **luôn** có `avatar` đủ ô (do server tính bằng `DefaultAvatarGenerator`), nên frontend không có nhánh "thiếu nhân vật" ngoài việc chờ dữ liệu.

## Nguồn dữ liệu và tính "hiện tại"

- Mỗi lần dựng snapshot phòng, repository nạp nhân vật đã lưu của **mọi** người chơi có tài khoản bằng **một** truy vấn (`UserAvatars WHERE UserId = ANY(...)`), cùng chỗ đã nạp `Users` để tính `level`. Không có truy vấn theo từng người (kiểm bằng test đếm lệnh SQL).
- Nhân vật không lưu trong `Players` (không chụp lại lúc vào phòng). Đổi đồ giữa trận không ảnh hưởng điểm; người khác thấy nhân vật mới ở sự kiện kế tiếp được dựng (bảng điểm câu sau, `RoomState` khi tải lại). Không có sự kiện đẩy riêng khi đổi đồ.

## Kích thước payload (ước tính, phòng 50 người)

Một `AvatarDto` JSON có đủ khóa và `null` ≈ **210 byte** (đo bằng `JSON.stringify` với 4 ô tùy chọn trống; nhân vật đầy đồ nhỉnh hơn một chút).

| Gói tin | Số `AvatarDto` | Tăng thêm | Ghi chú |
|---|---|---|---|
| `RoomState` (lúc vào/tải lại) | 100 (50 `players` + 50 `leaderboard`) | ≈ 21 KB | gửi riêng cho người vừa vào |
| `LeaderboardUpdated` (mỗi câu) | 50 | ≈ 10,5 KB | phát cho cả phòng, mỗi câu một lần |
| `GameEnded` | 50 | ≈ 10,5 KB | một lần cuối trận |
| `PlayerJoined` | 1 | ≈ 0,2 KB | cho 49 người đang ở sảnh |

Mỗi kết nối nhận thêm ≈ 10,5 KB mỗi câu (`LeaderboardUpdated` phát cho từng người). Cả trận 10 câu, 50 người: mỗi kết nối thêm ≈ 10 × 10,5 + 10,5 + 21 ≈ 137 KB; toàn phòng (50 kết nối) ≈ 6,8 MB gửi đi. Chấp nhận được cho một trận kéo dài vài phút (phần cộng thêm so với payload hiện tại khoảng gấp ba mỗi bảng điểm, vì một dòng bảng điểm hiện ≈ 110 byte). Nếu cần giảm: gửi bản đồ `avatars` theo `playerId` một lần cho cả `players` và `leaderboard` (giảm một nửa `RoomState`), hoặc bỏ khóa `null` bằng `WhenWritingNull`; **chưa làm** (quyết định đơn giản hơn, đo lại ở Đợt C).

## Frontend

- `gameReducer` lưu `avatar` đúng như nhận; `Avatar` (component dùng chung) vẽ từ DTO, không tự sinh.
- Một tài khoản đổi đồ trong lúc đang chơi: nhân vật của người đó trên màn người khác đổi ở lần cập nhật bảng điểm kế tiếp.
