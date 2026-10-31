using QuizBattle.Application.Questions;

namespace QuizBattle.Tests.Questions;

public sealed class OptionShufflerTests
{
    [Fact]
    public void Shuffle_KeepsCorrectAnswerText()
    {
        var options = new[] { "Đúng", "Sai A", "Sai B", "Sai C" };

        var (shuffled, correctIndex) = OptionShuffler.Shuffle(options, correctIndex: 0);

        Assert.Equal("Đúng", shuffled[correctIndex]);
        Assert.Equal(options.OrderBy(o => o), shuffled.OrderBy(o => o));
    }
}
