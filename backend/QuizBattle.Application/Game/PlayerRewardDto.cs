namespace QuizBattle.Application.Game;

/// <summary>
/// Thưởng của một người chơi (mỗi người chơi trong phòng có một phần tử, kể cả khách). NotAwardedReason:
/// "guest" | "host" | "no_answers" | "not_enough_accounts"; NoRankBonusReason: "zero_score" khi điểm 0 mà hạng ≤ 3.
/// TotalXp/LevelBefore/LevelAfter/LeveledUp chỉ có giá trị khi Awarded. Tổng Xu nhận được = Coins + RankBonus.
/// </summary>
public sealed record PlayerRewardDto(
    Guid PlayerId,
    bool Awarded,
    string? NotAwardedReason,
    int RewardRank,
    int Score,
    int Xp,
    int Coins,
    int RankBonus,
    string? NoRankBonusReason,
    int? TotalXp,
    int? LevelBefore,
    int? LevelAfter,
    bool? LeveledUp);
