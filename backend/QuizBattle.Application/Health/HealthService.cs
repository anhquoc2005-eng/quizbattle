using QuizBattle.Application.Abstractions;

namespace QuizBattle.Application.Health;

public sealed class HealthService(IDatabaseHealthCheck databaseHealthCheck) : IHealthService
{
    public async Task<HealthResponse> CheckAsync(CancellationToken cancellationToken)
    {
        var canConnect = await databaseHealthCheck.CanConnectAsync(cancellationToken);

        return canConnect
            ? new HealthResponse(HealthResponse.StatusOk, HealthResponse.DatabaseUp)
            : new HealthResponse(HealthResponse.StatusDegraded, HealthResponse.DatabaseDown);
    }
}
