# Data Model: Giới hạn AI theo ngày

**Spec**: [spec.md](./spec.md) · **Research**: [research.md](./research.md)

## Bảng mới: `AiUsageDaily`

| Cột | Kiểu | Ràng buộc | Ghi chú |
|---|---|---|---|
| `Day` | `date` | NOT NULL | Ngày theo giờ Việt Nam (UTC+7), không phải ngày UTC |
| `Scope` | `varchar(50)` | NOT NULL | `'global'` hoặc `'user:{guid:D}'` (tối đa 41 ký tự) |
| `Count` | `integer` | NOT NULL, `CHECK ("Count" >= 0)` | Số lượt đã giữ chỗ trong ngày cho phạm vi đó |

- **Khóa chính**: `(Day, Scope)`. Không có cột `Id`, không có `UserId` (lý do ở research.md R2).
- Không khóa ngoại. Không index phụ (mọi truy vấn đều theo khóa chính).
- Ý nghĩa `Count`: dòng `global` = số request đã được nhận và (có thể đã) gọi Gemini, không hoàn; dòng `user:*` = số lượt user đã dùng, có hoàn khi thất bại hoàn toàn.
- Dung lượng: mỗi (ngày × user có dùng AI) một dòng; không cần job dọn (ngoài phạm vi).

### Entity / Configuration (EF Core)

- `QuizBattle.Domain/Entities/AiUsage.cs`: `DateOnly Day`, `string Scope`, `int Count`.
- `QuizBattle.Infrastructure/Persistence/Configurations/AiUsageConfiguration.cs`: `ToTable("AiUsageDaily")`, `HasKey(x => new { x.Day, x.Scope })`, `Scope` `HasMaxLength(50)` + `IsRequired()`, `ToTable(t => t.HasCheckConstraint("CK_AiUsageDaily_Count_NonNegative", "\"Count\" >= 0"))`.
- `DbSet<AiUsage> AiUsages` trong `AppDbContext` (đúng tên DbContext hiện có).
- Migration `AddAiUsageDaily`: chỉ tạo bảng mới, không đụng bảng cũ.
- Npgsql/EF Core 8 ánh xạ `DateOnly` ↔ `date` sẵn có, không cần converter.

## Quy ước `Scope`

```
global
user:3f2504e0-4f89-11d3-9a0c-0305e82c3301     (Guid dạng "D", chữ thường)
```

Hai hàm tạo chuỗi nằm ở một chỗ duy nhất trong Application (`AiQuotaScopes.Global`, `AiQuotaScopes.ForUser(Guid)`) để repository, service và test dùng cùng định dạng.

## Truy vấn

### 1. Giữ chỗ (một transaction, READ COMMITTED, global → user)

Chạy lần lượt trong cùng một transaction; cả hai dùng câu sau, khác `@scope`/`@limit`:

```sql
INSERT INTO "AiUsageDaily" ("Day", "Scope", "Count")
VALUES (@day, @scope, 1)
ON CONFLICT ("Day", "Scope")
DO UPDATE SET "Count" = "AiUsageDaily"."Count" + 1
WHERE "AiUsageDaily"."Count" < @limit
RETURNING "Count";
```

```
BEGIN
  r1 = reserve(@day, 'global', globalLimit)
  if r1 is empty -> ROLLBACK; return Rejected(Global)
  r2 = reserve(@day, 'user:{id}', userLimit)
  if r2 is empty -> ROLLBACK; return Rejected(User)      -- hoàn tác luôn lượt global ở r1
COMMIT; return Granted(Day = @day, UserCount = r2)
```

Kết quả `Granted` mang `Day` để hoàn lượt dùng đúng ngày đã giữ chỗ.

### 2. Hoàn lượt (chỉ dòng user, một câu lệnh, không transaction nhiều bước)

```sql
UPDATE "AiUsageDaily"
SET "Count" = GREATEST("Count" - 1, 0)
WHERE "Day" = @day AND "Scope" = @scope;     -- @scope = 'user:{id}', KHÔNG BAO GIỜ 'global'
```

Interface chỉ có `RefundUserAsync(userId, day)`; không có hàm hoàn global, nên sai sót "hoàn global" không thể viết ra.

### 3. Đọc trạng thái (GET quota, chỉ đọc)

```sql
SELECT "Scope", "Count" FROM "AiUsageDaily"
WHERE "Day" = @day AND "Scope" IN ('global', @userScope);
```

Dùng `AsNoTracking`/projection; thiếu dòng nghĩa là 0.

## Bất biến

- Với mỗi ngày: `Count(global) ≥ Σ Count(user:*)` (global không bao giờ được hoàn; user chỉ giảm khi hoàn).
- Mọi `Count` ∈ `[0, limit]` trừ trường hợp đổi cấu hình hạ trần giữa ngày.
- Sau N giữ chỗ song song với trần K: số `Granted` = min(N, K) và `Count` = số `Granted`.

## Không thay đổi

Mọi bảng hiện có (`Users`, `Rooms`, `Questions`, ...) giữ nguyên; không khóa dòng `Users` (khác với cộng XP/Xu cuối trận).
