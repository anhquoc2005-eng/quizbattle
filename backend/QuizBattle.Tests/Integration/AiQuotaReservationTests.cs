using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using QuizBattle.Application.AiQuota;
using QuizBattle.Infrastructure.Persistence;

namespace QuizBattle.Tests.Integration;

/// <summary>
/// Câu lệnh giữ chỗ/hoàn lượt AI chạy trên Postgres THẬT: tính nguyên tử khi đồng thời, rollback hai dòng, hoàn đúng ngày,
/// không deadlock. (Fake repository không kiểm được hành vi khóa dòng và ON CONFLICT.)
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class AiQuotaReservationTests(DatabaseFixture fixture)
{
    private const int Unlimited = 100_000;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    private static int dayCounter = Random.Shared.Next(0, 100_000);

    // Mỗi test một ngày riêng (rất xa trong tương lai) vì dòng "global" dùng chung cho cả database test.
    private static DateOnly UniqueDay() => new DateOnly(3000, 1, 1).AddDays(10 * Interlocked.Increment(ref dayCounter)); // bước 10 để test dùng day+1 không đụng ngày của test khác

    [RequiresDatabaseFact]
    public async Task Reserve_FirstCallOfDay_CreatesGlobalAndUserRowsWithCount1()
    {
        var day = UniqueDay();
        var userId = Guid.NewGuid();

        var result = await ReserveAsync(userId, day, userLimit: 5, globalLimit: 5);

        Assert.True(result.Granted);
        Assert.Equal(day, result.Day);
        Assert.Equal(1, result.UserCount);
        Assert.Equal(1, await CountAsync(day, AiQuotaScopes.Global));
        Assert.Equal(1, await CountAsync(day, AiQuotaScopes.ForUser(userId)));
    }

    [RequiresDatabaseFact]
    public async Task Reserve_UntilLimit_GrantsEveryCallThenRejectsWithUser()
    {
        var day = UniqueDay();
        var userId = Guid.NewGuid();

        for (var i = 1; i <= 3; i++)
        {
            var granted = await ReserveAsync(userId, day, userLimit: 3, globalLimit: Unlimited);
            Assert.True(granted.Granted);
            Assert.Equal(i, granted.UserCount);
        }

        var rejected = await ReserveAsync(userId, day, userLimit: 3, globalLimit: Unlimited);

        Assert.False(rejected.Granted);
        Assert.Equal(AiLimitKind.User, rejected.RejectedBy);
    }

    [RequiresDatabaseFact]
    public async Task Reserve_AtLimit_DoesNotChangeAnyRow()
    {
        var day = UniqueDay();
        var userId = Guid.NewGuid();
        await ReserveAsync(userId, day, userLimit: 1, globalLimit: Unlimited);

        await ReserveAsync(userId, day, userLimit: 1, globalLimit: Unlimited);

        Assert.Equal(1, await CountAsync(day, AiQuotaScopes.ForUser(userId)));
        Assert.Equal(1, await CountAsync(day, AiQuotaScopes.Global));
    }

    [RequiresDatabaseFact]
    public async Task Reserve_DifferentDays_CountSeparately()
    {
        var day = UniqueDay();
        var nextDay = day.AddDays(1);
        var userId = Guid.NewGuid();

        await ReserveAsync(userId, day, userLimit: 1, globalLimit: Unlimited);
        var onNextDay = await ReserveAsync(userId, nextDay, userLimit: 1, globalLimit: Unlimited);

        Assert.True(onNextDay.Granted);
        Assert.Equal(1, await CountAsync(day, AiQuotaScopes.ForUser(userId)));
        Assert.Equal(1, await CountAsync(nextDay, AiQuotaScopes.ForUser(userId)));
    }

    [RequiresDatabaseFact]
    public async Task Reserve_ParallelCallsOverUserLimit_GrantsExactlyLimit()
    {
        const int limit = 5;
        const int attempts = 30;
        var day = UniqueDay();
        var userId = Guid.NewGuid();

        var results = await RunInParallelAsync(attempts, _ => ReserveAsync(userId, day, userLimit: limit, globalLimit: Unlimited));

        Assert.Equal(limit, results.Count(r => r.Granted));
        Assert.Equal(attempts - limit, results.Count(r => r.RejectedBy == AiLimitKind.User));
        Assert.Equal(limit, await CountAsync(day, AiQuotaScopes.ForUser(userId)));
        Assert.Equal(limit, await CountAsync(day, AiQuotaScopes.Global));
    }

    [RequiresDatabaseFact]
    public async Task Reserve_ManyUsersOverGlobalLimit_GrantsExactlyGlobalLimit()
    {
        const int limit = 5;
        const int users = 20;
        var day = UniqueDay();
        var userIds = Enumerable.Range(0, users).Select(_ => Guid.NewGuid()).ToArray();

        var results = await RunInParallelAsync(users, i => ReserveAsync(userIds[i], day, userLimit: Unlimited, globalLimit: limit));

        Assert.Equal(limit, results.Count(r => r.Granted));
        Assert.Equal(users - limit, results.Count(r => r.RejectedBy == AiLimitKind.Global));
        Assert.Equal(limit, await CountAsync(day, AiQuotaScopes.Global));

        // Không user nào bị trừ lượt mà không có lượt global tương ứng: tổng các dòng user = số lượt được cấp.
        var grantedUsers = userIds.Where((_, i) => results[i].Granted).ToHashSet();
        foreach (var userId in userIds)
        {
            var expected = grantedUsers.Contains(userId) ? 1 : 0;
            Assert.Equal(expected, await FindCountAsync(day, AiQuotaScopes.ForUser(userId)) ?? 0);
        }
    }

    [RequiresDatabaseFact]
    public async Task Reserve_UserAtLimitGlobalHasRoom_RollsBackGlobalIncrement()
    {
        var day = UniqueDay();
        var userId = Guid.NewGuid();
        await ReserveAsync(userId, day, userLimit: 2, globalLimit: Unlimited);
        await ReserveAsync(userId, day, userLimit: 2, globalLimit: Unlimited);

        var rejected = await ReserveAsync(userId, day, userLimit: 2, globalLimit: Unlimited);

        Assert.Equal(AiLimitKind.User, rejected.RejectedBy);
        Assert.Equal(2, await CountAsync(day, AiQuotaScopes.Global)); // lần tăng global của request bị từ chối đã được hoàn tác
        Assert.Equal(2, await CountAsync(day, AiQuotaScopes.ForUser(userId)));
    }

    [RequiresDatabaseFact]
    public async Task Reserve_GlobalAtLimitUserHasRoom_DoesNotTouchUserRow()
    {
        var day = UniqueDay();
        await ReserveAsync(Guid.NewGuid(), day, userLimit: Unlimited, globalLimit: 1);
        var newcomer = Guid.NewGuid();

        var rejected = await ReserveAsync(newcomer, day, userLimit: Unlimited, globalLimit: 1);

        Assert.Equal(AiLimitKind.Global, rejected.RejectedBy);
        Assert.Null(await FindCountAsync(day, AiQuotaScopes.ForUser(newcomer)));
        Assert.Equal(1, await CountAsync(day, AiQuotaScopes.Global));
    }

    [RequiresDatabaseFact]
    public async Task Reserve_BothAtLimit_RejectsAsGlobal()
    {
        var day = UniqueDay();
        var userId = Guid.NewGuid();
        await ReserveAsync(userId, day, userLimit: 1, globalLimit: 1);

        var rejected = await ReserveAsync(userId, day, userLimit: 1, globalLimit: 1);

        Assert.Equal(AiLimitKind.Global, rejected.RejectedBy); // global được kiểm trước
    }

    [RequiresDatabaseFact]
    public async Task Refund_AfterReserve_DecrementsUserRowOnly()
    {
        var day = UniqueDay();
        var userId = Guid.NewGuid();
        await ReserveAsync(userId, day, userLimit: 5, globalLimit: Unlimited);
        await ReserveAsync(userId, day, userLimit: 5, globalLimit: Unlimited);

        await RefundAsync(userId, day);

        Assert.Equal(1, await CountAsync(day, AiQuotaScopes.ForUser(userId)));
        Assert.Equal(2, await CountAsync(day, AiQuotaScopes.Global)); // global KHÔNG bao giờ được hoàn
    }

    [RequiresDatabaseFact]
    public async Task Refund_WhenCountIsZero_StaysZero()
    {
        var day = UniqueDay();
        var userId = Guid.NewGuid();
        await ReserveAsync(userId, day, userLimit: 5, globalLimit: Unlimited);
        await RefundAsync(userId, day);

        await RefundAsync(userId, day);

        Assert.Equal(0, await CountAsync(day, AiQuotaScopes.ForUser(userId)));
    }

    [RequiresDatabaseFact]
    public async Task Refund_UsesGivenDay_NotCurrentDay()
    {
        var day = UniqueDay();
        var nextDay = day.AddDays(1);
        var userId = Guid.NewGuid();
        await ReserveAsync(userId, day, userLimit: 5, globalLimit: Unlimited);
        await ReserveAsync(userId, nextDay, userLimit: 5, globalLimit: Unlimited);

        await RefundAsync(userId, day); // giữ chỗ ở ngày cũ, hoàn sau khi đã sang ngày mới

        Assert.Equal(0, await CountAsync(day, AiQuotaScopes.ForUser(userId)));
        Assert.Equal(1, await CountAsync(nextDay, AiQuotaScopes.ForUser(userId)));
    }

    [RequiresDatabaseFact]
    public async Task Refund_WhenNoRowExists_DoesNotCreateOne()
    {
        var day = UniqueDay();
        var userId = Guid.NewGuid();

        await RefundAsync(userId, day);

        Assert.Null(await FindCountAsync(day, AiQuotaScopes.ForUser(userId)));
    }

    [RequiresDatabaseFact]
    public async Task ReserveAndRefund_InterleavedInParallel_NoDeadlockAndCorrectFinalCounts()
    {
        const int users = 6;
        const int workersPerUser = 4;
        const int iterations = 10;
        var day = UniqueDay();
        var userIds = Enumerable.Range(0, users).Select(_ => Guid.NewGuid()).ToArray();
        var granted = new int[users];
        var refunded = new int[users];

        // Mỗi worker lặp: giữ chỗ, và cứ lần giữ chỗ thành công thứ chẵn thì hoàn. Nhiều worker cùng một user và cùng dòng global.
        // Deadlock sẽ ném PostgresException 40P01 (hoặc quá timeout) làm test đỏ.
        var work = RunInParallelAsync(users * workersPerUser, async worker =>
        {
            var u = worker % users;
            for (var i = 0; i < iterations; i++)
            {
                var result = await ReserveAsync(userIds[u], day, userLimit: 15, globalLimit: Unlimited);
                if (result.Granted && Interlocked.Increment(ref granted[u]) % 2 == 0)
                {
                    await RefundAsync(userIds[u], result.Day);
                    Interlocked.Increment(ref refunded[u]);
                }
            }

            return 0;
        });
        await work.WaitAsync(Timeout);

        Assert.Equal(granted.Sum(), await CountAsync(day, AiQuotaScopes.Global));
        for (var u = 0; u < users; u++)
        {
            Assert.Equal(granted[u] - refunded[u], await CountAsync(day, AiQuotaScopes.ForUser(userIds[u])));
        }
    }

    // ---- helpers ----

    private async Task<AiReserveResult> ReserveAsync(Guid userId, DateOnly day, int userLimit, int globalLimit)
    {
        await using var db = fixture.CreateContext();
        return await new AiQuotaRepository(db, NullLogger<AiQuotaRepository>.Instance).ReserveAsync(userId, day, userLimit, globalLimit, CancellationToken.None);
    }

    private async Task RefundAsync(Guid userId, DateOnly day)
    {
        await using var db = fixture.CreateContext();
        await new AiQuotaRepository(db, NullLogger<AiQuotaRepository>.Instance).RefundUserAsync(userId, day, CancellationToken.None);
    }

    private async Task<int?> FindCountAsync(DateOnly day, string scope)
    {
        await using var db = fixture.CreateContext();
        return await db.AiUsages.AsNoTracking().Where(a => a.Day == day && a.Scope == scope).Select(a => (int?)a.Count).SingleOrDefaultAsync();
    }

    private async Task<int> CountAsync(DateOnly day, string scope) =>
        await FindCountAsync(day, scope) ?? throw new Xunit.Sdk.XunitException($"Không có dòng {scope} ngày {day}.");

    // Mọi tác vụ chờ cùng một cổng rồi mới chạy, để thật sự tranh nhau (mỗi tác vụ một DbContext/connection riêng).
    private static async Task<T[]> RunInParallelAsync<T>(int count, Func<int, Task<T>> action)
    {
        var gate = new TaskCompletionSource();
        var tasks = Enumerable.Range(0, count).Select(i => Task.Run(async () =>
        {
            await gate.Task;
            return await action(i);
        })).ToArray();
        gate.SetResult();
        return await Task.WhenAll(tasks).WaitAsync(Timeout);
    }
}
