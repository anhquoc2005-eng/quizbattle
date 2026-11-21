# Quickstart: nghiệm thu thủ công (sau khi code xong)

Tài liệu này mô tả cách kiểm tra; ở đợt spec CHƯA có code để chạy.

1. `docker compose up -d`, áp migration `AddPracticeMode` ở dev, chạy backend và frontend.
2. Tạo 3 tài khoản A, B, C. A làm chủ phòng, B và C vào chơi hết một trận có thưởng (>= 2 tài khoản không phải chủ phòng trả lời).
3. Đăng nhập B, mở `/practice`: thấy phòng vừa chơi, "hôm nay 0/100 XP". Đăng nhập tài khoản không liên quan: danh sách rỗng.
4. Bấm "Ôn": kiểm tra payload ở DevTools không có `correctIndex`. Nộp một câu đúng: `+5 XP`, `xpGainedToday = 5`.
5. F5 ngay sau khi nộp: vẫn thấy đáp án câu vừa nộp (`lastResult`).
6. Gọi lại `POST /sessions` cùng phòng: 200 và cùng `sessionId` (không tạo lượt mới). Nút danh sách phòng hiện "Tiếp tục".
7. Nộp lại cùng câu (gửi lặp bằng công cụ HTTP), thử cả `selectedIndex` khác: luôn 200, kết quả lần đầu, XP không tăng.
8. Hoàn tất lượt, bấm "Ôn" lại: tạo lượt mới (201). Ôn lại cùng câu đã nhận XP hôm nay: `xpAwarded = 0`, `xpReason = already_today`.
9. Hồ sơ B: `TotalXp` tăng đúng bằng tổng XP ôn; `CoinBalance` không đổi.
10. Kiểm bất biến bằng SQL (chỉ đọc):
    `SELECT u."TotalXp", COALESCE((SELECT SUM("Xp") FROM "MatchRewards" WHERE "UserId" = u."Id"),0) + COALESCE((SELECT SUM("XpAwarded") FROM "PracticeAnswers" WHERE "UserId" = u."Id"),0) FROM "Users" u;` hai cột phải bằng nhau.
11. Chạm trần: đặt `Practice__DailyXpCap=10`, `Practice__XpPerCorrect=7` để thấy 7, rồi 3 (`daily_cap_partial`), rồi 0 (`daily_cap`).
12. Đặt `Practice__DailyXpCap=-1`: backend không khởi động và báo lỗi cấu hình rõ ràng.

## Bằng chứng mutation (điền khi làm task M1–M7)

| M | Test đỏ khi bật cờ | Quan sát | Test xanh khi tắt cờ |
|---|---|---|---|
| M1 | | | |
| M2 | | | |
| M3 | | | |
| M4 | | | |
| M5 | | | |
| M6 | | | |
| M7 | | | |
