using QuizBattle.Application.AiQuota;
using QuizBattle.Application.Common;

namespace QuizBattle.Tests.TestHelpers;

/// <summary>IAiQuotaService giả: ghi lại thứ tự lời gọi, có thể bắt buộc từ chối giữ chỗ.</summary>
public sealed class FakeAiQuotaService : IAiQuotaService
{
    public List<string> Calls { get; } = [];

    public List<AiReservation> Refunds { get; } = [];

    public int ReserveCount => Calls.Count(c => c == "Reserve");

    /// <summary>Khác null thì ReserveAsync ném AiDailyLimitException loại này.</summary>
    public AiLimitKind? RejectWith { get; set; }

    public DateOnly ReservedDay { get; set; } = new(2026, 11, 19);

    public Task<AiReservation> ReserveAsync(Guid userId, CancellationToken cancellationToken)
    {
        Calls.Add("Reserve");
        if (RejectWith is { } kind)
        {
            throw new AiDailyLimitException(kind, new DateTimeOffset(2026, 11, 20, 0, 0, 0, TimeSpan.FromHours(7)));
        }

        return Task.FromResult(new AiReservation(userId, ReservedDay));
    }

    public Task RefundAsync(AiReservation reservation, CancellationToken cancellationToken)
    {
        Calls.Add("Refund");
        Refunds.Add(reservation);
        return Task.CompletedTask;
    }

    public Task<AiQuotaResponse> GetStatusAsync(Guid userId, CancellationToken cancellationToken) => throw new NotSupportedException();
}
