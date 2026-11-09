using QuizBattle.Domain.Enums;

namespace QuizBattle.Domain.Entities;

/// <summary>Sổ cái Xu, chỉ thêm. Amount dương = cộng, âm = trừ (mua đồ ở tính năng sau).</summary>
public sealed class CoinTransaction
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public int Amount { get; set; }

    public CoinTransactionReason Reason { get; set; }

    public Guid? RoomId { get; set; }

    public Guid? MatchRewardId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
