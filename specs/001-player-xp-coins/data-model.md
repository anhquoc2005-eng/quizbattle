# Data Model: Tài khoản người chơi, XP và Xu

Chỉ liệt kê phần thay đổi. Quy ước giữ nguyên: bảng số nhiều PascalCase, thời gian `timestamptz`, `Guid` làm khóa.

## Thay đổi bảng hiện có

### Users (sửa)

| Cột | Kiểu | Ghi chú |
|---|---|---|
| `TotalXp` (mới) | `int NOT NULL DEFAULT 0` | Chỉ tăng. `CHECK ("TotalXp" >= 0)` tên `CK_Users_TotalXp_NonNegative` |
| `CoinBalance` (mới) | `int NOT NULL DEFAULT 0` | `CHECK ("CoinBalance" >= 0)` tên `CK_Users_CoinBalance_NonNegative` |

Không có cột cấp độ: cấp = `LevelCalculator.FromXp(TotalXp)`.

Index mới: `IX_Users_TotalXp` trên `(TotalXp DESC, CreatedAt, Id)`, `WHERE "TotalXp" > 0`.

Bất biến:
- `CoinBalance = SUM(CoinTransactions.Amount WHERE UserId = Users.Id)`
- `TotalXp = SUM(MatchRewards.Xp WHERE UserId = Users.Id)` (đúng cả sau khi phòng bị xóa: `MatchRewards` không cascade theo phòng/người chơi)

### Players (sửa)

| Cột | Thay đổi |
|---|---|
| `UserId` (đã có, nullable) | bắt đầu được dùng; FK tới Users `ON DELETE SET NULL` giữ nguyên |

Index mới: `IX_Players_RoomId_UserId` unique trên `(RoomId, UserId)`, `WHERE "UserId" IS NOT NULL`.
Index hiện có giữ nguyên: `IX_Players_RoomId_lower_DisplayName` (unique, SQL thô), `IX_Players_SessionTokenHash` (unique).

## Bảng mới

### MatchRewards

Kết quả thưởng của **một Player** trong một trận. Có một dòng cho mọi Player có tài khoản (kể cả bị loại, để lưu lý do); khách không có dòng.

| Cột | Kiểu | Ghi chú |
|---|---|---|
| `Id` | `uuid` PK | |
| `PlayerId` | `uuid NULL` | FK Players `ON DELETE SET NULL` (xóa phòng làm Player bị cascade nhưng thưởng phải còn). **Unique** (`IX_MatchRewards_PlayerId`): chặn cộng đôi; nhiều NULL hợp lệ. Luôn có giá trị lúc tạo |
| `UserId` | `uuid NOT NULL` | FK Users `ON DELETE CASCADE` (chỉ khi xóa tài khoản) |
| `RoomId` | `uuid NULL` | FK Rooms `ON DELETE SET NULL`. Luôn có giá trị lúc tạo |
| `RoomCode` | `varchar(6) NOT NULL` | bản sao `Room.Code` lúc kết thúc, để hồ sơ hiển thị khi phòng đã bị xóa |
| `RoomTopic` | `varchar(200) NOT NULL` | bản sao `Room.Topic` |
| `FinishedAt` | `timestamptz NOT NULL` | bản sao `Room.EndedAt` |
| `Status` | `text NOT NULL` | `Awarded` / `NotAwarded` |
| `DenialReason` | `text NULL` | `Host` / `NoAnswers` / `NotEnoughAccounts`; NULL khi `Awarded` |
| `Score` | `int NOT NULL` | điểm trận lúc kết thúc |
| `RewardRank` | `int NOT NULL` | hạng theo điểm (competition ranking, tính cả khách/chủ phòng) |
| `Xp` | `int NOT NULL` | `floor(Score / 10)`; 0 khi `NotAwarded` |
| `Coins` | `int NOT NULL` | phần điểm: `floor(Score / 100)`; 0 khi `NotAwarded` |
| `RankBonus` | `int NOT NULL` | 30/20/10 hoặc 0 |

