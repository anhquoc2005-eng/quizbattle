# Data Model: Màn chơi kiểu lớp học + bục vinh danh

**Không thay đổi schema database, không migration.** Mọi dữ liệu mới suy ra từ bảng `Answers`
đã có (`PlayerId`, `QuestionId`, `IsCorrect`) qua snapshot sẵn trong GameService.

## DTO backend mở rộng (QuizBattle.Application/Game)

| DTO | Trường mới | Kiểu | Điền khi nào | Ai nhận |
|---|---|---|---|---|
| `AnswerProgressDto` | `AnsweredPlayerIds` | `IReadOnlyList<Guid>` | Mỗi lần có người trả lời: id các answer đã commit của câu hiện tại mà giao dịch thấy + người vừa gửi (có thể thiếu người nộp đồng thời — client gộp hợp, R7) | Chỉ nhóm `{code}:host` |
| `QuestionEndedDto` | `CorrectPlayerIds` | `IReadOnlyList<Guid>` | Khi câu kết thúc: id người có answer `IsCorrect` cho câu đó | Cả phòng |
| `RoomStateDto` | `AnsweredPlayerIds` | `IReadOnlyList<Guid>?` | Chỉ khi `IsHost` && phòng InProgress && câu hiện tại chưa ended; mọi trường hợp khác `null` | Người vừa JoinRoom |

Bất biến:
- Payload tới client người chơi KHÔNG BAO GIỜ chứa danh sách đã trả lời trước khi câu kết thúc
  (FR-007, SC-002).
- `CorrectPlayerIds` ⊆ id mọi `Player` của phòng. Người rời phòng/rớt mạng không bị xóa nên vẫn
  nằm trong `players` và vẫn có desk (ngoài ra, id không có desk vô hại, desk chỉ tra theo id).
- `RoomStateDto.Players` sắp theo `JoinedAt` rồi `Id` (do `BuildRoomState` sắp, FR-022); chỉ đổi
  thứ tự, không đổi kiểu.
- Đường `SubmitAnswerSnapshot` đổi `int AnsweredCountForCurrentQuestion` thành
  `IReadOnlyList<Guid> AnsweredPlayerIds` (số đếm = `.Count`) — không đổi `GameSnapshot`.

## Kiểu frontend khớp (frontend/src/types/game.ts)

- `AnswerProgress` + `answeredPlayerIds: string[]`
- `QuestionEnded` + `correctPlayerIds: string[]`
- `RoomState` + `answeredPlayerIds: string[] | null`

## State client (frontend/src/realtime/gameReducer.ts)

| Trường GameState | Kiểu | Chuyển trạng thái |
|---|---|---|
| `answeredPlayerIds` | `string[]` | `ANSWER_PROGRESS`: bỏ qua nếu câu đã ended (R3), ngược lại GỘP HỢP với danh sách hiện có (chỉ thêm, không ghi đè — R7); `ROOM_STATE`: thay thế bằng `payload ?? []`; `QUESTION_STARTED`: reset `[]` |
| `correctPlayerIds` | `string[] \| null` | `QUESTION_ENDED` ghi; `QUESTION_STARTED`/`ROOM_STATE`/`GAME_ENDED` reset `null` |

Trạng thái một desk (suy diễn thuần trong Classroom, không lưu riêng):

```
ended = false:  answeredPlayerIds chứa player.id ? "giơ tay" : "thường"
ended = true :  correctPlayerIds chứa player.id  ? "✓"       : "✗"
                (correctPlayerIds null — ví dụ host reconnect trong 6s kết quả → "thường")
```

## Bậc vinh danh (frontend/src/components/podiumSteps.ts — hàm thuần)

```
podiumSteps(entries: LeaderboardEntry[]): { rank: number; entry: LeaderboardEntry }[]
```

- `rank` của bảng xếp hạng luôn duy nhất (GameService.cs:383-385: `Rank = i + 1`) → mỗi bậc đúng
  MỘT người. Hàm lọc `rank <= 3` rồi sắp tăng dần theo `rank`; không nhóm, không tính lại.
- Hai người cùng điểm nhưng `rank` khác (1 và 2) → hai bậc khác nhau, đúng thứ tự `rank`.
- 1–2 người: trả đúng số bậc có người; rỗng → `[]`. Thứ tự trả về là 1,2,3 — `Podium` tự đảo
  thành 3→2→1 khi dàn animation.
- `rank` ≠ `RewardResult.RewardRank` (competition ranking 1-1-3); bục luôn dùng `rank` bảng xếp hạng.
- Không entity mới, không gọi mạng — đủ điều kiện Vitest thuần.
