namespace QuizBattle.Application.AiQuota;

public interface IAiQuotaRepository
{
    /// <summary>
    /// Giữ chỗ một lượt trong MỘT transaction: dòng global trước, dòng user sau, mỗi dòng bằng một câu lệnh nguyên tử
    /// "tăng nếu Count &lt; trần". Dòng nào chạm trần thì rollback cả hai. Commit xong mới trả về (trước khi gọi AI).
    /// </summary>
    Task<AiReserveResult> ReserveAsync(Guid userId, DateOnly day, int userLimit, int globalLimit, CancellationToken cancellationToken);

    /// <summary>
    /// Hoàn một lượt cho dòng USER của đúng <paramref name="day"/> đã giữ chỗ. Cố ý không có hàm hoàn dòng global:
    /// trần global đo số lần đã thực sự gọi AI, lần thất bại vẫn tốn quota.
    /// </summary>
    Task RefundUserAsync(Guid userId, DateOnly day, CancellationToken cancellationToken);

    Task<(int UserCount, int GlobalCount)> GetUsageAsync(Guid userId, DateOnly day, CancellationToken cancellationToken);
}
