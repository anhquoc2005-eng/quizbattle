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

## Bằng chứng mutation (Đợt 2, 2026-11-21)

Cách đo: bật TẠM cờ làm mặc định trong `PracticeRepository`, chạy toàn bộ test `Practice*` thường (không gồm `PracticeSensitivityTests`), ghi test đỏ, hoàn lại cờ, build và chạy lại → xanh (608/608). Ngoài ra mỗi M có một test trong `PracticeSensitivityTests` chạy cùng kịch bản với bản gốc (xanh) rồi bản bật cờ (đỏ) và in thông điệp.

| M | Cờ | Test thường đỏ khi bật cờ | Thông điệp lỗi | Tắt cờ |
|---|---|---|---|---|
| M1 | `ReserveVariant = NoWhere` | `PracticeCapTests.Cap_LoweredBelowAlreadyUsed_DoesNotDecreaseCounterOrGrant` | `DbUpdateException` <- `PostgresException 23514: ... violates check constraint "CK_PracticeAnswers_XpAwarded_NonNegative"` (bộ đếm bị kéo từ 100 xuống 50 nên `granted = -50`) | xanh |
| M2 | `ReserveVariant = NoLeast` | `PracticeCapTests.Cap_RemainingLessThanQuestionXp_GrantsRemainderThenZero` | `Expected: Tuple (3, "daily_cap_partial") / Actual: Tuple (7, null)` | xanh |
| M3 | `ReserveVariant = RejectWhole` | cùng test với M2 | `Expected: Tuple (3, "daily_cap_partial") / Actual: Tuple (0, "daily_cap")` | xanh |
| M4 | `UseUserRowLock = false` | `PracticeCapTests.Cap_ParallelSubmitsOverCap_NeverExceedsCap` (`Expected: 100 / Actual: 1045`: tổng `XpAwarded` ghi nhận bị thổi phồng vì `granted` dựa vào khóa); `PracticeIdempotencyTests.Submit_TwoParallelRequestsSameQuestion_...` (7/8 tác vụ nhận `DbUpdateException` do unique `IX_PracticeAnswers_SessionId_QuestionId`); `PracticeInvariantTests.Submit_ConcurrentWithMatchSettlementOfSameUser_...` (`Expected: 510 / Actual: 110`: mất cập nhật `TotalXp`) | như cột trước. Test độ nhạy `Mutation_M4_...` thêm: ép hai giao dịch cùng đọc → `Expected: 15 / Actual: 10` ở bất biến, VÀ khẳng định bộ đếm ngày vẫn `<= trần` nhờ câu SQL nguyên tử | xanh |
| M5 | `SkipAlreadyTodayCheck = true` | `PracticeSubmitTests.Submit_SameQuestionNextSessionSameDay_ZeroXpAlreadyToday` | `DbUpdateException` <- `PostgresException 23505: ... unique constraint "IX_PracticeAnswers_UserId_QuestionId_Day_Awarded"` (lưới DB chặn, nhưng thành 500 thay vì `already_today`) | xanh |
| M6 | `SkipReplay = true` | 6 test: `Submit_SameQuestionAgain_...`, `Submit_AgainWithDifferentSelectedIndex_...`, `Submit_AgainAfterSessionCompleted_...`, `Submit_Replay_ReportsCurrentXpGainedToday...`, `Submit_AnsweredEarlierQuestionIsNotCurrent_...`, `Submit_TwoParallelRequestsSameQuestion_...` | `ConflictException: Chỉ được trả lời câu hiện tại của lượt ôn.` | xanh |
| M7 | `UseOnConflict = false` | `PracticeSessionTests.CreateSession_TwoParallelRequests_ReturnSameSessionId` (7/8 tác vụ lỗi) và `CreateSession_Twice_ReturnsSameSessionIdSecondNotCreated` | `PostgresException 23505: duplicate key value violates unique constraint "IX_PracticeSessions_OpenPerUserRoom"` | xanh |
| M8 (thêm) | `UseUserRowLock = false` + `ReserveVariant = NonAtomic` | `Cap_ParallelSubmitsOverCap_NeverExceedsCap` (`Expected: 100 / Actual: 2310`), `Submit_TwoParallelRequestsSameQuestion_...`, `Submit_ConcurrentWithMatchSettlementOfSameUser_...` (`Expected: 510 / Actual: 110`) | như cột trước | xanh |

Đính chính so với plan ban đầu: M1 (bỏ `WHERE`) KHÔNG làm test trần đồng thời đỏ, vì `LEAST` đã chặn bộ đếm vượt trần; `WHERE` chỉ cần khi trần bị hạ thấp hơn mức đã dùng (nếu không bộ đếm bị kéo xuống và `granted` âm). Phần bảo vệ trần đồng thời nằm ở câu lệnh nguyên tử + khóa; mutation chứng minh phần đó là M8 (đọc rồi ghi không nguyên tử, không khóa).
