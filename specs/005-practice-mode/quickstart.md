# Quickstart: nghiệm thu thủ công (sau khi code xong)

Tài liệu này mô tả cách kiểm tra; ở đợt này CHƯA có code để chạy.

1. `docker compose up -d`, áp migration `AddPracticeMode` ở dev, chạy backend và frontend.
2. Tạo 3 tài khoản A, B, C. A làm chủ phòng, B và C vào chơi hết một trận có thưởng (>= 2 tài khoản không phải chủ phòng trả lời).
3. Đăng nhập B, mở `/practice`: thấy phòng vừa chơi, "hôm nay 0/100 XP". Đăng nhập một tài khoản không liên quan: danh sách rỗng.
4. Bắt đầu ôn: kiểm tra payload ở DevTools không có `correctIndex`. Nộp một câu đúng: thấy `+5 XP`, `xpGainedToday = 5`.
5. Ôn lại đúng câu đó ở lượt mới trong cùng ngày: `xpAwarded = 0`, `xpReason = already_today`.
6. Nộp cùng câu hai lần trong một lượt (gửi lặp bằng công cụ HTTP): lần hai 409.
7. Hồ sơ B: `TotalXp` tăng đúng bằng tổng XP ôn; `CoinBalance` không đổi.
8. Kiểm bất biến bằng SQL (chỉ đọc):
   `SELECT u."TotalXp", COALESCE((SELECT SUM("Xp") FROM "MatchRewards" WHERE "UserId" = u."Id"),0) + COALESCE((SELECT SUM("XpAwarded") FROM "PracticeAnswers" WHERE "UserId" = u."Id"),0) FROM "Users" u;` hai cột phải bằng nhau.
9. Chạm trần: đặt `Practice__DailyXpCap=10`, `Practice__XpPerCorrect=7` để thấy 7, rồi 3 (`daily_cap_partial`), rồi 0 (`daily_cap`).
