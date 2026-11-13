using Microsoft.EntityFrameworkCore;
using Npgsql;
using QuizBattle.Application.Common;
using QuizBattle.Application.Shop;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Infrastructure.Persistence;

public sealed class AvatarRepository(AppDbContext db) : IAvatarRepository
{
    // CHỈ DÀNH CHO TEST (internal, qua InternalsVisibleTo), mặc định không làm gì: dùng để chứng minh test đồng thời đủ nhạy.
    // UseUserRowLock = false bỏ câu FOR UPDATE; BeforeBalanceWrite chạy ngay trước SaveChanges để test ép hai giao dịch
    // cùng đọc số dư rồi mới ghi. Mã production không bao giờ đặt hai thuộc tính này.
    internal bool UseUserRowLock { get; set; } = true;

    internal Func<Task>? BeforeBalanceWrite { get; set; }

    public async Task<IReadOnlyList<AvatarItem>> GetCatalogAsync(CancellationToken cancellationToken) =>
        await db.AvatarItems.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<TResult> PurchaseAsync<TResult>(Guid userId, string itemKey, Func<PurchaseSnapshot, TResult> work, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // SELECT ... FOR UPDATE: khóa dòng Users của người mua cho tới khi commit. Hai lần mua cùng tài khoản (và mua với cộng
        // thưởng cuối trận, vốn cũng khóa Users) xếp hàng ở đây nên không mất cập nhật số dư. Chỉ giữ MỘT khóa Users và không chờ
        // Room hay Users khác, nên không thể nằm trong chu trình chờ với kết thúc trận (khóa Room rồi Users theo Id).
        var user = await LoadUserAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy tài khoản.");

        var item = await db.AvatarItems.AsNoTracking().SingleOrDefaultAsync(i => i.Key == itemKey, cancellationToken);
        var alreadyOwned = item is not null
            && await db.UserAvatarItems.AnyAsync(o => o.UserId == userId && o.ItemKey == itemKey, cancellationToken);

        var snapshot = new PurchaseSnapshot { User = user, Item = item, AlreadyOwned = alreadyOwned };
        var result = work(snapshot); // ném exception -> rollback khi transaction Dispose, không lưu gì

        if (snapshot.NewOwnership is not null)
        {
            db.UserAvatarItems.Add(snapshot.NewOwnership);
        }

        if (snapshot.NewCoinTransaction is not null)
        {
            db.CoinTransactions.Add(snapshot.NewCoinTransaction);
        }

        if (BeforeBalanceWrite is { } beforeWrite)
        {
            await beforeWrite();
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken); // số dư, quyền sở hữu và dòng sổ cái cùng một transaction
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_UserAvatarItems_UserId_ItemKey" })
        {
            // Lưới an toàn: khi đã khóa Users thì không xảy ra, nhưng DB vẫn chặn sở hữu hai lần.
            throw new ConflictException("Bạn đã sở hữu món này.", "already_owned");
        }

        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private Task<User?> LoadUserAsync(Guid userId, CancellationToken cancellationToken) =>
        UseUserRowLock
            ? db.Users.FromSqlInterpolated($"SELECT * FROM \"Users\" WHERE \"Id\" = {userId} FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
            : db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
}
