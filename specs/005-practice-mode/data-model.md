# Data Model: Chế độ ôn tập

Một migration `AddPracticeMode` tạo 3 bảng. Không sửa bảng có sẵn. (Chưa viết migration ở đợt này.)

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
| `CompletedAt` | timestamptz NULL | set khi nộp câu cuối |

Index: `(UserId, StartedAt DESC)` cho "lượt gần đây".

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
| `Day` | date NOT NULL | ngày VN của thời điểm nộp |
| `AnsweredAt` | timestamptz NOT NULL | |

- Unique `(SessionId, QuestionId)`: mỗi câu chấm tối đa 1 lần trong một lượt (FR-005). NULL không xung đột nên không chặn nhầm (R7).
- Unique một phần `(UserId, QuestionId, Day) WHERE "XpAwarded" > 0`: lưới an toàn cho "XP một lần cho mỗi (user, câu, ngày)".
- CHECK `("IsCorrect" OR "XpAwarded" = 0)`: sai không có XP.

## PracticeXpDaily (bộ đếm trần)
| Cột | Kiểu | Ghi chú |
|---|---|---|
| `UserId` | uuid | FK `Users`, CASCADE |
| `Day` | date | ngày VN |
| `Xp` | int NOT NULL | CHECK `>= 0` |

PK `(UserId, Day)`. Trần (`Practice:DailyXpCap`) kiểm bằng SQL, không bằng CHECK cứng, để đổi cấu hình không cần migration.

## Bất biến (sau tính năng)
1. `Users.TotalXp = SUM(MatchRewards.Xp) + SUM(PracticeAnswers.XpAwarded)` cho từng user. (Thay `CLAUDE.md:60`; xem research R8.)
2. `PracticeXpDaily.Xp = SUM(PracticeAnswers.XpAwarded)` theo `(UserId, Day)`.
3. `Users.CoinBalance = SUM(CoinTransactions.Amount)`: không đổi, ôn tập không ghi `CoinTransaction`.
4. `PracticeXpDaily.Xp <= Practice:DailyXpCap`.

## Quan hệ trạng thái lượt ôn
`Đang ôn` (CompletedAt null) → `Hoàn tất` (CompletedAt set khi câu cuối được nộp). Lượt chưa xong vẫn đọc lại/tiếp tục được; không hết hạn ở phạm vi này (xem plan, điểm cần chốt).
