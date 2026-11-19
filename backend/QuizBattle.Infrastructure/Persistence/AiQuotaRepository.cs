using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Npgsql;
using QuizBattle.Application.AiQuota;

namespace QuizBattle.Infrastructure.Persistence;

public sealed class AiQuotaRepository(AppDbContext db, ILogger<AiQuotaRepository> logger) : IAiQuotaRepository
{
    // Kiểm tra và tăng trong MỘT câu lệnh: hai request tranh cùng dòng thì request sau chờ khóa dòng rồi đánh giá lại WHERE
    // với giá trị mới. Không có dòng trả về = dòng đã chạm trần. Lần đầu trong ngày (INSERT) luôn tạo Count = 1, hợp lệ vì trần >= 1.
    private const string ReserveSql = """
        INSERT INTO "AiUsageDaily" ("Day", "Scope", "Count")
        VALUES (@day, @scope, 1)
        ON CONFLICT ("Day", "Scope")
        DO UPDATE SET "Count" = "AiUsageDaily"."Count" + 1
        WHERE "AiUsageDaily"."Count" < @limit
        RETURNING "Count"
        """;

    public async Task<AiReserveResult> ReserveAsync(Guid userId, DateOnly day, int userLimit, int globalLimit, CancellationToken cancellationToken)
    {
        // Rời khỏi using mà chưa Commit thì transaction tự rollback - hoàn tác luôn lần tăng global khi user bị từ chối.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Luôn global trước, user sau: mọi giao dịch giữ chỗ cùng thứ tự khóa nên không thể khóa chéo nhau.
        if (await TryReserveRowAsync(transaction, day, AiQuotaScopes.Global, globalLimit, cancellationToken) is null)
        {
            return AiReserveResult.Rejected(day, AiLimitKind.Global);
        }

        var userCount = await TryReserveRowAsync(transaction, day, AiQuotaScopes.ForUser(userId), userLimit, cancellationToken);
        if (userCount is null)
        {
            return AiReserveResult.Rejected(day, AiLimitKind.User);
        }

        await transaction.CommitAsync(cancellationToken);
        return AiReserveResult.Accepted(day, userCount.Value);
    }

    public async Task RefundUserAsync(Guid userId, DateOnly day, CancellationToken cancellationToken)
    {
        // Một câu UPDATE chạm đúng một dòng user, không giữ khóa nào khác nên không thể tạo vòng chờ với ReserveAsync.
        var scope = AiQuotaScopes.ForUser(userId);
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""UPDATE "AiUsageDaily" SET "Count" = GREATEST("Count" - 1, 0) WHERE "Day" = {day} AND "Scope" = {scope}""",
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Hoàn lượt là best-effort: lỗi DB ở đây không được thay thế lỗi AI gốc mà người dùng sẽ thấy. Ghi log (tầng
            // Application không có logger) để biết lượt nào bị mất; không che lỗi nào khác vì request này đã thất bại sẵn.
            logger.LogError(ex, "Không hoàn được lượt AI của {Scope} ngày {Day}.", scope, day);
        }
    }

    public async Task<(int UserCount, int GlobalCount)> GetUsageAsync(Guid userId, DateOnly day, CancellationToken cancellationToken)
    {
        var userScope = AiQuotaScopes.ForUser(userId);
        var rows = await db.AiUsages.AsNoTracking()
            .Where(a => a.Day == day && (a.Scope == AiQuotaScopes.Global || a.Scope == userScope))
            .Select(a => new { a.Scope, a.Count })
            .ToListAsync(cancellationToken);

        return (
            rows.FirstOrDefault(r => r.Scope == userScope)?.Count ?? 0,
            rows.FirstOrDefault(r => r.Scope == AiQuotaScopes.Global)?.Count ?? 0);
    }

    // DbCommand thô thay vì SqlQuery: EF bọc câu thô trong SELECT ... FROM (...), không hợp lệ với INSERT ... RETURNING.
    private async Task<int?> TryReserveRowAsync(IDbContextTransaction transaction, DateOnly day, string scope, int limit, CancellationToken cancellationToken)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = ReserveSql;
        command.Parameters.Add(new NpgsqlParameter("day", day));
        command.Parameters.Add(new NpgsqlParameter("scope", scope));
        command.Parameters.Add(new NpgsqlParameter("limit", limit));

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is int count ? count : null;
    }
}
