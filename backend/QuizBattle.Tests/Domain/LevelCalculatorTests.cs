using QuizBattle.Domain;

namespace QuizBattle.Tests.Domain;

public sealed class LevelCalculatorTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(199, 1)]
    [InlineData(200, 2)]
    [InlineData(599, 2)]
    [InlineData(600, 3)]
    public void LevelFromXp_Boundaries_ReturnsExpectedLevel(int xp, int expectedLevel)
    {
        Assert.Equal(expectedLevel, LevelCalculator.FromXp(xp));
    }

    [Fact]
    public void LevelFromXp_0_Returns1() => Assert.Equal(1, LevelCalculator.FromXp(0));

    [Fact]
    public void LevelFromXp_NegativeXp_Returns1() => Assert.Equal(1, LevelCalculator.FromXp(-5));

    [Fact]
    public void XpForLevel_RoundTripsWithLevelFromXp()
    {
        for (var level = 1; level <= 200; level++)
        {
            var threshold = LevelCalculator.XpForLevel(level);

            Assert.Equal(level, LevelCalculator.FromXp(threshold));
            if (threshold > 0)
            {
                Assert.Equal(level - 1, LevelCalculator.FromXp(threshold - 1));
            }
        }
    }

    [Fact]
    public void XpForLevel_FollowsSpecThresholds()
    {
        Assert.Equal(0, LevelCalculator.XpForLevel(1));
        Assert.Equal(200, LevelCalculator.XpForLevel(2));
        Assert.Equal(600, LevelCalculator.XpForLevel(3));
        Assert.Equal(1200, LevelCalculator.XpForLevel(4));
    }

    [Fact]
    public void Progress_ReturnsXpIntoLevelAndXpForNext()
    {
        var progress = LevelCalculator.Progress(650); // cấp 3 bắt đầu ở 600, cần 600 XP để lên cấp 4

        Assert.Equal(3, progress.Level);
        Assert.Equal(50, progress.XpIntoLevel);
        Assert.Equal(600, progress.XpForNext);
    }

    [Fact]
    public void Progress_ZeroXp_StartsAtLevel1()
    {
        var progress = LevelCalculator.Progress(0);

        Assert.Equal(1, progress.Level);
        Assert.Equal(0, progress.XpIntoLevel);
        Assert.Equal(200, progress.XpForNext);
    }
}