Ràng buộc: `CHECK ("Xp" >= 0 AND "Coins" >= 0 AND "RankBonus" >= 0)`; `CHECK (("Status" = 'Awarded') = ("DenialReason" IS NULL))`.
Index: `IX_MatchRewards_UserId_FinishedAt` trên `(UserId, FinishedAt DESC)` cho lịch sử hồ sơ. Hồ sơ chỉ đọc bảng này (không join Rooms/Players), nên không đổi khi phòng bị xóa.
Tổng Xu của dòng = `Coins + RankBonus`.

### CoinTransactions

Sổ cái Xu, chỉ thêm, không sửa/xóa. Mua đồ ở tính năng 2 là dòng `Amount < 0`.

| Cột | Kiểu | Ghi chú |
|---|---|---|
| `Id` | `uuid` PK | |
| `UserId` | `uuid NOT NULL` | FK Users `ON DELETE CASCADE` |
| `Amount` | `int NOT NULL` | dương = cộng, âm = trừ. `CHECK ("Amount" <> 0)` |
| `Reason` | `text NOT NULL` | `MatchReward` (dùng ngay), `ShopPurchase` (dành cho tính năng 2) |
| `RoomId` | `uuid NULL` | FK Rooms `ON DELETE SET NULL` (trận/phòng liên quan) |
| `MatchRewardId` | `uuid NULL` | FK MatchRewards `ON DELETE SET NULL`; unique có điều kiện `WHERE "MatchRewardId" IS NOT NULL` (một khoản thưởng sinh tối đa một dòng sổ cái) |
| `CreatedAt` | `timestamptz NOT NULL` | |

Index: `IX_CoinTransactions_UserId_CreatedAt` trên `(UserId, CreatedAt DESC)`.

Dòng sổ cái (`Reason = MatchReward`) được tạo khi `Coins + RankBonus > 0` cùng `SaveChanges` với cập nhật `Users.CoinBalance`.

## Quan hệ

```text
Users 1 ──< Players (UserId nullable)   Rooms 1 ──< Players
Users 1 ──< MatchRewards >── 0..1 Players    MatchRewards >── 0..1 Rooms  (SET NULL khi xóa phòng, thưởng còn)
Users 1 ──< CoinTransactions >── 0..1 MatchRewards, 0..1 Rooms
```

## Quy tắc nghiệp vụ (Domain, hàm thuần)

`RewardCalculator.Calculate(participants, hostUserId)` nhận danh sách `{ PlayerId, UserId?, Score, AnsweredCount }` của **mọi** người chơi và trả `{ PlayerId, RewardRank, Status, DenialReason?, Xp, Coins, RankBonus }`.

1. `RewardRank`: competition ranking theo `Score` giảm dần trên mọi người chơi (1, 1, 3).
2. Người chơi đủ điều kiện = có `UserId`, `UserId != hostUserId`, `AnsweredCount >= 1`.
3. Trận hợp lệ = số `UserId` khác nhau trong nhóm đủ điều kiện `>= 2`.
4. Đủ điều kiện và trận hợp lệ: `Xp = Score / 10`, `Coins = Score / 100`, `RankBonus = Score > 0 ? {1:30, 2:20, 3:10}[RewardRank] : 0`.
5. Còn lại `NotAwarded` với `DenialReason` theo thứ tự ưu tiên: `Host`, `NoAnswers`, `NotEnoughAccounts`. Khách không có dòng (DTO trả lý do `Guest`).

`LevelCalculator`:
- `XpForLevel(n) = 100 * n * (n - 1)`; `FromXp(xp) = max n sao cho XpForLevel(n) <= xp`.
- `Progress(xp)` trả `{ Level, XpIntoLevel = xp - XpForLevel(L), XpForNext = XpForLevel(L + 1) - XpForLevel(L) = 200 * L }`.

## Chuyển trạng thái

```text
Room: Draft -> Lobby -> InProgress -> Finished     (không đổi)
                                      └─ chỉ ở bước này: tạo MatchRewards (+ CoinTransactions, cập nhật Users), cùng 1 transaction
MatchReward: (không tồn tại) -> Awarded | NotAwarded    (bất biến sau khi tạo)
```

## Migration

Một migration `AddXpCoinsAndPlayerAccounts`: không chuyển dữ liệu, mọi cột mới có DEFAULT, bảng mới rỗng. Chi tiết an toàn ở [research.md R8](research.md#r8-migration-và-triển-khai-production).
