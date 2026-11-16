# Contract: Payload SignalR mở rộng

Tên sự kiện GIỮ NGUYÊN (`src/realtime/events.ts` và `GameHubEvents.cs` không đổi danh sách).
Chỉ payload 3 sự kiện/luồng sau được thêm trường — thêm trường là thay đổi tương thích ngược
(client cũ bỏ qua trường lạ).

## AnswerProgress (Server → Client, CHỈ nhóm `{code}:host`)

```jsonc
{
  "answeredCount": 3,
  "totalPlayers": 10,
  "answeredPlayerIds": ["guid-a", "guid-b", "guid-c"]   // MỚI: id đã trả lời câu hiện tại
}
```

Ràng buộc: không bao giờ phát tới nhóm phòng chung. `answeredPlayerIds.length == answeredCount`.

## QuestionEnded (Server → Client, cả phòng)

```jsonc
{
  "questionId": "guid",
  "correctIndex": 2,
  "answeredCount": 8,
  "totalPlayers": 10,
  "endedAt": "2026-11-16T07:00:00Z",
  "nextQuestionAt": "2026-11-16T07:00:06Z",
  "correctPlayerIds": ["guid-a", "guid-d"]               // MỚI: id trả lời đúng câu này
}
```

Ràng buộc: chỉ chứa người có answer đúng (người không trả lời / trả lời sai / điểm 0 không có
mặt). Có thể chứa id người đã rời phòng (answer vẫn trong DB) — client chỉ vẽ người còn trong
danh sách phòng.

## RoomState (Server → Client, gửi riêng người vừa JoinRoom)

```jsonc
{
  // ... các trường hiện có giữ nguyên ...
  "answeredPlayerIds": ["guid-a", "guid-b"]              // MỚI: xem bảng dưới
}
```

| Người nhận | Trạng thái phòng | Giá trị |
|---|---|---|
| Chủ phòng | InProgress, câu hiện tại chưa ended | Danh sách id đã trả lời câu hiện tại |
| Chủ phòng | InProgress, câu đã ended (6s kết quả) | `null` (KHÔNG khôi phục ✓/✗ — xem Assumption spec) |
| Chủ phòng | Lobby / Finished | `null` |
| Người chơi | Mọi trạng thái | `null` — bắt buộc, có test chống rò |
