using QuizBattle.Application.Common;
using QuizBattle.Domain.AiQuota;

namespace QuizBattle.Application.AiQuota;

public sealed class AiQuotaService(
    IAiQuotaRepository repository,
    AiQuotaOptions options,
    TimeProvider timeProvider) : IAiQuotaService
{
    public async Task<AiReservation> ReserveAsync(Guid userId, CancellationToken cancellationToken)
    {
        var day = VietnamDay.DayOf(timeProvider.GetUtcNow());
        var result = await repository.ReserveAsync(userId, day, options.DailyLimitPerUser, options.DailyLimitGlobal, cancellationToken);
        if (!result.Granted)
        {
            throw new AiDailyLimitException(result.RejectedBy ?? AiLimitKind.User, VietnamDay.NextResetAt(day));
        }

        return new AiReservation(userId, result.Day);
    }

    // Dùng Day của lần giữ chỗ, không gọi lại TimeProvider: giữ chỗ 23:59:59 mà hoàn sau 00:00 phải trừ đúng dòng ngày cũ.
    public Task RefundAsync(AiReservation reservation, CancellationToken cancellationToken) =>
        repository.RefundUserAsync(reservation.UserId, reservation.Day, cancellationToken);

    public async Task<AiQuotaResponse> GetStatusAsync(Guid userId, CancellationToken cancellationToken)
    {
        var day = VietnamDay.DayOf(timeProvider.GetUtcNow());
        var (userCount, globalCount) = await repository.GetUsageAsync(userId, day, cancellationToken);

        return new AiQuotaResponse(
            options.DailyLimitPerUser,
            userCount,
            Math.Max(options.DailyLimitPerUser - userCount, 0), // hạ trần giữa ngày có thể làm userCount > trần
            globalCount >= options.DailyLimitGlobal,
            VietnamDay.NextResetAt(day));
    }
}
