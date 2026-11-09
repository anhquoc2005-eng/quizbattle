using QuizBattle.Domain;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Tests.Domain;

public sealed class RewardCalculatorTests
{
    private static readonly Guid HostUserId = Guid.NewGuid();

    private static RewardParticipant Account(int score, int answered = 1, Guid? userId = null) =>
        new(Guid.NewGuid(), userId ?? Guid.NewGuid(), score, answered);

    private static RewardParticipant Guest(int score, int answered = 1) =>
        new(Guid.NewGuid(), null, score, answered);

    private static RewardResult For(IReadOnlyList<RewardResult> results, RewardParticipant participant) =>
        results.Single(r => r.PlayerId == participant.PlayerId);

    // FR-004

    [Fact]
    public void Calculate_OnlyOneAnsweredAccount_NoOneRewarded()
    {
        var a = Account(800);

        var results = RewardCalculator.Calculate([a], HostUserId);

        Assert.All(results, r => Assert.Equal(RewardStatus.NotAwarded, r.Status));
        Assert.Equal(RewardDenialReason.NotEnoughAccounts, For(results, a).DenialReason);
    }

    [Fact]
    public void Calculate_TwoAccountsOneNeverAnswered_NoOneRewarded()
    {
        var a = Account(800);
        var b = Account(0, answered: 0);

        var results = RewardCalculator.Calculate([a, b], HostUserId);

        Assert.All(results, r => Assert.Equal(RewardStatus.NotAwarded, r.Status));
        Assert.Equal(RewardDenialReason.NotEnoughAccounts, For(results, a).DenialReason);
        Assert.Equal(RewardDenialReason.NoAnswers, For(results, b).DenialReason);
    }

    [Fact]
    public void Calculate_HostAndOneAccount_NotEnoughAccounts()
    {
        var host = Account(900, userId: HostUserId);
        var a = Account(800);

        var results = RewardCalculator.Calculate([host, a], HostUserId);

        Assert.All(results, r => Assert.Equal(RewardStatus.NotAwarded, r.Status));
        Assert.Equal(RewardDenialReason.NotEnoughAccounts, For(results, a).DenialReason);
    }

    [Fact]
    public void Calculate_OneAccountManyGuests_NoOneRewarded()
    {
        var a = Account(500);

        var results = RewardCalculator.Calculate([a, Guest(900), Guest(800), Guest(700)], HostUserId);

        Assert.All(results, r => Assert.Equal(RewardStatus.NotAwarded, r.Status));
    }

    [Fact]
    public void Calculate_TwoDifferentAnsweredAccounts_BothRewarded()
    {
        var a = Account(800);
        var b = Account(600);

        var results = RewardCalculator.Calculate([a, b], HostUserId);

        Assert.All(results, r => Assert.Equal(RewardStatus.Awarded, r.Status));
        Assert.All(results, r => Assert.Null(r.DenialReason));
    }

    [Fact]
    public void Calculate_SameUserTwoPlayers_CountsAsOneAccount()
    {
        var userId = Guid.NewGuid();

        var results = RewardCalculator.Calculate([Account(800, userId: userId), Account(600, userId: userId)], HostUserId);

        Assert.All(results, r => Assert.Equal(RewardStatus.NotAwarded, r.Status));
    }

    // FR-005

    [Fact]
    public void Calculate_HostPlaysOwnRoom_HostNotRewardedButOthersAre()
    {
        var host = Account(900, userId: HostUserId);
        var a = Account(800);
        var b = Account(700);

        var results = RewardCalculator.Calculate([host, a, b], HostUserId);

        Assert.Equal(RewardStatus.NotAwarded, For(results, host).Status);
        Assert.Equal(RewardDenialReason.Host, For(results, host).DenialReason);
        Assert.Equal(0, For(results, host).Xp);
        Assert.Equal(RewardStatus.Awarded, For(results, a).Status);
        Assert.Equal(RewardStatus.Awarded, For(results, b).Status);
    }

    // FR-006

    [Fact]
    public void Calculate_GuestPlayer_NeverRewarded()
    {
        var guest = Guest(1000);

        var results = RewardCalculator.Calculate([guest, Account(800), Account(700)], HostUserId);

        var r = For(results, guest);
        Assert.Equal(RewardStatus.NotAwarded, r.Status);
        Assert.Equal(0, r.Xp);
        Assert.Equal(0, r.Coins);
        Assert.Equal(0, r.RankBonus);
    }

    // FR-007

    [Fact]
    public void CalculateXp_Score999_Returns99()
    {
        var a = Account(999);

        var results = RewardCalculator.Calculate([a, Account(100)], HostUserId);

        Assert.Equal(99, For(results, a).Xp);
    }

