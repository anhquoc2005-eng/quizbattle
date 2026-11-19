using QuizBattle.Application.AiQuota;
using QuizBattle.Application.Common;
using QuizBattle.Tests.TestHelpers;

namespace QuizBattle.Tests.AiQuota;

public sealed class AiQuotaOptionsAndServiceTests
{
    private static readonly DateTimeOffset Noon = new(2026, 11, 19, 5, 0, 0, TimeSpan.Zero); // 12:00 ngày 19 giờ VN

    [Fact]
    public void Parse_NothingSet_UsesDefaults() =>
        Assert.Equal(new AiQuotaOptions(20, 300), AiQuotaOptions.Parse(null, ""));

    [Fact]
    public void Parse_ValidValues_UsesThem() =>
        Assert.Equal(new AiQuotaOptions(3, 5), AiQuotaOptions.Parse("3", "5"));

    [Theory]
    [InlineData("0")]
    [InlineData("-2")]
    [InlineData("abc")]
    [InlineData("1.5")]
    public void Parse_InvalidValue_Throws(string raw)
    {
        Assert.Throws<InvalidOperationException>(() => AiQuotaOptions.Parse(raw, null));
        Assert.Throws<InvalidOperationException>(() => AiQuotaOptions.Parse(null, raw));
    }

    [Fact]
    public async Task GetStatus_NothingUsed_FullRemaining()
    {
        var status = await CreateService(new FakeRepo(0, 0)).GetStatusAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(new AiQuotaResponse(20, 0, 20, false, new DateTimeOffset(2026, 11, 20, 0, 0, 0, TimeSpan.FromHours(7))), status);
    }

    [Fact]
    public async Task GetStatus_UsedAllLimit_RemainingZero()
    {
        var status = await CreateService(new FakeRepo(20, 40)).GetStatusAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(0, status.Remaining);
        Assert.False(status.GlobalExhausted);
    }

    [Fact]
    public async Task GetStatus_UsedAboveLimitAfterLoweringLimit_RemainingNeverNegative()
    {
        var status = await CreateService(new FakeRepo(25, 40)).GetStatusAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(0, status.Remaining);
    }

    [Fact]
    public async Task GetStatus_GlobalAtLimit_GlobalExhausted()
    {
        var status = await CreateService(new FakeRepo(1, 300)).GetStatusAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(status.GlobalExhausted);
        Assert.Equal(19, status.Remaining);
    }

    [Fact]
    public void AiQuotaResponse_DoesNotExposeGlobalCount()
    {
        // Chỉ biết đã hết hay chưa, không lộ mức dùng toàn hệ thống.
        var names = typeof(AiQuotaResponse).GetProperties().Select(p => p.Name).ToArray();

        Assert.DoesNotContain(names, n => n.Contains("GlobalCount", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("GlobalExhausted", names);
    }

    [Fact]
    public async Task Reserve_Rejected_ThrowsWithKindAndResetAt()
    {
        var repo = new FakeRepo(0, 0) { Reserve = AiReserveResult.Rejected(new DateOnly(2026, 11, 19), AiLimitKind.User) };

        var ex = await Assert.ThrowsAsync<AiDailyLimitException>(() => CreateService(repo).ReserveAsync(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal("AI_DAILY_LIMIT_USER", ex.Code);
        Assert.Equal(new DateTimeOffset(2026, 11, 20, 0, 0, 0, TimeSpan.FromHours(7)), ex.ResetAt);
    }

    [Fact]
    public async Task Refund_UsesReservedDayNotCurrentDay()
    {
        var repo = new FakeRepo(0, 0);
        var service = CreateService(repo);
        var reservation = new AiReservation(Guid.NewGuid(), new DateOnly(2026, 11, 18)); // giữ chỗ từ hôm qua, "bây giờ" là 19/11

        await service.RefundAsync(reservation, CancellationToken.None);

        Assert.Equal((reservation.UserId, new DateOnly(2026, 11, 18)), repo.Refunded);
    }

    private static AiQuotaService CreateService(IAiQuotaRepository repo) =>
        new(repo, new AiQuotaOptions(20, 300), new FakeTimeProvider(Noon));

    private sealed class FakeRepo(int userCount, int globalCount) : IAiQuotaRepository
    {
        public AiReserveResult? Reserve { get; init; }

        public (Guid, DateOnly)? Refunded { get; private set; }

        public Task<AiReserveResult> ReserveAsync(Guid userId, DateOnly day, int userLimit, int globalLimit, CancellationToken cancellationToken) =>
            Task.FromResult(Reserve ?? AiReserveResult.Accepted(day, 1));

        public Task RefundUserAsync(Guid userId, DateOnly day, CancellationToken cancellationToken)
        {
            Refunded = (userId, day);
            return Task.CompletedTask;
        }

        public Task<(int UserCount, int GlobalCount)> GetUsageAsync(Guid userId, DateOnly day, CancellationToken cancellationToken) =>
            Task.FromResult((userCount, globalCount));
    }
}
