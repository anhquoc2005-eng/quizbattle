using QuizBattle.Domain.Enums;

namespace QuizBattle.Domain.Entities;

/// <summary>Sổ cái Xu, chỉ thêm. Amount dương = cộng (thưởng trận), âm = trừ (mua đồ).</summary>
public sealed class CoinTransaction
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public int Amount { get; set; }

    public CoinTransactionReason Reason { get; set; }

    public Guid? RoomId { get; set; }

    public Guid? MatchRewardId { get; set; }

    /// <summary>Có giá trị khi và chỉ khi Reason = ShopPurchase: bản ghi sở hữu của món đã mua.</summary>
    public Guid? UserAvatarItemId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
