# Research: Chế độ ôn tập

Mỗi quyết định ghi kèm file:dòng đã đọc (nhánh `feat/practice-mode` tách từ `main` @ 7678878).

## R1. Cách hiện tại chặn lộ `CorrectIndex`
- `QuestionStartedDto` (`Application/Game/QuestionStartedDto.cs:4-7`) cố tình KHÔNG khai báo `CorrectIndex`.
- `GameService.cs:350` chỉ gán `ended ? current.CorrectIndex : null` trong `CurrentQuestionStateDto` (`CurrentQuestionStateDto.cs:4,16`).
- Chấm điểm ở server: `GameService.cs:260` (`selectedIndex == snapshot.Question.CorrectIndex`).
- **Quyết định**: tạo `PracticeQuestionDto` (không có `CorrectIndex`) và `PracticeAnswerResultDto` (có `CorrectIndex`). Hai kiểu riêng, không dùng một DTO có trường nullable.
- Câu có thể ôn: `Approved` và `EndedAt != null` (`Domain/Entities/Question.cs:23-27`). `EndedAt` có nghĩa đáp án đã được công bố cho cả phòng (`QuestionEndedDto.cs:5` mang `CorrectIndex`), nên ôn không làm lộ thứ chưa từng lộ. Câu `Pending` là đầu ra AI chưa duyệt, không dùng.

## R2. Điều kiện "phòng ôn được"
- `Room.Status == Finished` (`Domain/Enums/RoomStatus.cs`), chủ phòng `Room.HostUserId` (`Room.cs:20`), người chơi `Player.UserId`, null = khách (`Player.cs:14-15`).
- Chặn phòng tự tạo một mình: trận chỉ có thưởng khi >= 2 tài khoản không phải chủ phòng đã trả lời (`RewardCalculator.cs:25,29-33,54`). Phòng hợp lệ ⇔ tồn tại `MatchReward` `Status = Awarded` của phòng (`MatchRewardConfiguration.cs:14` bảo đảm Awarded ⇔ không có `DenialReason`). Tái dùng kết quả đã lưu, không tính lại luật.
- Phòng đã xóa: `MatchReward.RoomId` SET NULL (`MatchRewardConfiguration.cs:38-40`); phòng không còn thì không còn gì để ôn.

## R3. XP và bất biến `TotalXp`
- `Xp = Score / 10` mỗi trận (`RewardCalculator.cs:44`); `TotalXp` chỉ tăng ở `RewardSettlement.cs:64`, trong transaction kết thúc trận, sau khi khóa `Users` theo `Id` (`GameRepository.cs:205`).
- Bất biến `TotalXp = SUM(MatchRewards.Xp)` nằm ở `CLAUDE.md:60`, `RewardSettlementTests.cs:82-91`, `ProfileRewardsTests.cs:45` (seed "đúng bất biến" ở `:113`).
- Các chỗ dựa vào bất biến và so sánh phương án lưu: xem R8.

## R4. Khóa `Users`
- Kết thúc trận: khóa `Room` rồi `Users ... ORDER BY "Id" FOR UPDATE` (`GameRepository.cs:23-26,205`).
- Cửa hàng: chỉ khóa MỘT dòng Users, không chờ Room (`AvatarRepository.cs:25-28,112`), nên không nằm trong chu trình chờ.
- **Quyết định**: ôn tập chỉ khóa MỘT dòng `Users` của chính user (không bao giờ khóa Room, không khóa Users khác) rồi mới đụng `PracticeXpDaily`/`PracticeAnswers`. Settlement và cửa hàng không đụng hai bảng đó nên không có chu trình chờ.
- Test độ nhạy dùng cờ `internal` `UseUserRowLock`/`BeforeBalanceWrite` (`AvatarRepository.cs:11-16`, dùng ở `ShopPurchaseSensitivityTests.cs:63`). Ôn tập theo cùng mẫu.

