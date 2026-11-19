namespace QuizBattle.Application.AiQuota;

public enum AiLimitKind
{
    User,
    Global,
}

/// <summary>Kết quả giữ chỗ lượt. Khi Granted, Day là ngày đã giữ chỗ - hoàn lượt phải dùng đúng ngày này.</summary>
public sealed record AiReserveResult(bool Granted, DateOnly Day, int UserCount, AiLimitKind? RejectedBy)
{
    public static AiReserveResult Accepted(DateOnly day, int userCount) => new(true, day, userCount, null);

    public static AiReserveResult Rejected(DateOnly day, AiLimitKind by) => new(false, day, 0, by);
}
