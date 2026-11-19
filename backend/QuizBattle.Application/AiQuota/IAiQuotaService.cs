namespace QuizBattle.Application.AiQuota;

/// <summary>Một lượt đã giữ chỗ. Day là ngày VN lúc giữ chỗ - hoàn lượt dùng đúng ngày này, không tính lại.</summary>
public sealed record AiReservation(Guid UserId, DateOnly Day);

/// <summary>Số lượt AI còn lại hôm nay của một user (không lộ con số global, chỉ cho biết đã hết hay chưa).</summary>
public sealed record AiQuotaResponse(int DailyLimit, int Used, int Remaining, bool GlobalExhausted, DateTimeOffset ResetAt);

public interface IAiQuotaService
{
    /// <summary>Giữ chỗ một lượt trước khi gọi AI. Ném AiDailyLimitException (429) khi user hoặc hệ thống đã hết lượt.</summary>
    Task<AiReservation> ReserveAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Hoàn lượt của user khi sinh thất bại hoàn toàn. Không bao giờ hoàn lượt global.</summary>
    Task RefundAsync(AiReservation reservation, CancellationToken cancellationToken);

    Task<AiQuotaResponse> GetStatusAsync(Guid userId, CancellationToken cancellationToken);
}
