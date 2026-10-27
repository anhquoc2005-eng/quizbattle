namespace QuizBattle.Application.Health;

public interface IHealthService
{
    Task<HealthResponse> CheckAsync(CancellationToken cancellationToken);
}
