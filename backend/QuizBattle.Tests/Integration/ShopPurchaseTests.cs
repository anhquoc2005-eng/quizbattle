using Microsoft.EntityFrameworkCore;
using QuizBattle.Application.Common;
using QuizBattle.Application.Game;
using QuizBattle.Application.Shop;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;
using QuizBattle.Infrastructure.Persistence;
using QuizBattle.Tests.TestHelpers;

namespace QuizBattle.Tests.Integration;

/// <summary>
/// Mua đồ trên Postgres thật: khóa dòng Users FOR UPDATE, không mất cập nhật, không trừ Xu hai lần, không deadlock với
/// kết thúc trận. Sau mỗi test kiểm bất biến CoinBalance = SUM(CoinTransactions.Amount).
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class ShopPurchaseTests(DatabaseFixture fixture)
{
    private const string Common1 = "hair.variant03"; // Thường 50
    private const string Common2 = "hair.variant04"; // Thường 50
    private static readonly TimeSpan DeadlockTimeout = TimeSpan.FromSeconds(30);

    private static readonly DateTimeOffset Start = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FinishAt = Start.AddSeconds(10 + Room.RESULT_DISPLAY_SECONDS + 1);

    [RequiresDatabaseFact]
    public async Task Purchase_Success_DeductsCoinsAddsOwnershipAndNegativeLedgerRow()
    {
        var user = await SeedUserAsync(100);

        var outcome = await PurchaseAsync(user.Id, Common1);

        Assert.Null(outcome.Error);
        Assert.Equal(new PurchaseResultDto(Common1, 50, 50), outcome.Result);
        await using var db = fixture.CreateContext();
        Assert.Equal(50, (await db.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id)).CoinBalance);
        var ownership = await db.UserAvatarItems.AsNoTracking().SingleAsync(o => o.UserId == user.Id);
        Assert.Equal(Common1, ownership.ItemKey);
        var purchase = await db.CoinTransactions.AsNoTracking().SingleAsync(c => c.UserId == user.Id && c.Reason == CoinTransactionReason.ShopPurchase);
        Assert.Equal(-50, purchase.Amount);
        Assert.Equal(ownership.Id, purchase.UserAvatarItemId);
        await AssertInvariantsAsync([user.Id]);
    }

    [RequiresDatabaseFact]
    public async Task Purchase_SameItemTwiceSequentially_SecondIsAlreadyOwnedAndChargedOnce()
    {
        var user = await SeedUserAsync(100);

        var first = await PurchaseAsync(user.Id, Common1);
        var second = await PurchaseAsync(user.Id, Common1);

        Assert.Null(first.Error);
        Assert.Equal("already_owned", Assert.IsType<ConflictException>(second.Error).Code);
        await AssertStateAsync(user.Id, balance: 50, ownerships: 1, purchaseRows: 1);
    }

    [RequiresDatabaseFact]
    public async Task Purchase_InsufficientCoins_BalanceAndOwnershipUnchanged()
    {
        var user = await SeedUserAsync(49);

        var outcome = await PurchaseAsync(user.Id, Common1);

        Assert.Equal("insufficient_coins", Assert.IsType<ConflictException>(outcome.Error).Code);
        await AssertStateAsync(user.Id, balance: 49, ownerships: 0, purchaseRows: 0);
    }

    [RequiresDatabaseFact]
    public async Task Purchase_FreeOrUnknownItem_DoesNotTouchBalance()
    {
        var user = await SeedUserAsync(100);

        var free = await PurchaseAsync(user.Id, "hair.variant01");
        var unknown = await PurchaseAsync(user.Id, "hair.variant99");

        Assert.Equal("item_not_purchasable", Assert.IsType<RequestValidationException>(free.Error).Code);
        Assert.Equal("item_not_found", Assert.IsType<NotFoundException>(unknown.Error).Code);
        await AssertStateAsync(user.Id, balance: 100, ownerships: 0, purchaseRows: 0);
    }

    [RequiresDatabaseFact]
    public async Task Purchase_UnknownUser_ThrowsNotFound()
    {
        var outcome = await PurchaseAsync(Guid.NewGuid(), Common1);

        Assert.IsType<NotFoundException>(outcome.Error);
    }

    [RequiresDatabaseFact]
    public async Task Purchase_SameItemConcurrently_ChargedOnce()
    {
        for (var round = 0; round < 10; round++)
        {
            var user = await SeedUserAsync(200);

            var outcomes = await RunConcurrentlyAsync(Enumerable.Range(0, 8).Select(_ => PurchaseAsync(user.Id, Common1)));

            Assert.Single(outcomes, o => o.Error is null);
            Assert.Equal(7, outcomes.Count(o => o.Error is ConflictException { Code: "already_owned" }));
            await AssertStateAsync(user.Id, balance: 150, ownerships: 1, purchaseRows: 1);
        }
    }

    [RequiresDatabaseFact]
    public async Task Purchase_TwoItemsConcurrently_CoinsForOnlyOne_NeverNegative()
    {
        for (var round = 0; round < 10; round++)
        {
            var user = await SeedUserAsync(60);

            var outcomes = await RunConcurrentlyAsync(new[] { PurchaseAsync(user.Id, Common1), PurchaseAsync(user.Id, Common2) });

            Assert.Single(outcomes, o => o.Error is null);
            Assert.Single(outcomes, o => o.Error is ConflictException { Code: "insufficient_coins" });
            await AssertStateAsync(user.Id, balance: 10, ownerships: 1, purchaseRows: 1);
        }
    }

    [RequiresDatabaseFact]
    public async Task Purchase_ConcurrentWithMatchEnd_NoLostUpdate()
    {
        for (var round = 0; round < 8; round++)
        {
            var buyer = await SeedUserAsync(100);
            var room = await SeedRoomAsync(scoreA: 800, scoreB: 600, accountA: buyer);

            var results = await RunConcurrentlyAsync(
            new[]
            {
                PurchaseAsync(buyer.Id, Common1).ContinueWith(t => (Exception?)t.Result.Error),
                TryNextQuestionAsync(room.Code, room.HostUserId),
            });

            Assert.All(results, e => Assert.Null(e));
            await AssertBalanceIsStartPlusRewardsMinusPurchasesAsync(buyer.Id, start: 100, purchases: 50);
            await AssertInvariantsAsync(room.UserIds);
        }
    }

    [RequiresDatabaseFact]
    public async Task Purchase_ConcurrentWithTwoMatchEndsForSameUser_NoLostUpdateNoDeadlock()
    {
        for (var round = 0; round < 4; round++)
        {
            var buyer = await SeedUserAsync(100);
            var room1 = await SeedRoomAsync(scoreA: 800, scoreB: 100, accountA: buyer);
            var room2 = await SeedRoomAsync(scoreA: 500, scoreB: 100, accountA: buyer);

            var results = await RunConcurrentlyAsync(
            new[]
            {
                PurchaseAsync(buyer.Id, Common1).ContinueWith(t => (Exception?)t.Result.Error),
                TryNextQuestionAsync(room1.Code, room1.HostUserId),
                TryNextQuestionAsync(room2.Code, room2.HostUserId),
            });

            Assert.All(results, e => Assert.Null(e));
            await AssertBalanceIsStartPlusRewardsMinusPurchasesAsync(buyer.Id, start: 100, purchases: 50);
            await AssertInvariantsAsync([buyer.Id]);
        }
    }

    [RequiresDatabaseFact]
    public async Task Invariant_BalanceEqualsLedger_AfterMixedOperations()
    {
        var user = await SeedUserAsync(500);

        await RunConcurrentlyAsync(
        new[]
        {
            PurchaseAsync(user.Id, Common1),
            PurchaseAsync(user.Id, Common2),
            PurchaseAsync(user.Id, "gesture.ok"),
            PurchaseAsync(user.Id, "hair.variant01"),
            PurchaseAsync(user.Id, "hair.variant99"),
            PurchaseAsync(user.Id, Common1),
        });

        await AssertStateAsync(user.Id, balance: 500 - 50 - 50 - 150, ownerships: 3, purchaseRows: 3);
    }

    // Chạy các tác vụ cùng lúc và quá hạn = nghi deadlock.
    private static async Task<T[]> RunConcurrentlyAsync<T>(IEnumerable<Task<T>> tasks) =>
        await Task.WhenAll(tasks).WaitAsync(DeadlockTimeout);

    private sealed record Outcome(PurchaseResultDto? Result, Exception? Error);

    private async Task<Outcome> PurchaseAsync(Guid userId, string itemKey)
    {
        await Task.Yield(); // bắt đầu thật sự song song với các tác vụ khác
        await using var db = fixture.CreateContext();
        var service = new ShopService(new AvatarRepository(db), TimeProvider.System);
        try
        {
            return new Outcome(await service.PurchaseAsync(userId, itemKey, CancellationToken.None), null);
        }
        catch (Exception ex) when (ex is ConflictException or NotFoundException or RequestValidationException)
        {
            return new Outcome(null, ex);
        }
    }

    private async Task AssertStateAsync(Guid userId, int balance, int ownerships, int purchaseRows)
    {
        await using var db = fixture.CreateContext();
        Assert.Equal(balance, (await db.Users.AsNoTracking().SingleAsync(u => u.Id == userId)).CoinBalance);
        Assert.Equal(ownerships, await db.UserAvatarItems.CountAsync(o => o.UserId == userId));
        Assert.Equal(purchaseRows, await db.CoinTransactions.CountAsync(c => c.UserId == userId && c.Reason == CoinTransactionReason.ShopPurchase));
        await AssertInvariantsAsync([userId]);
    }

    // Số dư cuối = đầu + mọi khoản thưởng trận − tiền mua, và không bao giờ âm.
    private async Task AssertBalanceIsStartPlusRewardsMinusPurchasesAsync(Guid userId, int start, int purchases)
    {
        await using var db = fixture.CreateContext();
        var rewards = await db.MatchRewards.Where(r => r.UserId == userId && r.Status == RewardStatus.Awarded).SumAsync(r => r.Coins + r.RankBonus);
        var balance = (await db.Users.AsNoTracking().SingleAsync(u => u.Id == userId)).CoinBalance;

        Assert.True(rewards > 0, "trận đã kết thúc phải có thưởng");
        Assert.Equal(start + rewards - purchases, balance);
    }

    // CoinBalance == SUM(CoinTransactions.Amount), không âm, mỗi lần mua có đúng một quyền sở hữu và ngược lại.
    private async Task AssertInvariantsAsync(IEnumerable<Guid> userIds)
    {
        await using var db = fixture.CreateContext();
        foreach (var userId in userIds)
        {
            var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == userId);
            var ledger = await db.CoinTransactions.Where(c => c.UserId == userId).SumAsync(c => c.Amount);
            Assert.Equal(ledger, user.CoinBalance);
            Assert.True(user.CoinBalance >= 0);
            Assert.Equal(
                await db.UserAvatarItems.CountAsync(o => o.UserId == userId),
                await db.CoinTransactions.CountAsync(c => c.UserId == userId && c.Reason == CoinTransactionReason.ShopPurchase));
        }
    }

    // Người dùng có số dư khởi đầu đi kèm một dòng sổ cái cùng số tiền (giữ bất biến ngay từ đầu).
    private async Task<User> SeedUserAsync(int balance)
    {
        var id = Guid.NewGuid();
        var user = new User { Id = id, Email = $"{id:N}@test.local", PasswordHash = "h", DisplayName = "Mua", CreatedAt = Start, CoinBalance = balance };
        await using var db = fixture.CreateContext();
        db.Users.Add(user);
        if (balance > 0)
        {
            db.CoinTransactions.Add(new CoinTransaction
            {
                Id = Guid.NewGuid(), UserId = id, Amount = balance, Reason = CoinTransactionReason.MatchReward, CreatedAt = Start,
            });
        }

        await db.SaveChangesAsync();
        return user;
    }

    private async Task<User> SeedPlainUserAsync(string name)
    {
        var id = Guid.NewGuid();
        var user = new User { Id = id, Email = $"{name}-{id:N}@test.local", PasswordHash = "h", DisplayName = name, CreatedAt = Start };
        await using var db = fixture.CreateContext();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private async Task<Exception?> TryNextQuestionAsync(string code, Guid hostUserId)
    {
        await Task.Yield();
        await using var db = fixture.CreateContext();
        var service = new GameService(new GameRepository(db), new UnusedTokenGenerator(), new FakeTimeProvider(FinishAt));
        try
        {
            await service.NextQuestionAsync(hostUserId, code, CancellationToken.None);
            return null;
        }
        catch (ConflictException ex)
        {
            return ex;
        }
    }

    private sealed record SeededRoom(string Code, Guid HostUserId, Guid[] UserIds);

    // Phòng InProgress còn đúng một câu đã kết thúc; hai tài khoản đã trả lời (accountA là người mua).
    private async Task<SeededRoom> SeedRoomAsync(int scoreA, int scoreB, User accountA)
    {
        var host = await SeedPlainUserAsync("host");
        var b = await SeedPlainUserAsync("b");
        var room = new Room
        {
            Id = Guid.NewGuid(), Code = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(), HostUserId = host.Id,
            Topic = "Chủ đề", Status = RoomStatus.InProgress, CreatedAt = Start, StartedAt = Start,
        };
        var question = new Question
        {
            Id = Guid.NewGuid(), RoomId = room.Id, OrderIndex = 0, Text = "Câu 1", Options = ["A", "B", "C", "D"],
            CorrectIndex = 0, ReviewStatus = QuestionReviewStatus.Approved, StartedAt = Start, EndedAt = Start.AddSeconds(10),
        };
        var playerA = NewPlayer(room.Id, accountA.Id, "A", scoreA);
        var playerB = NewPlayer(room.Id, b.Id, "B", scoreB);

        await using var db = fixture.CreateContext();
        db.Rooms.Add(room);
        db.Questions.Add(question);
        db.Players.AddRange(playerA, playerB);
        db.Answers.AddRange(NewAnswer(playerA, question, scoreA), NewAnswer(playerB, question, scoreB));
        await db.SaveChangesAsync();
        return new SeededRoom(room.Code, host.Id, [accountA.Id, b.Id]);
    }

    private static Player NewPlayer(Guid roomId, Guid userId, string name, int score) => new()
    {
        Id = Guid.NewGuid(), RoomId = roomId, UserId = userId, DisplayName = name, SessionTokenHash = Guid.NewGuid().ToString("N"),
        Score = score, JoinedAt = Start,
    };

    private static Answer NewAnswer(Player player, Question question, int points) => new()
    {
        Id = Guid.NewGuid(), PlayerId = player.Id, QuestionId = question.Id, SelectedIndex = 0,
        AnsweredAt = Start.AddSeconds(2), IsCorrect = points > 0, PointsAwarded = points,
    };

    private sealed class UnusedTokenGenerator : ISessionTokenGenerator
    {
        public string Generate() => throw new NotSupportedException();
    }
}
