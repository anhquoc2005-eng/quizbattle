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

Ràng buộc:
- Không bao giờ phát tới nhóm phòng chung.
- Danh sách là các answer ĐÃ COMMIT mà giao dịch của người gửi nhìn thấy + chính người gửi. Hai
  người nộp đồng thời có thể mỗi payload thiếu người kia (phòng chỉ khóa `FOR SHARE`), nên server
  KHÔNG đảm bảo `answeredPlayerIds.length == answeredCount`, và client KHÔNG được dựa vào đó.
- Client phải gộp HỢP các danh sách trong cùng một câu (chỉ thêm), không ghi đè (research R7).

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

Ràng buộc: chỉ chứa người có answer đúng (người không trả lời / trả lời sai không có mặt).
Người rời phòng hoặc rớt mạng KHÔNG bị xóa khỏi trận (`LeaveRoom`/`OnDisconnectedAsync` chỉ phát
`PlayerLeft`), nên nếu họ đã trả lời đúng thì vẫn có mặt trong danh sách và vẫn nhận ✓.

## RoomState (Server → Client, gửi riêng người vừa JoinRoom)

```jsonc
{
  // ... các trường hiện có giữ nguyên ...
  "players": [ /* ... */ ],                              // ĐỔI: sắp theo JoinedAt rồi Id (ổn định)
  "answeredPlayerIds": ["guid-a", "guid-b"]              // MỚI: xem bảng dưới
}
```

`players` sắp theo `JoinedAt` tăng dần, hòa thì theo `Id` tăng dần, do `BuildRoomState` sắp (một
chỗ duy nhất dựng RoomState; không phụ thuộc thứ tự truy vấn DB). Chỉ đổi THỨ TỰ, không đổi kiểu
hay trường. Client coi `ROOM_STATE` là thứ tự gốc, `PLAYER_JOINED` nối cuối.

| Người nhận | Trạng thái phòng | Giá trị |
|---|---|---|
| Chủ phòng | InProgress, câu hiện tại chưa ended | Danh sách id đã trả lời câu hiện tại |
| Chủ phòng | InProgress, câu đã ended (6s kết quả) | `null` (KHÔNG khôi phục ✓/✗ — xem Assumption spec) |
| Chủ phòng | Lobby / Finished | `null` |
| Người chơi | Mọi trạng thái | `null` — bắt buộc, có test chống rò |
