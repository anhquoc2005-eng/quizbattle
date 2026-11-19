using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using QuizBattle.Application.AiQuota;
using QuizBattle.Application.Common;
using QuizBattle.Application.Questions;
using QuizBattle.Domain.AiQuota;
using QuizBattle.Domain.Entities;
using QuizBattle.Infrastructure.Persistence;
using QuizBattle.Tests.Questions;

namespace QuizBattle.Tests.Integration;

/// <summary>
/// Luồng đầy đủ QuestionService.GenerateAsync + AiQuotaService + AiQuotaRepository thật trên Postgres, với đồng hồ giả.
/// Kiểm đếm dòng trong DB sau mỗi tình huống: hoàn đúng dòng user/đúng ngày, không hoàn global, hủy request không hoàn.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class AiQuotaGenerateFlowTests(DatabaseFixture fixture)
{
    private static readonly GenerateQuestionsRequest Five = new() { Count = 5 };
    private static int dayCounter = Random.Shared.Next(0, 100_000);

    // 16:59:59Z = 23:59:59 giờ VN. Mỗi test một ngày riêng (năm 4000+) vì dòng "global" dùng chung cả database test.
    private static DateTimeOffset UniqueInstant() =>
        new DateTimeOffset(4000, 1, 1, 16, 59, 59, TimeSpan.Zero).AddDays(10 * Interlocked.Increment(ref dayCounter));

    [RequiresDatabaseFact]
    public async Task GenerateFlow_GeminiBadGateway_RefundsUserRowButNotGlobal()
    {
        var clock = new MutableTimeProvider(UniqueInstant());
        var (service, room) = CreateService(clock, new QuestionServiceTests.FakeQuestionGenerator(throwFormatErrorTimes: 2), out var db);
        await using var _ = db;

        await Assert.ThrowsAsync<BadGatewayException>(() => service.GenerateAsync(room.HostUserId, room.Code, Five, CancellationToken.None));

        var day = VietnamDay.DayOf(clock.GetUtcNow());
        Assert.Equal(0, await CountAsync(day, AiQuotaScopes.ForUser(room.HostUserId))); // đã hoàn
        Assert.Equal(1, await CountAsync(day, AiQuotaScopes.Global)); // global KHÔNG hoàn
    }

    [RequiresDatabaseFact]
    public async Task GenerateFlow_RequestCancelled_KeepsBothCounts()
    {
        var clock = new MutableTimeProvider(UniqueInstant());
        using var cts = new CancellationTokenSource();
        var (service, room) = CreateService(clock, new CancellingGenerator(cts), out var db);
        await using var _ = db;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GenerateAsync(room.HostUserId, room.Code, Five, cts.Token));

        var day = VietnamDay.DayOf(clock.GetUtcNow());
        Assert.Equal(1, await CountAsync(day, AiQuotaScopes.ForUser(room.HostUserId)));
        Assert.Equal(1, await CountAsync(day, AiQuotaScopes.Global));
    }

    [RequiresDatabaseFact]
    public async Task GenerateFlow_ClientCancelledDuringRealGeminiCall_DoesNotRefund()
    {
        // GeminiQuestionGenerator THẬT; chỉ HttpMessageHandler là giả, chờ đến khi token bị hủy rồi ném TaskCanceledException
        // đúng như HttpClient khi RequestAborted. Nếu generator bọc nó thành AiUnavailableException thì lượt bị hoàn (lỗ hổng).
        var clock = new MutableTimeProvider(UniqueInstant());
        using var cts = new CancellationTokenSource();
        var handler = new GeminiQuestionGeneratorCancellationTests.FakeHandler(async (_, token) =>
        {
            await cts.CancelAsync(); // client đóng tab trong lúc request tới Gemini đang chờ
            await Task.Delay(Timeout.Infinite, token);
            return new HttpResponseMessage();
        });
        var generator = GeminiQuestionGeneratorCancellationTests.CreateGenerator(handler);
        var (service, room) = CreateService(clock, generator, out var db);
        await using var _ = db;

        var ex = await Record.ExceptionAsync(() => service.GenerateAsync(room.HostUserId, room.Code, Five, cts.Token));

        Assert.IsAssignableFrom<OperationCanceledException>(ex);
        var day = VietnamDay.DayOf(clock.GetUtcNow());
        Assert.Equal(1, await CountAsync(day, AiQuotaScopes.ForUser(room.HostUserId)));
        Assert.Equal(1, await CountAsync(day, AiQuotaScopes.Global));
    }

    [RequiresDatabaseFact]
    public async Task GenerateFlow_GeminiTimeoutWithTokenAlive_RefundsUserRow()
    {
        // Đối chứng với test trên: timeout của HttpClient (token CHƯA hủy) thì là lỗi AI thật → hoàn lượt user.
        var clock = new MutableTimeProvider(UniqueInstant());
        var handler = new GeminiQuestionGeneratorCancellationTests.FakeHandler((_, _) => throw new TaskCanceledException("timeout"));
        var (service, room) = CreateService(clock, GeminiQuestionGeneratorCancellationTests.CreateGenerator(handler), out var db);
        await using var _ = db;

        await Assert.ThrowsAsync<AiUnavailableException>(() => service.GenerateAsync(room.HostUserId, room.Code, Five, CancellationToken.None));

        var day = VietnamDay.DayOf(clock.GetUtcNow());
        Assert.Equal(0, await CountAsync(day, AiQuotaScopes.ForUser(room.HostUserId)));
        Assert.Equal(1, await CountAsync(day, AiQuotaScopes.Global));
    }

    [RequiresDatabaseFact]
    public async Task GenerateFlow_ReservedAt235959Vietnam_GeminiFailsAfterMidnight_RefundsOldDayRow()
    {
        var clock = new MutableTimeProvider(UniqueInstant()); // 23:59:59 giờ VN
        var oldDay = VietnamDay.DayOf(clock.GetUtcNow());
        var generator = new ClockAdvancingFailingGenerator(clock, TimeSpan.FromSeconds(2)); // sang 00:00:01 ngày mới rồi mới lỗi
        var (service, room) = CreateService(clock, generator, out var db);
        await using var _ = db;

        await Assert.ThrowsAsync<AiUnavailableException>(() => service.GenerateAsync(room.HostUserId, room.Code, Five, CancellationToken.None));

        var newDay = VietnamDay.DayOf(clock.GetUtcNow());
        Assert.Equal(oldDay.AddDays(1), newDay);
        Assert.Equal(0, await CountAsync(oldDay, AiQuotaScopes.ForUser(room.HostUserId))); // hoàn đúng dòng ngày cũ
        Assert.Null(await FindCountAsync(newDay, AiQuotaScopes.ForUser(room.HostUserId))); // ngày mới không bị tạo/cộng bù
    }

    [RequiresDatabaseFact]
    public async Task GenerateFlow_ReserveAt000000Vietnam_UsesNewDayRow()
    {
        var at235959 = UniqueInstant();
        var clock = new MutableTimeProvider(at235959.AddSeconds(1)); // 00:00:00 giờ VN của ngày kế tiếp
        var (service, room) = CreateService(clock, new QuestionServiceTests.FakeQuestionGenerator(validCandidates: [new("Câu 1", ["A", "B", "C", "D"], 0)]), out var db);
        await using var _ = db;

        await service.GenerateAsync(room.HostUserId, room.Code, Five, CancellationToken.None);

        var oldDay = VietnamDay.DayOf(at235959);
        var newDay = VietnamDay.DayOf(clock.GetUtcNow());
        Assert.Equal(oldDay.AddDays(1), newDay);
        Assert.Null(await FindCountAsync(oldDay, AiQuotaScopes.ForUser(room.HostUserId)));
        Assert.Equal(1, await CountAsync(newDay, AiQuotaScopes.ForUser(room.HostUserId)));
    }

    // ---- helpers ----

    private (QuestionService Service, Room Room) CreateService(TimeProvider clock, IQuestionGenerator generator, out AppDbContext db)
    {
        db = fixture.CreateContext();
        var room = QuestionServiceTests.NewDraftRoom();
        var quota = new AiQuotaService(new AiQuotaRepository(db, NullLogger<AiQuotaRepository>.Instance), new AiQuotaOptions(20, 300), clock);
        var service = new QuestionService(new QuestionServiceTests.FakeQuestionRepository(), new QuestionServiceTests.FakeRoomRepository(room), generator, quota);
        return (service, room);
    }

    private async Task<int?> FindCountAsync(DateOnly day, string scope)
    {
        await using var db = fixture.CreateContext();
        return await db.AiUsages.AsNoTracking().Where(a => a.Day == day && a.Scope == scope).Select(a => (int?)a.Count).SingleOrDefaultAsync();
    }

    private async Task<int> CountAsync(DateOnly day, string scope) =>
        await FindCountAsync(day, scope) ?? throw new Xunit.Sdk.XunitException($"Không có dòng {scope} ngày {day}.");

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }

    /// <summary>Generator giả: hủy request rồi ném OperationCanceledException (không đi qua HttpClient).</summary>
    private sealed class CancellingGenerator(CancellationTokenSource cts) : IQuestionGenerator
    {
        public Task<IReadOnlyList<QuestionCandidate>> GenerateAsync(string topic, int count, CancellationToken cancellationToken)
        {
            cts.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<QuestionCandidate>>([]);
        }
    }

    /// <summary>Generator giả: đẩy đồng hồ tới sau nửa đêm giờ VN rồi ném lỗi AI (Gemini chậm rồi hỏng).</summary>
    private sealed class ClockAdvancingFailingGenerator(MutableTimeProvider clock, TimeSpan advance) : IQuestionGenerator
    {
        public Task<IReadOnlyList<QuestionCandidate>> GenerateAsync(string topic, int count, CancellationToken cancellationToken)
        {
            clock.Now = clock.Now.Add(advance);
            throw new AiUnavailableException("Gemini lỗi sau nửa đêm.");
        }
    }
}
