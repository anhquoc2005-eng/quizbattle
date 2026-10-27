using QuizBattle.Application.Abstractions;

namespace QuizBattle.Infrastructure.Persistence;

public sealed class DatabaseHealthCheck(AppDbContext dbContext) : IDatabaseHealthCheck
{
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken) =>
        dbContext.Database.CanConnectAsync(cancellationToken);
}
