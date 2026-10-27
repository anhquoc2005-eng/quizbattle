using QuizBattle.Application.Abstractions;
using QuizBattle.Application.Health;

namespace QuizBattle.Tests.Health;

public sealed class HealthServiceTests
{
    [Fact]
    public async Task CheckAsync_DatabaseReachable_ReturnsOkAndUp()
    {
        var service = new HealthService(new FakeDatabaseHealthCheck(canConnect: true));

        var result = await service.CheckAsync(CancellationToken.None);

        Assert.Equal(HealthResponse.StatusOk, result.Status);
        Assert.Equal(HealthResponse.DatabaseUp, result.Database);
        Assert.True(result.IsHealthy);
    }

    [Fact]
    public async Task CheckAsync_DatabaseUnreachable_ReturnsDegradedAndDown()
    {
        var service = new HealthService(new FakeDatabaseHealthCheck(canConnect: false));

        var result = await service.CheckAsync(CancellationToken.None);

        Assert.Equal(HealthResponse.StatusDegraded, result.Status);
        Assert.Equal(HealthResponse.DatabaseDown, result.Database);
        Assert.False(result.IsHealthy);
    }

    /// <summary>Thay thế database thật để test chạy nhanh, không cần PostgreSQL.</summary>
    private sealed class FakeDatabaseHealthCheck(bool canConnect) : IDatabaseHealthCheck
    {
        public Task<bool> CanConnectAsync(CancellationToken cancellationToken) => Task.FromResult(canConnect);
    }
}
