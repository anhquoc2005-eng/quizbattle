# Contract: SignalR (`GameHub`, `/hubs/game`)

Danh sách tên sự kiện **không đổi** (CLAUDE.md). Chỉ thay đổi payload và thêm một mã lỗi. Cập nhật đồng thời `GameHubEvents.cs`, `src/realtime/events.ts` (nếu đổi tên: không có) và `src/types/game.ts`.

## Client → Server

Không đổi tên hay tham số. Thay đổi hành vi:

- `JoinRoom(code, sessionToken)`: sau khi thành công, Hub lưu thêm hash phiên vào `Context.Items` và cập nhật `PlayerConnectionRegistry`. Nếu Player đã có kết nối khác với hash khác, kết nối cũ nhận `Error session_replaced` và bị gỡ khỏi group.
- `SubmitAnswer(questionId, selectedIndex)`: nếu hash phiên của kết nối không khớp `Player.SessionTokenHash` trong DB thì từ chối bằng `Error { method: "SubmitAnswer", code: "session_replaced" }`.
- `LeaveRoom()`: bỏ qua (không phát `PlayerLeft`) nếu hash phiên không khớp.

## Server → Client: payload thay đổi

Thêm trường, không bỏ trường nào (tương thích ngược).

### `PlayerSummaryDto` / `PlayerJoinedDto` / `LeaderboardEntryDto`

Thêm `level: number | null` (`null` = khách). Hiển thị cạnh tên ở sảnh chờ và bảng điểm.

### `GameEnded`

```json
{
  "finalLeaderboard": [ { "playerId": "...", "displayName": "...", "score": 2400, "rank": 1, "level": 3 } ],
  "rewards": [ "PlayerRewardDto" ]
}
```

### `RoomState`

Thêm `rewards: PlayerRewardDto[] | null`: có giá trị khi `status = "Finished"` (đọc từ `MatchRewards`, không tính lại), `null` ở trạng thái khác. Nhờ đó người rớt mạng đúng lúc kết thúc vẫn thấy khối thưởng khi `JoinRoom` lại.

### `PlayerRewardDto` (mới)

Có một phần tử cho **mỗi** người chơi trong phòng (kể cả khách và chủ phòng tự chơi), để client chọn theo `playerId` của mình mà không phải suy ra lý do.

```json
{
  "playerId": "uuid",
  "awarded": true,
  "notAwardedReason": null,
  "rewardRank": 2,
  "score": 2400,
  "xp": 240,
  "coins": 44,
  "rankBonus": 20,
  "noRankBonusReason": null,
  "totalXp": 840,
  "levelBefore": 3,
  "levelAfter": 3,
  "leveledUp": false
}
```

| Trường | Ý nghĩa |
|---|---|
| `awarded` | `true` khi đã cộng XP/Xu (kể cả khi bằng 0) |
| `notAwardedReason` | `null` khi `awarded`; ngược lại `"guest"` / `"host"` / `"no_answers"` / `"not_enough_accounts"` |
| `rewardRank` | hạng theo điểm (có thể khác `rank` trên bảng điểm khi hòa, xem research R3) |
| `noRankBonusReason` | `"zero_score"` khi `awarded` và `score = 0` mà hạng ≤ 3 (FR-013: điểm 0 nên không có thưởng hạng); ngược lại `null` |
| `totalXp`, `levelBefore`, `levelAfter`, `leveledUp` | `null`/bỏ khi không `awarded` hoặc là khách |
| `coins` / `rankBonus` | Xu phần điểm / Xu thưởng hạng; tổng nhận = `coins + rankBonus` |

Với khách: `awarded=false`, `notAwardedReason="guest"`, các số bằng 0.

## Server → Client: `Error`

`GameErrorDto { method, code, message }` — thêm mã **`session_replaced`**:

| `code` | Khi nào | Client xử lý |
|---|---|---|
| `unauthorized` / `not_found` / `conflict` / `unknown` | như hiện nay | không đổi |
| `session_replaced` (mới) | tài khoản đã vào lại phòng từ thiết bị khác | hiện thông báo "Bạn đã vào phòng từ thiết bị khác", xóa phiên đã lưu, dừng tự kết nối lại, quay về `/join` |

`message` hiển thị cho người dùng. Cần cập nhật danh sách mã trong CLAUDE.md và `GameError['code']` ở `src/types/game.ts`.
