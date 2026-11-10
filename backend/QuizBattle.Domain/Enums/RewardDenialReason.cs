namespace QuizBattle.Domain.Enums;

/// <summary>Lý do một người chơi có tài khoản không nhận thưởng. Khách không có lý do này (không có dòng MatchReward).</summary>
public enum RewardDenialReason
{
    Host,
    NoAnswers,
    NotEnoughAccounts,
}
