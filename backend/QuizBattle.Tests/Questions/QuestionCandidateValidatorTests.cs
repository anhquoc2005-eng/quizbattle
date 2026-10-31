using QuizBattle.Application.Questions;

namespace QuizBattle.Tests.Questions;

public sealed class QuestionCandidateValidatorTests
{
    private static readonly string[] ValidOptions = ["A", "B", "C", "D"];

    [Fact]
    public void FilterValid_MissingOptions_DropsCandidate()
    {
        var candidates = new[] { new QuestionCandidate("Câu hỏi", ["A", "B", "C"], 0) };

        var result = QuestionCandidateValidator.FilterValid(candidates, []);

        Assert.Empty(result);
    }

    [Fact]
    public void FilterValid_DuplicateOptions_DropsCandidate()
    {
        var candidates = new[] { new QuestionCandidate("Câu hỏi", ["A", "A", "C", "D"], 0) };

        var result = QuestionCandidateValidator.FilterValid(candidates, []);

        Assert.Empty(result);
    }

    [Fact]
    public void FilterValid_CorrectIndexOutOfRange_DropsCandidate()
    {
        var candidates = new[] { new QuestionCandidate("Câu hỏi", ValidOptions, 4) };

        var result = QuestionCandidateValidator.FilterValid(candidates, []);

        Assert.Empty(result);
    }

    [Fact]
    public void FilterValid_DuplicateOfExistingQuestion_DropsCandidate()
    {
        var candidates = new[] { new QuestionCandidate("  câu hỏi cũ  ", ValidOptions, 0) };

        var result = QuestionCandidateValidator.FilterValid(candidates, ["Câu hỏi cũ"]);

        Assert.Empty(result);
    }

    [Fact]
    public void FilterValid_ValidCandidate_KeepsIt()
    {
        var candidates = new[] { new QuestionCandidate("Câu hỏi mới", ValidOptions, 1) };

        var result = QuestionCandidateValidator.FilterValid(candidates, ["Câu khác"]);

        Assert.Equal(candidates, result);
    }
}