## R5. Con số XP đề xuất
- Điểm một câu đúng 500..1000 (`ScoreCalculator.cs:5-6,22-26`), tức 50..100 XP/câu đúng trong trận.
- Trận thường (giả định 10 câu, đúng ~70%, điểm TB ~750 mỗi câu đúng): ~5250 điểm, khoảng **525 XP**.
- Cấp n→n+1 cần `200*n` XP (`LevelCalculator.cs:6`): 1→2 = 200, 5→6 = 1000.
- **Đề xuất**: `Practice:XpPerCorrect = 5` (khoảng 7% XP/câu đúng của trận) và `Practice:DailyXpCap = 100` (20 câu đúng, khoảng 19% một trận thường, nửa cấp 1→2).
- Lý do: ôn không áp lực thời gian, đáp án có thể đã nhớ, nên thưởng phải rẻ hơn thi đấu nhiều. 100/ngày vẫn là thói quen thấy được nhưng không thay thế việc chơi: một trận thường bằng ~5 ngày ôn kịch trần. `5` chia hết `100` nên nhánh `LEAST` không xảy ra ở cấu hình mặc định; nhánh đó vẫn được test bằng cấu hình tiêm vào (xem plan).

## R6. Bộ đếm ngày an toàn đồng thời
- Mẫu có sẵn: `AiQuotaRepository.cs:13-20` (`INSERT ... ON CONFLICT DO UPDATE ... WHERE Count < limit RETURNING`), không dòng trả về = chạm trần; dùng `DbCommand` thô vì EF bọc `SqlQuery` trong `SELECT` (`:74-86`).
- Khác biệt: AI commit giữ chỗ TRƯỚC khi gọi Gemini (`:25-39`) vì có bước gọi ngoài, kèm hoàn lượt khi lỗi (`:43-59`). Ôn tập không có bước ngoài, nên bộ đếm + dòng XP + `TotalXp` ở CÙNG một transaction; lỗi ở bước nào thì rollback tất cả, không cần hoàn lượt.
- Ngày = `VietnamDay.DayOf(now)` (`Domain/AiQuota/VietnamDay.cs:13`), `now` truyền từ service. `NextResetAt` (`:16`) cho "làm mới lúc ...".
- Khóa bảng `(UserId, Day)`, khác `Scope` chuỗi ở `AiUsageConfiguration.cs:13-15` vì không có dòng "global", nên không có NULL trong khóa.

## R7. Khóa ngoại để không phá bất biến
- `MatchReward` giữ thưởng khi xóa phòng bằng SET NULL (`MatchRewardConfiguration.cs:36-40`); xóa phòng là thao tác có thật (`RoomsController.cs:41`, `RoomService.cs:57`).
- **Quyết định**: `PracticeAnswer` (dòng mang XP) chỉ cascade theo `User`. `RoomId`, `QuestionId`, `SessionId` đều nullable và SET NULL. Nếu cascade theo Room/Question thì xóa phòng làm mất XP đã cộng, `TotalXp > SUM(...)`.
- Chống chấm đôi dựa vào khóa duy nhất `(SessionId, QuestionId)`; khi `SessionId` thành NULL (hiếm: chỉ khi phiên bị xóa riêng) nhiều dòng NULL vẫn hợp lệ (như `IX_MatchRewards_PlayerId` ở `MatchRewardConfiguration.cs:27`), nên không chặn nhầm. Không có chức năng xóa phiên riêng trong phạm vi này.

## R8. Mọi chỗ dựa vào "TotalXp = SUM(MatchRewards.Xp)"