    [Fact]
    public void CalculateXp_ZeroScore_ReturnsZero()
    {
        var a = Account(0);

        var results = RewardCalculator.Calculate([a, Account(100)], HostUserId);

        Assert.Equal(0, For(results, a).Xp);
    }

    // FR-008

    [Fact]
    public void CalculateCoins_Score250Rank4_Returns2()
    {
        var last = Account(250);

        var results = RewardCalculator.Calculate([Account(900), Account(800), Account(700), last], HostUserId);

        var r = For(results, last);
        Assert.Equal(4, r.RewardRank);
        Assert.Equal(2, r.Coins);
        Assert.Equal(0, r.RankBonus);
    }

    [Fact]
    public void CalculateCoins_Rank1_Adds30()
    {
        var first = Account(1000);

        var results = RewardCalculator.Calculate([first, Account(500)], HostUserId);

        var r = For(results, first);
        Assert.Equal(10, r.Coins);
        Assert.Equal(30, r.RankBonus);
    }

    [Fact]
    public void CalculateCoins_Rank2_Adds20()
    {
        var second = Account(500);

        var results = RewardCalculator.Calculate([Account(1000), second], HostUserId);

        Assert.Equal(20, For(results, second).RankBonus);
    }

    [Fact]
    public void CalculateCoins_Rank3_Adds10()
    {
        var third = Account(300);

        var results = RewardCalculator.Calculate([Account(1000), Account(500), third], HostUserId);

        Assert.Equal(10, For(results, third).RankBonus);
    }

    [Fact]
    public void CalculateCoins_ZeroScoreRank1_NoRankBonus()
    {
        var a = Account(0);
        var b = Account(0);

        var results = RewardCalculator.Calculate([a, b], HostUserId);

        Assert.Equal(1, For(results, a).RewardRank);
        Assert.Equal(0, For(results, a).RankBonus);
        Assert.Equal(0, For(results, b).RankBonus);
    }

    // Hạng

    [Fact]
    public void Rank_TiedAtTop_BothRank1AndNextIsRank3()
    {
        var a = Account(900);
        var b = Account(900);
        var c = Account(500);

        var results = RewardCalculator.Calculate([a, b, c], HostUserId);

        Assert.Equal(1, For(results, a).RewardRank);
        Assert.Equal(1, For(results, b).RewardRank);
        Assert.Equal(3, For(results, c).RewardRank);
        Assert.Equal(30, For(results, a).RankBonus);
        Assert.Equal(30, For(results, b).RankBonus);
        Assert.Equal(10, For(results, c).RankBonus);
    }

    [Fact]
    public void Rank_GuestTakesFirst_AccountSecondGetsRank2Bonus()
    {
        var guest = Guest(900);
        var a = Account(800);
        var b = Account(700);

        var results = RewardCalculator.Calculate([guest, a, b], HostUserId);

        Assert.Equal(RewardStatus.NotAwarded, For(results, guest).Status);
        Assert.Equal(1, For(results, guest).RewardRank);
        Assert.Equal(2, For(results, a).RewardRank);
        Assert.Equal(20, For(results, a).RankBonus);
        Assert.Equal(10, For(results, b).RankBonus);
    }

    [Fact]
    public void Rank_HostCountsInRankingButNotRewarded()
    {
        var host = Account(900, userId: HostUserId);
        var a = Account(800);
        var b = Account(700);

        var results = RewardCalculator.Calculate([host, a, b], HostUserId);

        Assert.Equal(1, For(results, host).RewardRank);
        Assert.Equal(2, For(results, a).RewardRank);
        Assert.Equal(20, For(results, a).RankBonus);
    }

    // FR-013

    [Fact]
    public void Calculate_DenialReasonPrecedence_GuestThenHostThenNoAnswersThenNotEnoughAccounts()
    {
        var guest = Guest(0, answered: 0);          // khách: không có lý do thuộc enum (DTO tự gắn "guest")
        var hostNoAnswer = Account(0, answered: 0, userId: HostUserId); // Host thắng NoAnswers
        var noAnswer = Account(0, answered: 0);     // NoAnswers thắng NotEnoughAccounts
        var lonely = Account(500);                  // chỉ còn NotEnoughAccounts

        var results = RewardCalculator.Calculate([guest, hostNoAnswer, noAnswer, lonely], HostUserId);

        Assert.Null(For(results, guest).DenialReason);
        Assert.Equal(RewardDenialReason.Host, For(results, hostNoAnswer).DenialReason);
        Assert.Equal(RewardDenialReason.NoAnswers, For(results, noAnswer).DenialReason);
        Assert.Equal(RewardDenialReason.NotEnoughAccounts, For(results, lonely).DenialReason);
    }
}
