# Quickstart: Giới hạn AI theo ngày

Dùng để nghiệm thu (T032) sau khi hoàn tất 4 đợt.

## Chuẩn bị

```bash
docker compose up -d                                   # Postgres dev
export QUIZBATTLE_TEST_DB="<chuỗi kết nối tới server Postgres dev, có quyền CREATE DATABASE>"
cd backend && dotnet build && dotnet test              # gồm test tích hợp Postgres thật
cd ../frontend && npm test && npm run build
```

Thiếu `QUIZBATTLE_TEST_DB` thì test tích hợp tự bỏ qua — nghiệm thu bắt buộc phải chạy CÓ biến này.

## Cấu hình thử nhanh

Hạ trần để thử bằng tay (không cần sinh 20 lần thật):

```
Ai__DailyLimitPerUser=3
Ai__DailyLimitGlobal=5
```

Khởi động lại backend sau khi đổi. Giá trị `0`, `-1`, `abc` phải khiến backend không khởi động và báo lỗi rõ.

## Kịch bản tay (backend + frontend chạy thật)

1. Đăng nhập chủ phòng A, tạo phòng Draft, mở trang phòng → thấy "Còn 3/3 lượt AI hôm nay".
2. Bấm "Sinh câu hỏi" → xong thì hiện "Còn 2/3". (Một request = một lượt dù Gemini bị gọi 1 hay 2 lần.)
3. Sinh tới hết lượt → nút "Sinh câu hỏi" bị tắt, có thông báo "đã dùng hết lượt hôm nay, làm mới lúc 00:00 (giờ Việt Nam)". Thêm câu hỏi nhập tay vẫn dùng được.
4. Gọi thẳng `POST /api/rooms/{code}/questions/generate` khi hết lượt → `429`, body có `code: "AI_DAILY_LIMIT_USER"` và `resetAt` dạng `...T00:00:00+07:00`.
5. Đăng nhập chủ phòng B, sinh hai lượt (tổng hệ thống ≥ 5) → khi global hết: `429` `AI_DAILY_LIMIT_GLOBAL`; trang phòng báo "hệ thống AI hôm nay đã hết lượt" (khác thông báo cá nhân).
6. Dồn 11 request trong 1 giờ (với trần ngày đủ lớn) → request thứ 11 là `429` `AI_RATE_LIMIT` (policy 10/giờ còn nguyên).
7. Ngắt Gemini (đặt `Ai__ApiKey` sai) → generate trả 502/503, số lượt của user KHÔNG giảm (đã hoàn); lượt global vẫn giữ.
8. Kiểm tra DB: `SELECT * FROM "AiUsageDaily" ORDER BY "Day", "Scope";` — dòng `global` ≥ tổng các dòng `user:*`.
9. Phòng không phải Draft / không phải chủ phòng / vượt 50 câu → lỗi 404/409 và số lượt KHÔNG đổi.

## Kiểm tra migration (T031)

```bash
cd backend
dotnet ef migrations script --idempotent -p QuizBattle.Infrastructure -s QuizBattle.Api -o ../specs/004-ai-daily-limit/migration.idempotent.sql
psql "<dev>" -f ../specs/004-ai-daily-limit/migration.idempotent.sql   # lần 1
psql "<dev>" -f ../specs/004-ai-daily-limit/migration.idempotent.sql   # lần 2: không lỗi
```

Chạy file này lên Supabase bằng chuỗi **Session pooler (cổng 5432)** TRƯỚC khi merge vào `main`.

## Bằng chứng mutation (T013 điền khi làm)

- Xóa `WHERE "AiUsageDaily"."Count" < @limit` → `Reserve_ParallelCallsOverUserLimit_GrantsExactlyLimit`: ĐỎ (Granted = ___ thay vì 5); `Reserve_ManyUsersOverGlobalLimit_GrantsExactlyGlobalLimit`: ĐỎ (Granted = ___ thay vì 5).
- Khôi phục câu SQL → cả hai XANH.

## Checklist nghiệm thu

- [ ] `dotnet test` xanh có `QUIZBATTLE_TEST_DB`; `npm test` + `npm run build` xanh
- [ ] Mutation: bỏ `WHERE` thì T008/T009 đỏ (đã ghi số liệu ở trên)
- [ ] Kịch bản tay 1–9 đạt
- [ ] SQL idempotent chạy hai lần không lỗi; đã chạy lên Supabase trước merge
- [ ] `.env.example` có hai biến mới; không có secret mới
- [ ] Bảng đối chiếu quyết định → task ở cuối tasks.md đủ Q1–Q7, A–E