| Chỗ | File:dòng | Phụ thuộc bất biến? | Ảnh hưởng khi thêm XP ôn tập |
|---|---|---|---|
| Quy tắc dự án | `CLAUDE.md:60` | Có (văn bản) | Sửa thành `TotalXp = SUM(MatchRewards.Xp) + SUM(PracticeAnswers.XpAwarded)` |
| Test bất biến settlement | `RewardSettlementTests.cs:82-91` | Có (`Assert.Equal(xp, user.TotalXp)`) | Test hiện tại không ôn nên vẫn xanh, nhưng helper phải cộng nguồn mới để không sai khi user có ôn |
| Test hồ sơ | `ProfileRewardsTests.cs:45,113` | Có | Tương tự |
| `GET /api/users/me/rewards` | `UserRepository.cs:34-40` (lọc `Awarded`), `UsersController.cs:19-22` | Không (lịch sử trận) | Không đổi nếu XP ôn nằm bảng riêng. Nếu mở rộng `MatchReward`, dòng ôn lọt vào danh sách trừ khi thêm lọc |
| Bảng xếp hạng XP | `UserRepository.cs:42-48`, `UserConfiguration.cs:29-33` | Không (đọc `TotalXp`) | XP ôn tính vào xếp hạng. Chấp nhận nhờ trần 100/ngày; nêu ở plan |
| Hồ sơ, cấp | `ProfileService.cs:18-19`, `GameService.cs:424` | Không | Cấp tăng nhờ XP ôn, đúng ý |
| `RewardSettlement.FromStored` | `RewardSettlement.cs:90-109`, nạp ở `GameRepository.cs:220` | Gián tiếp | Không đổi nếu bảng riêng. Nếu mở rộng, dòng ôn (PlayerId null) vẫn bị nạp theo `RoomId`, chỉ nhờ `Where(PlayerId != null)` mới bị bỏ |
| Ràng buộc `MatchRewards` | `MatchRewardConfiguration.cs:13-14,27` | Có | Ôn tập không có `Player`, `Score`, `Rank`: phải nới ràng buộc hoặc nhồi giá trị giả |
| Test schema | `SchemaConstraintsTests.cs:35-56` | Có | Phải xét lại nếu đổi `MatchRewards` |

### So sánh phương án
**A. Bảng mới (`PracticeAnswers.XpAwarded`): ĐỀ XUẤT**
- Ưu: không đụng `MatchRewards` (lịch sử trận, `FromStored`, ràng buộc, test schema giữ nguyên); mỗi bảng một nghĩa; dòng ôn mang dữ liệu thật (lượt, câu, đúng/sai, ngày).
- Giá: bất biến đổi thành tổng hai nguồn; sửa `CLAUDE.md:60` và hai helper test (`RewardSettlementTests.cs:89`, `ProfileRewardsTests.cs:45`); thêm test bất biến bao cả hai nguồn; một migration tạo 3 bảng.

**B. Mở rộng `MatchReward`**
- Ưu: bất biến giữ nguyên chữ.
- Giá: `MatchReward` mô tả "kết quả thưởng của một Player trong một trận" (`MatchReward.cs:5-9`). Phải thêm `Kind`, nới `PlayerId`/`RoomCode`/`Score`/`RewardRank`/`Status`/`DenialReason` (CK ở `:13-14`), và mọi truy vấn lịch sử phải lọc `Kind`; quên lọc ở một nơi (`/rewards`, `FromStored`, `GameRepository.cs:220`) là lọt dòng ôn.

Kết luận: chọn A; đưa vào "Điểm cần chốt" #1 của plan.

## R9. Rate limit
- Chính sách có sẵn ở `Program.cs:104-124` (`auth`, `generate`, `join-room`). `UseRateLimiter` đứng sau `UseAuthorization` nên phân vùng theo user được (bài học commit e319123). Thêm policy `practice` theo user, ví dụ 120 request/phút.

## R10. Mẫu test sẵn có
- Tích hợp Postgres thật: `DatabaseFixture` + `RequiresDatabaseFact`. HTTP thật: `Integration/Http/ApiFactory.cs`, `HttpTestBase.cs`.
- "Mutation" trong repo là test độ nhạy tự viết, không dùng công cụ mutation: tắt cơ chế bằng cờ `internal` rồi chứng minh test đồng thời đỏ (`ShopPurchaseSensitivityTests.cs:63`, `AvatarRepository.cs:11-16`). Ôn tập theo đúng mẫu, không thêm thư viện.
