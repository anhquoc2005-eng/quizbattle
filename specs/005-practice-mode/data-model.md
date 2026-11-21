# Data Model: Chế độ ôn tập

Một migration `AddPracticeMode` tạo 3 bảng. Không sửa bảng có sẵn. (Chưa viết migration ở đợt spec.)

## PracticeSessions
| Cột | Kiểu | Ghi chú |
|---|---|---|
| `Id` | uuid PK | |
| `UserId` | uuid NOT NULL | FK `Users`, CASCADE |
| `RoomId` | uuid NULL | FK `Rooms`, SET NULL |
| `RoomCode` | varchar(6) NOT NULL | bản sao, như `MatchReward` |
| `RoomTopic` | varchar(200) NOT NULL | bản sao |
| `QuestionIds` | uuid[] NOT NULL | thứ tự ngẫu nhiên chốt lúc tạo; không FK (mảng) |
| `StartedAt` | timestamptz NOT NULL | |
| `CompletedAt` | timestamptz NULL | set khi nộp câu cuối hoặc khi hết câu còn tồn tại (Ch8) |

Index:
- Unique một phần `IX_PracticeSessions_OpenPerUserRoom` trên `("UserId", "RoomId") WHERE "CompletedAt" IS NULL`: mỗi (user, phòng) tối đa một lượt đang mở (Ch7). `RoomId` NULL (phòng đã xóa) không xung đột.
- `("UserId", "StartedAt" DESC)` cho "lượt gần đây".

## PracticeAnswers (dòng ghi nhận XP)
| Cột | Kiểu | Ghi chú |
|---|---|---|
| `Id` | uuid PK | |
| `SessionId` | uuid NULL | FK `PracticeSessions`, SET NULL |
| `UserId` | uuid NOT NULL | FK `Users`, CASCADE (nguồn bất biến XP) |
| `QuestionId` | uuid NULL | FK `Questions`, SET NULL |
| `SelectedIndex` | int NOT NULL | CHECK 0..3 |
| `IsCorrect` | bool NOT NULL | |
| `XpAwarded` | int NOT NULL | CHECK >= 0 |
| `XpReason` | varchar(20) NULL | NULL ⇔ nhận đủ `XpPerCorrect`; xem CHECK dưới |
| `Day` | date NOT NULL | ngày VN của thời điểm nộp |
| `AnsweredAt` | timestamptz NOT NULL | |

Ràng buộc:
- Unique `(SessionId, QuestionId)`: lưới DB cho "mỗi câu chấm đúng 1 lần trong một lượt" (FR-005). NULL không xung đột nên không chặn nhầm (research R7). Khi khóa `Users` đang giữ, vi phạm này không thể xảy ra; nó chỉ để DB tự bảo vệ (research R12).
- Unique một phần `(UserId, QuestionId, Day) WHERE "XpAwarded" > 0`: lưới DB cho "XP một lần cho mỗi (user, câu, ngày)" (Ch4).
- `CK_PracticeAnswers_XpReason` — một CHECK duy nhất gom giá trị và quan hệ:
  ```sql
  ("XpReason" IS NULL AND "IsCorrect")
  OR ("XpReason" = 'wrong' AND NOT "IsCorrect" AND "XpAwarded" = 0)
  OR ("XpReason" IN ('already_today', 'daily_cap') AND "IsCorrect" AND "XpAwarded" = 0)
  OR ("XpReason" = 'daily_cap_partial' AND "IsCorrect" AND "XpAwarded" > 0)
  ```
  Nghĩa: `XpReason` chỉ nhận `{wrong, already_today, daily_cap, daily_cap_partial}` hoặc NULL; NULL chỉ hợp lệ với câu đúng. DB không biết `XpPerCorrect` (cấu hình) nên "NULL ⇒ XpAwarded đúng bằng `XpPerCorrect`" do `PracticeRewardCalculator`/repository bảo đảm và test kiểm (không bằng CHECK). Hệ quả: `XpPerCorrect = 0` cho câu đúng ra `XpReason = NULL, XpAwarded = 0` (hợp lệ).
- CHECK `("IsCorrect" OR "XpAwarded" = 0)` được ràng buộc ở trên nên không thêm riêng.

## PracticeXpDaily (bộ đếm trần)
| Cột | Kiểu | Ghi chú |
|---|---|---|
| `UserId` | uuid | FK `Users`, CASCADE |
| `Day` | date | ngày VN |
| `Xp` | int NOT NULL | CHECK `>= 0` |

PK `(UserId, Day)`. Trần (`Practice:DailyXpCap`) kiểm bằng SQL, không bằng CHECK cứng, để đổi cấu hình không cần migration.

## Bất biến (sau tính năng)
1. `Users.TotalXp = SUM(MatchRewards.Xp) + SUM(PracticeAnswers.XpAwarded)` cho từng user. (Thay `CLAUDE.md:60`; research R8.)
2. `PracticeXpDaily.Xp = SUM(PracticeAnswers.XpAwarded)` theo `(UserId, Day)`.
3. `Users.CoinBalance = SUM(CoinTransactions.Amount)`: không đổi.
4. `PracticeXpDaily.Xp <= Practice:DailyXpCap`.
5. Mỗi `(UserId, RoomId)` có tối đa một `PracticeSession` với `CompletedAt IS NULL`.

## Trạng thái lượt ôn
`Đang mở` (CompletedAt null) → `Hoàn tất` (CompletedAt set khi nộp câu cuối, hoặc khi mọi câu còn tồn tại đã nộp mà vẫn thiếu câu bị xóa). Lượt hoàn tất không mở lại; bấm "Ôn" lần nữa tạo lượt mới.
