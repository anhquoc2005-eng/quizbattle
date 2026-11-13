using Microsoft.EntityFrameworkCore;
using QuizBattle.Application.Common;
using QuizBattle.Application.Shop;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;
using QuizBattle.Infrastructure.Persistence;

namespace QuizBattle.Tests.Integration;

/// <summary>
/// Kiểm độ nhạy của test đồng thời: khi tắt khóa Users (FOR UPDATE) thì bất biến CoinBalance = SUM(CoinTransactions.Amount)
/// PHẢI bị phá. Nếu test này đỏ nghĩa là kịch bản không còn ép được tranh chấp, và các test đồng thời trong
/// <see cref="ShopPurchaseTests"/> không còn chứng minh được gì về cái khóa.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class ShopPurchaseSensitivityTests(DatabaseFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [RequiresDatabaseFact]
    public async Task Purchase_WithoutUserRowLock_LosesUpdate_ProvingConcurrencyTestsAreSensitive()
    {
        var user = await SeedUserAsync(60); // chỉ đủ Xu cho một món 50
        using var barrier = new Barrier(2);

        // Không khóa + barrier: cả hai giao dịch cùng đọc số dư 60 (đều thấy đủ Xu) rồi mới ghi.
        var outcomes = await Task.WhenAll(
            PurchaseWithoutLockAsync(user.Id, "hair.variant03", barrier),
            PurchaseWithoutLockAsync(user.Id, "hair.variant04", barrier));

        Assert.All(outcomes, e => Assert.Null(e)); // cả hai "thành công" dù chỉ đủ Xu cho một món
        await using var db = fixture.CreateContext();
        var balance = (await db.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id)).CoinBalance;
        var ledger = await db.CoinTransactions.Where(c => c.UserId == user.Id).SumAsync(c => c.Amount);
        Assert.Equal(10, balance); // cập nhật bị mất: đáng lẽ phải từ chối một món
        Assert.Equal(-40, ledger); // 60 - 50 - 50
        Assert.NotEqual(ledger, balance); // bất biến bị phá
    }

    [RequiresDatabaseFact]
    public async Task Purchase_WithUserRowLock_SameScenarioKeepsInvariant()
    {
        var user = await SeedUserAsync(60);

        var outcomes = await Task.WhenAll(
            PurchaseWithLockAsync(user.Id, "hair.variant03"),
            PurchaseWithLockAsync(user.Id, "hair.variant04"));

        Assert.Single(outcomes, e => e is null);
        Assert.Single(outcomes, e => e is ConflictException { Code: "insufficient_coins" });
        await using var db = fixture.CreateContext();
        var balance = (await db.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id)).CoinBalance;
        Assert.Equal(10, balance);
        Assert.Equal(balance, await db.CoinTransactions.Where(c => c.UserId == user.Id).SumAsync(c => c.Amount));
    }

    private async Task<Exception?> PurchaseWithoutLockAsync(Guid userId, string itemKey, Barrier barrier)
    {
        await Task.Yield();
        await using var db = fixture.CreateContext();
        var repository = new AvatarRepository(db)
        {
            UseUserRowLock = false,
            BeforeBalanceWrite = () =>
            {
                // Quá hạn thì ném lỗi chứ không treo test.
                if (!barrier.SignalAndWait(TimeSpan.FromSeconds(20)))
                {
                    throw new TimeoutException("Hai giao dịch không gặp nhau ở barrier.");
                }

                return Task.CompletedTask;
            },
        };
        return await TryPurchaseAsync(new ShopService(repository, TimeProvider.System), userId, itemKey);
    }

    private async Task<Exception?> PurchaseWithLockAsync(Guid userId, string itemKey)
    {
        await Task.Yield();
        await using var db = fixture.CreateContext();
        return await TryPurchaseAsync(new ShopService(new AvatarRepository(db), TimeProvider.System), userId, itemKey);
    }

    private static async Task<Exception?> TryPurchaseAsync(ShopService service, Guid userId, string itemKey)
    {
        try
        {
            await service.PurchaseAsync(userId, itemKey, CancellationToken.None);
            return null;
        }
        catch (ConflictException ex)
        {
            return ex;
        }
    }

    private async Task<User> SeedUserAsync(int balance)
    {
        var id = Guid.NewGuid();
        var user = new User { Id = id, Email = $"{id:N}@test.local", PasswordHash = "h", DisplayName = "Mua", CreatedAt = Now, CoinBalance = balance };
        await using var db = fixture.CreateContext();
        db.Users.Add(user);
        db.CoinTransactions.Add(new CoinTransaction
        {
            Id = Guid.NewGuid(), UserId = id, Amount = balance, Reason = CoinTransactionReason.MatchReward, CreatedAt = Now,
        });
        await db.SaveChangesAsync();
        return user;
    }
}
