using QuizBattle.Application.AiQuota;

namespace QuizBattle.Application.Common;

/// <summary>Hết lượt generate AI trong ngày (của user hoặc của cả hệ thống). Map sang 429 kèm code và resetAt.</summary>
public sealed class AiDailyLimitException(AiLimitKind kind, DateTimeOffset resetAt) : Exception(
    kind == AiLimitKind.Global ? "Hệ thống AI hôm nay đã hết lượt." : "Bạn đã dùng hết lượt AI hôm nay.")
{
    public AiLimitKind Kind { get; } = kind;

    /// <summary>00:00 giờ Việt Nam của ngày kế tiếp, lúc bộ đếm được làm mới.</summary>
    public DateTimeOffset ResetAt { get; } = resetAt;

    public string Code => Kind == AiLimitKind.Global ? "AI_DAILY_LIMIT_GLOBAL" : "AI_DAILY_LIMIT_USER";
}
