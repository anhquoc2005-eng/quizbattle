# Contract: REST API

Mọi lỗi theo ProblemDetails `{ type, title, status, detail?, errors? }`, **thêm trường mở rộng `code`** (chuỗi) khi client cần phân biệt (xem 401 bên dưới). Các DTO TypeScript ở `frontend/src/types` phải khớp.

## Thay đổi: `POST /api/rooms/{code}/players` (vào phòng)

`[AllowAnonymous]`, rate limit `join-room` (giữ nguyên). Header `Authorization: Bearer <jwt>` là **tùy chọn**: client luôn gắn khi đã đăng nhập (`request()` hiện làm vậy).

Request (không đổi): `{ "displayName": "..." }` (1-50 ký tự). Với tài khoản đã có Player trong phòng, `displayName` bị bỏ qua.

| Tình huống | Kết quả |
|---|---|
| Không có header `Authorization` | khách: hành vi như hiện nay, `201` |
| Token hợp lệ, chưa có Player trong phòng, phòng `Lobby` | tạo Player gắn `UserId`, `201` |
| Token hợp lệ, đã có Player trong phòng, phòng `Lobby` hoặc `InProgress` | dùng lại Player, cấp `sessionToken` mới (token cũ vô hiệu), `201` |
| Token hợp lệ, chưa có Player, phòng khác `Lobby` | `409` "Phòng chưa mở hoặc đã bắt đầu, không thể vào." |
| Tên trùng (lần đầu vào) | `409` "Tên hiển thị đã có người dùng trong phòng." |
| Có header nhưng token **hết hạn** | `401`, `code: "token_expired"` |
| Có header nhưng token sai chữ ký/định dạng, hoặc User không còn | `401`, `code: "token_invalid"` |
| Phòng không tồn tại | `404` |

Response `201` (không đổi): `{ "playerId", "sessionToken", "roomCode" }`.

Ví dụ `401`:

```json
{ "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2", "title": "Phiên đăng nhập đã hết hạn.", "status": 401, "code": "token_expired" }
```

Frontend: `request()` vẫn xóa token đã lưu khi gặp 401 có token (đúng, vì token đã chết); `JoinPage` đọc `problem.code` để hiện lựa chọn "Đăng nhập lại" / "Vào làm khách". Không bao giờ tự gửi lại như khách.

## Mới: `GET /api/users/me`

`[Authorize]`. `200`:

```json
{
  "userId": "uuid",
  "displayName": "Alice",
  "level": 3,
  "totalXp": 640,
  "xpIntoLevel": 40,
  "xpForNextLevel": 600,
  "coinBalance": 125
}
```

`xpForNextLevel` = XP cần để đi từ đầu cấp hiện tại lên cấp kế (= `200 × level`); thanh tiến trình = `xpIntoLevel / xpForNextLevel`. Server tính, client không lặp công thức. `401` khi chưa đăng nhập/hết hạn.

## Mới: `GET /api/users/me/rewards?limit=20`

`[Authorize]`. `limit` 1-50, mặc định 20 (ngoài khoảng: `400` với `errors.limit`). `200` mảng, mới nhất trước, chỉ các trận `Awarded`:

```json
[
  {
    "roomCode": "ABC123",
    "topic": "Lịch sử Việt Nam",
    "playedAt": "2026-11-09T10:15:00Z",
    "score": 2400,
    "rewardRank": 2,
    "xp": 240,
    "coins": 44,
    "rankBonus": 20
  }
]
```

Mảng rỗng là trạng thái rỗng hợp lệ (người dùng chưa có trận được thưởng). `coins` là phần điểm; tổng Xu của trận = `coins + rankBonus`.

## Mới: `GET /api/leaderboards/xp`

`[AllowAnonymous]`. Top 50 theo `TotalXp` giảm dần (hòa: tài khoản tạo sớm hơn trước), không gồm tài khoản 0 XP. `200`:

```json
[
  { "rank": 1, "displayName": "Alice", "level": 7, "totalXp": 4520 }
]
```

Không trả email hay Id người dùng. `rank` ở đây là số thứ tự trong danh sách (1..N). Mảng rỗng khi chưa ai có XP.

## Không thay đổi

`POST /api/rooms/{code}/open`, các endpoint Auth, Questions, Rooms, Health.
