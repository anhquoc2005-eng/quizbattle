# Data Model: Màn chơi kiểu lớp học + bục vinh danh

**Không thay đổi schema database, không migration.** Mọi dữ liệu mới suy ra từ bảng `Answers`
đã có (`PlayerId`, `QuestionId`, `IsCorrect`) qua snapshot sẵn trong GameService.

## DTO backend mở rộng (QuizBattle.Application/Game)

| DTO | Trường mới | Kiểu | Điền khi nào | Ai nhận |
|---|---|---|---|---|
| `AnswerProgressDto` | `AnsweredPlayerIds` | `IReadOnlyList<Guid>` | Mỗi lần có người trả lời: id mọi người đã trả lời câu hiện tại (gồm người vừa gửi) | Chỉ nhóm `{code}:host` |
| `QuestionEndedDto` | `CorrectPlayerIds` | `IReadOnlyList<Guid>` | Khi câu kết thúc: id người có answer `IsCorrect` cho câu đó | Cả phòng |
| `RoomStateDto` | `AnsweredPlayerIds` | `IReadOnlyList<Guid>?` | Chỉ khi `IsHost` && phòng InProgress && câu hiện tại chưa ended; mọi trường hợp khác `null` | Người vừa JoinRoom |

Bất biến:
- Payload tới client người chơi KHÔNG BAO GIỜ chứa danh sách đã trả lời trước khi câu kết thúc
  (FR-007, SC-002).
- `CorrectPlayerIds` ⊆ id người chơi còn trong phòng ∪ người đã rời (answer còn trong DB);
  desk chỉ vẽ người còn trong `players` nên id thừa vô hại.

## Kiểu frontend khớp (frontend/src/types/game.ts)

- `AnswerProgress` + `answeredPlayerIds: string[]`
- `QuestionEnded` + `correctPlayerIds: string[]`
- `RoomState` + `answeredPlayerIds: string[] | null`

## State client (frontend/src/realtime/gameReducer.ts)

| Trường GameState | Kiểu | Chuyển trạng thái |
|---|---|---|
| `answeredPlayerIds` | `string[]` | `ANSWER_PROGRESS` (bỏ qua nếu câu đã ended — R3) và `ROOM_STATE` ghi; `QUESTION_STARTED` reset `[]` |
| `correctPlayerIds` | `string[] \| null` | `QUESTION_ENDED` ghi; `QUESTION_STARTED`/`ROOM_STATE`/`GAME_ENDED` reset `null` |

Trạng thái một desk (suy diễn thuần trong Classroom, không lưu riêng):

```
ended = false:  answeredPlayerIds chứa player.id ? "giơ tay" : "thường"
ended = true :  correctPlayerIds chứa player.id  ? "✓"       : "✗"
                (correctPlayerIds null — ví dụ host reconnect trong 6s kết quả → "thường")
```

## Bậc vinh danh (frontend/src/components/podiumSteps.ts — hàm thuần)

```
podiumSteps(entries: LeaderboardEntry[]): { rank: number; entries: LeaderboardEntry[] }[]
```

- Nhóm theo `rank` server trả, lấy các nhóm có `rank` ≤ hạng của 3 người đứng đầu danh sách.
- Đồng hạng: cả nhóm chung một bậc; dãy bậc là dãy `rank` thực tế (hai rank 1 → bậc 1, bậc 3).
- 1–2 người: trả đúng số bậc có người; rỗng → `[]`.
- Không entity mới, không gọi mạng — đủ điều kiện Vitest thuần.
