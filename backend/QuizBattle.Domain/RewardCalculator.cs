using QuizBattle.Domain.Enums;

namespace QuizBattle.Domain;

/// <summary>Dữ liệu của một người chơi trong trận; UserId null nghĩa là khách.</summary>
public sealed record RewardParticipant(Guid PlayerId, Guid? UserId, int Score, int AnsweredCount);

/// <summary>
/// Kết quả thưởng của một người chơi. Khách luôn <see cref="RewardStatus.NotAwarded"/> với
/// <c>DenialReason = null</c> (không thuộc enum; tầng trên nhận biết khách qua UserId).
/// Tổng Xu nhận được = <c>Coins + RankBonus</c>.
/// </summary>
public sealed record RewardResult(
    Guid PlayerId,
    int RewardRank,
    RewardStatus Status,
    RewardDenialReason? DenialReason,
    int Xp,
    int Coins,
    int RankBonus);

/// <summary>Luật thưởng XP/Xu cho một trận - hàm thuần, không phụ thuộc DB hay đồng hồ.</summary>
public static class RewardCalculator
{
    private const int MinAccountsForValidMatch = 2;

    public static IReadOnlyList<RewardResult> Calculate(IReadOnlyList<RewardParticipant> participants, Guid hostUserId)
    {
        var validMatch = participants
            .Where(p => IsEligible(p, hostUserId))
            .Select(p => p.UserId)
            .Distinct()
            .Count() >= MinAccountsForValidMatch;

        return participants.Select(p =>
        {
            // Competition ranking (1, 1, 3): 1 + số người có điểm cao hơn, tính cả khách và chủ phòng.
            var rank = 1 + participants.Count(o => o.Score > p.Score);

            if (IsEligible(p, hostUserId) && validMatch)
            {
                return new RewardResult(
                    p.PlayerId, rank, RewardStatus.Awarded, null,
                    Xp: p.Score / 10,
                    Coins: p.Score / 100,
                    RankBonus: RankBonus(rank, p.Score));
            }

            return new RewardResult(p.PlayerId, rank, RewardStatus.NotAwarded, DenialReason(p, hostUserId), 0, 0, 0);
        }).ToList();
    }

    private static bool IsEligible(RewardParticipant p, Guid hostUserId) =>
        p.UserId is { } userId && userId != hostUserId && p.AnsweredCount >= 1;

    // Thứ tự ưu tiên: Host > NoAnswers > NotEnoughAccounts. Khách: null.
    private static RewardDenialReason? DenialReason(RewardParticipant p, Guid hostUserId)
    {
        if (p.UserId is not { } userId)
        {
            return null;
        }

        if (userId == hostUserId)
        {
            return RewardDenialReason.Host;
        }

        return p.AnsweredCount < 1 ? RewardDenialReason.NoAnswers : RewardDenialReason.NotEnoughAccounts;
    }

    private static int RankBonus(int rank, int score) =>
        score <= 0 ? 0 : rank switch { 1 => 30, 2 => 20, 3 => 10, _ => 0 };
}
