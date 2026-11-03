using QuizBattle.Domain;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Tests.Game;

public sealed class GameTimelineTests
{
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ShouldEndQuestion_PastDeadline_ReturnsTrue() =>
        Assert.True(GameTimeline.ShouldEndQuestion(Start, null, 20, answeredCount: 0, totalPlayers: 3, Start.AddSeconds(20)));

    [Fact]
    public void ShouldEndQuestion_BeforeDeadlineAllAnswered_ReturnsTrue() =>
        Assert.True(GameTimeline.ShouldEndQuestion(Start, null, 20, answeredCount: 3, totalPlayers: 3, Start.AddSeconds(5)));

    [Fact]
    public void ShouldEndQuestion_BeforeDeadlineNotAllAnswered_ReturnsFalse() =>
        Assert.False(GameTimeline.ShouldEndQuestion(Start, null, 20, answeredCount: 2, totalPlayers: 3, Start.AddSeconds(5)));

    [Fact]
    public void ShouldEndQuestion_NoPlayersBeforeDeadline_ReturnsFalse() =>
        Assert.False(GameTimeline.ShouldEndQuestion(Start, null, 20, answeredCount: 0, totalPlayers: 0, Start.AddSeconds(5)));

    [Fact]
    public void ShouldEndQuestion_AlreadyEnded_ReturnsFalse() =>
        Assert.False(GameTimeline.ShouldEndQuestion(Start, Start.AddSeconds(3), 20, answeredCount: 3, totalPlayers: 3, Start.AddSeconds(30)));

    [Fact]
    public void ShouldAdvance_EndedFiveSecondsAgo_ReturnsFalse() =>
        Assert.False(GameTimeline.ShouldAdvance(Start, Start.AddSeconds(Room.RESULT_DISPLAY_SECONDS - 1)));

    [Fact]
    public void ShouldAdvance_EndedSixSecondsAgo_ReturnsTrue() =>
        Assert.True(GameTimeline.ShouldAdvance(Start, Start.AddSeconds(Room.RESULT_DISPLAY_SECONDS)));

    [Fact]
    public void ShouldAdvance_QuestionNotEnded_ReturnsFalse() =>
        Assert.False(GameTimeline.ShouldAdvance(null, Start.AddMinutes(5)));
}
