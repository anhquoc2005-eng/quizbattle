# Contract: AI daily quota (HTTP)

**Spec**: [../spec.md](../spec.md) · Định dạng lỗi chung: ProblemDetails `{ type, title, status, detail?, errors? }` + trường mở rộng `code`.

DTO backend và type frontend (`src/types/aiQuota.ts`) khớp tên trường (camelCase khi serialize).

## GET /api/ai-quota

Yêu cầu JWT chủ phòng (`[Authorize]`). Theo user, không theo phòng. Chỉ đọc.

**200 OK** — `AiQuotaResponse`

```json
{
  "dailyLimit": 20,
  "used": 3,
  "remaining": 17,
  "globalExhausted": false,
  "resetAt": "2026-11-20T00:00:00+07:00"
}
```

| Trường | Kiểu | Ghi chú |
|---|---|---|
| `dailyLimit` | int | `Ai:DailyLimitPerUser` |
| `used` | int | Số dòng `user:{id}` của ngày VN hiện tại, 0 nếu chưa có dòng |
| `remaining` | int | `max(dailyLimit - used, 0)` |
| `globalExhausted` | bool | `Count(global) >= Ai:DailyLimitGlobal`. KHÔNG trả con số global |
| `resetAt` | string (ISO 8601, offset `+07:00`) | 00:00 VN của ngày kế tiếp |

**401** — chưa đăng nhập (như mọi endpoint `[Authorize]`).

## POST /api/rooms/{code}/questions/generate (thay đổi)

Request/201 response giữ nguyên. Thêm các phản hồi sau (đều ProblemDetails):

### 429 — hết lượt theo ngày

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.20",
  "title": "Bạn đã dùng hết 20 lượt AI hôm nay.",
  "status": 429,
  "code": "AI_DAILY_LIMIT_USER",
  "resetAt": "2026-11-20T00:00:00+07:00"
}
```

| `code` | Khi nào | Title gợi ý |
|---|---|---|
| `AI_DAILY_LIMIT_USER` | Dòng user đã chạm trần (và global còn chỗ) | "Bạn đã dùng hết lượt AI hôm nay." |
| `AI_DAILY_LIMIT_GLOBAL` | Dòng global đã chạm trần (ưu tiên khi cả hai cùng hết) | "Hệ thống AI hôm nay đã hết lượt." |

`resetAt` luôn có trong hai trường hợp này.

### 429 — dồn dập (policy 10/giờ, giữ nguyên)

```json
{ "type": "...#section-15.5.20", "title": "Quá nhiều yêu cầu, vui lòng thử lại sau.", "status": 429, "code": "AI_RATE_LIMIT" }
```

Không có `resetAt`. `code` này chỉ gắn khi endpoint dùng policy `generate-questions`.

### 502 / 503 — Gemini lỗi (đã có)

Không đổi định dạng. Khác trước: lượt của user đã được hoàn trước khi trả lỗi (client nên gọi lại `GET /api/ai-quota` hoặc invalidate query).

### Các lỗi đã có, KHÔNG làm mất lượt

`400` (validation), `404` (phòng không tồn tại / không phải chủ), `409` (phòng không Draft / vượt `MAX_QUESTIONS_PER_ROOM`): trả TRƯỚC khi giữ chỗ.

## Thứ tự xử lý (để client suy luận mã lỗi)

1. Rate limiter 10/giờ (middleware) → `429 AI_RATE_LIMIT`
2. Authorize → 401
3. Model validation → 400
4. Phòng tồn tại + đúng chủ + Draft + còn chỗ → 404/409
5. **Giữ chỗ lượt** → `429 AI_DAILY_LIMIT_GLOBAL` / `AI_DAILY_LIMIT_USER`
6. Gọi Gemini → 502/503 (hoàn lượt user)
7. Chèn câu hỏi → 409 có thể xảy ra (KHÔNG hoàn lượt)
8. 201

## Cấu hình

| Biến | Mặc định | Ghi chú |
|---|---|---|
| `Ai__DailyLimitPerUser` | 20 | Số nguyên > 0 |
| `Ai__DailyLimitGlobal` | 300 | Số nguyên > 0; nên đặt theo RPD của key Gemini |
