using QuizBattle.Domain;

namespace QuizBattle.Tests.Game;

public sealed class ScoreCalculatorTests
{
    private static readonly DateTimeOffset StartedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Calculate_AnsweredImmediately_ReturnsAlmostThousand()
    {
        var score = ScoreCalculator.Calculate(isCorrect: true, StartedAt, StartedAt, timeLimitSeconds: 20);

        Assert.Equal(1000, score);
    }

    [Fact]
    public void Calculate_AnsweredAtDeadline_ReturnsFiveHundred()
    {
        var score = ScoreCalculator.Calculate(isCorrect: true, StartedAt, StartedAt.AddSeconds(20), timeLimitSeconds: 20);

        Assert.Equal(500, score);
    }

    [Fact]
    public void Calculate_Incorrect_ReturnsZero()
    {
        var score = ScoreCalculator.Calculate(isCorrect: false, StartedAt, StartedAt.AddSeconds(2), timeLimitSeconds: 20);

        Assert.Equal(0, score);
    }

    [Fact]
    public void Calculate_AfterDeadline_ReturnsZero()
    {
        var score = ScoreCalculator.Calculate(isCorrect: true, StartedAt, StartedAt.AddSeconds(21), timeLimitSeconds: 20);

        Assert.Equal(0, score);
    }

    [Fact]
    public void Calculate_AnsweredBeforeStarted_TreatedAsImmediate()
    {
        var score = ScoreCalculator.Calculate(isCorrect: true, StartedAt, StartedAt.AddSeconds(-5), timeLimitSeconds: 20);

        Assert.Equal(1000, score);
    }

    [Fact]
    public void Calculate_AnsweredHalfway_ReturnsSevenHundredFifty()
    {
        var score = ScoreCalculator.Calculate(isCorrect: true, StartedAt, StartedAt.AddSeconds(10), timeLimitSeconds: 20);

        Assert.Equal(750, score);
    }
}
