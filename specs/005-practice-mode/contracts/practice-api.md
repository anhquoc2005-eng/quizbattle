# Contract: Practice (HTTP)

Mọi endpoint yêu cầu JWT người dùng (`[Authorize]`), rate limit policy `practice` theo user. Lỗi theo ProblemDetails chung `{ type, title, status, detail?, errors? }` + `code` khi cần. DTO backend và `src/types/practice.ts` khớp tên (camelCase).

Không endpoint nào trả `correctIndex` của câu chưa nộp.

## GET /api/practice/rooms
**200** — `PracticeRoomsResponse`
```json
{
  "xpPerCorrect": 5,
  "dailyXpCap": 100,
  "xpGainedToday": 35,
  "remainingToday": 65,
  "resetAt": "2026-11-22T00:00:00+07:00",
  "rooms": [
    { "roomCode": "ABC123", "topic": "Lịch sử", "finishedAt": "…", "questionCount": 10, "role": "player", "openSessionId": null }
  ]
}
```
- `role`: `"player" | "host"`. `rooms` rỗng = trạng thái rỗng. Mới nhất trước.
- `openSessionId`: `sessionId` của lượt đang dở của phòng đó, hoặc null (để UI hiện "Tiếp tục" thay vì "Ôn").

## POST /api/practice/sessions
Body `{ "roomCode": "ABC123" }`.
- **201 Created** + `Location`: tạo lượt mới. Body `{ "sessionId": "…", "total": 10 }`.
- **200 OK**: đã có lượt đang mở của `(user, phòng)`; trả lại lượt đó, cùng `sessionId`, cùng dạng body. Không tạo mới.
- Hai request đồng thời: một trả 201, một trả 200, cùng `sessionId`.
- 404 `not_found`: phòng không tồn tại HOẶC user không đủ điều kiện (cùng một phản hồi, không lộ phòng của người khác).
- 400: `roomCode` sai định dạng.

## GET /api/practice/sessions/{id}
**200** — `PracticeSessionDto`
```json
{
  "sessionId": "…", "topic": "Lịch sử", "total": 10, "answeredCount": 3, "completed": false,
  "current": { "questionId": "…", "index": 3, "text": "…", "options": ["A","B","C","D"] },
  "lastResult": {
    "questionId": "…", "isCorrect": true, "correctIndex": 2,
    "xpAwarded": 5, "xpReason": null
  },
  "summary": null
}
```
- `current` null khi `completed`. KHÔNG có `correctIndex`.
- `lastResult`: kết quả câu nộp gần nhất của lượt (theo `AnsweredAt`), null nếu chưa nộp câu nào. Có `correctIndex` vì câu đã nộp (không lộ thêm gì). Dùng để F5 sau khi nộp vẫn thấy đáp án.
- Khi `completed`: `summary = { correctCount, xpGainedInSession, xpGainedToday, remainingToday, resetAt }`.
- 404 nếu lượt không tồn tại hoặc không thuộc user.

## POST /api/practice/sessions/{id}/answers
Body `{ "questionId": "…", "selectedIndex": 2 }`.

**200** — `PracticeAnswerResultDto`
```json
{
  "isCorrect": true, "correctIndex": 2,
  "xpAwarded": 5, "xpReason": null,
  "xpGainedToday": 40, "remainingToday": 60,
  "completed": false
}
```

### Idempotent
Nếu `(lượt, câu)` đã được chấm, response luôn là **200** với kết quả đã lưu của lần chấm đầu:
- `isCorrect`, `correctIndex`, `xpAwarded`, `xpReason` lấy từ dòng đã lưu, KHÔNG tính lại.
- `selectedIndex` mới (kể cả khác lần đầu) bị bỏ qua; không cộng XP; không ghi gì.
- `xpGainedToday`, `remainingToday`, `completed` phản ánh trạng thái HIỆN TẠI (không đóng băng): chúng là số tổng của ngày/lượt, không thuộc kết quả của câu đó.
- Hai request đồng thời cùng câu: cả hai 200, cùng kết quả, XP cộng 1 lần (khóa `Users` tuần tự hóa; request sau thấy dòng đã lưu).
- KHÔNG có 409 `already_answered`: phát lại luôn an toàn nên client được phép gửi lại khi mất kết nối.

### `xpReason`
`null` ⇔ nhận đủ `XpPerCorrect` (câu đúng). Ngược lại một trong:

| `xpReason` | Ý nghĩa | `isCorrect` | `xpAwarded` |
|---|---|---|---|
| `wrong` | Sai | false | 0 |
| `already_today` | Đã nhận XP cho câu này hôm nay | true | 0 |
| `daily_cap` | Hết trần ngày | true | 0 |
| `daily_cap_partial` | Còn lại < `XpPerCorrect`, cộng phần còn lại | true | phần còn lại (>0) |

### Lỗi
- 400: `selectedIndex` ngoài 0..3; `questionId` thiếu.
- 404: lượt không tồn tại/không thuộc user; hoặc `questionId` không thuộc lượt.
- 409 `not_current_question`: câu CHƯA chấm và không phải câu hiện tại của lượt. (Câu đã chấm thì không bao giờ 409: phát lại.)
- Khóa duy nhất của DB là lưới cuối; nếu từng bị vi phạm thì là lỗi hệ thống (500), không phải luồng thường (xem `research.md` R12).
