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
    { "roomCode": "ABC123", "topic": "Lịch sử", "finishedAt": "…", "questionCount": 10, "role": "player" }
  ]
}
```
`role`: `"player" | "host"`. `rooms` rỗng = trạng thái rỗng. Mới nhất trước.

## POST /api/practice/sessions
Body `{ "roomCode": "ABC123" }` → **201** `{ "sessionId": "…", "total": 10 }` + `Location`.
- 404 `not_found`: phòng không tồn tại HOẶC user không đủ điều kiện (cùng một phản hồi, không lộ phòng của người khác).
- 400: `roomCode` sai định dạng.

## GET /api/practice/sessions/{id}
**200** — `PracticeSessionDto`
```json
{
  "sessionId": "…", "topic": "Lịch sử", "total": 10, "answeredCount": 3, "completed": false,
  "current": { "questionId": "…", "index": 3, "text": "…", "options": ["A","B","C","D"] },
  "summary": null
}
```
- `current` null khi `completed`. KHÔNG có `correctIndex`.
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
`xpReason` khi `xpAwarded = 0`: `"wrong" | "already_today" | "daily_cap"`; khi có XP: `null`.
Khi cộng một phần (còn lại < `xpPerCorrect`): `xpAwarded` = phần còn lại, `xpReason = "daily_cap_partial"`.

Lỗi:
- 400: `selectedIndex` ngoài 0..3; `questionId` thiếu.
- 404: lượt không tồn tại/không thuộc user.
- 409 `already_answered`: câu này đã chấm trong lượt (kể cả hai request đồng thời).
- 409 `not_current_question`: `questionId` không phải câu hiện tại của lượt.
