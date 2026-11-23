using QuizBattle.Domain;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Tests.Domain;

public class PracticeRewardCalculatorTests
{
    [Fact]
    public void Calculate_WrongAnswer_ReturnsZeroWithWrong()
    {
        var (xp, reason) = PracticeRewardCalculator.Calculate(isCorrect: false, alreadyRewardedTodayForQuestion: false, xpPerCorrect: 5);

        Assert.Equal(0, xp);
        Assert.Equal(PracticeXpReason.Wrong, reason);
    }

    [Fact]
    public void Calculate_CorrectAlreadyRewardedToday_ReturnsZeroWithAlreadyToday()
    {
        var (xp, reason) = PracticeRewardCalculator.Calculate(true, true, 5);

        Assert.Equal(0, xp);
        Assert.Equal(PracticeXpReason.AlreadyToday, reason);
    }

    [Fact]
    public void Calculate_WrongAndAlreadyRewarded_WrongWins()
    {
        Assert.Equal(PracticeXpReason.Wrong, PracticeRewardCalculator.Calculate(false, true, 5).Reason);
    }

    [Fact]
    public void Calculate_Correct_ReturnsXpPerCorrectWithNullReason()
    {
        var (xp, reason) = PracticeRewardCalculator.Calculate(true, false, 5);

        Assert.Equal(5, xp);
        Assert.Null(reason);
    }

    [Fact]
    public void Calculate_XpPerCorrectZero_ReturnsZeroWithNullReason()
    {
        var (xp, reason) = PracticeRewardCalculator.Calculate(true, false, 0);

        Assert.Equal(0, xp);
        Assert.Null(reason);
    }

    [Fact]
    public void ApplyCap_EnoughRoom_GrantsAllWithNoCapReason()
    {
        var (granted, capReason) = PracticeRewardCalculator.ApplyCap(5, usedToday: 95, dailyCap: 100);

        Assert.Equal(5, granted);
        Assert.Null(capReason);
    }

    [Fact]
    public void ApplyCap_RemainingLessThanDesired_GrantsRemainderWithPartial()
    {
        // Cấu hình 7 XP/câu, trần 10: câu thứ hai chỉ còn 3.
        var (granted, capReason) = PracticeRewardCalculator.ApplyCap(7, usedToday: 7, dailyCap: 10);

        Assert.Equal(3, granted);
        Assert.Equal(PracticeXpReason.DailyCapPartial, capReason);
    }

    [Fact]
    public void ApplyCap_CapReached_ReturnsZeroWithDailyCap()
    {
        var (granted, capReason) = PracticeRewardCalculator.ApplyCap(7, usedToday: 10, dailyCap: 10);

        Assert.Equal(0, granted);
        Assert.Equal(PracticeXpReason.DailyCap, capReason);
    }

    [Fact]
    public void ApplyCap_CapZero_ReturnsZeroWithDailyCap()
    {
        var (granted, capReason) = PracticeRewardCalculator.ApplyCap(5, usedToday: 0, dailyCap: 0);

        Assert.Equal(0, granted);
        Assert.Equal(PracticeXpReason.DailyCap, capReason);
    }

    [Fact]
    public void ApplyCap_UsedAboveCap_ReturnsZeroNotNegative()
    {
        // Hạ trần giữa ngày: đã dùng 100, trần mới 50.
        var (granted, capReason) = PracticeRewardCalculator.ApplyCap(5, usedToday: 100, dailyCap: 50);

        Assert.Equal(0, granted);
        Assert.Equal(PracticeXpReason.DailyCap, capReason);
    }

    [Fact]
    public void ApplyCap_DesiredZero_ReturnsZeroWithNoCapReason()
    {
        var (granted, capReason) = PracticeRewardCalculator.ApplyCap(0, usedToday: 100, dailyCap: 100);

        Assert.Equal(0, granted);
        Assert.Null(capReason);
    }

    [Theory]
    [InlineData(PracticeXpReason.Wrong, "wrong")]
    [InlineData(PracticeXpReason.AlreadyToday, "already_today")]
    [InlineData(PracticeXpReason.DailyCap, "daily_cap")]
    [InlineData(PracticeXpReason.DailyCapPartial, "daily_cap_partial")]
    public void ReasonCodes_RoundTrip(PracticeXpReason reason, string code)
    {
        Assert.Equal(code, reason.ToCode());
        Assert.Equal(reason, PracticeXpReasonCodes.FromCode(code));
    }
}
