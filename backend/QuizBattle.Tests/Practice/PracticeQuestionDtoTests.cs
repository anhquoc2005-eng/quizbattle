using System.Text.Json;
using QuizBattle.Application.Practice;

namespace QuizBattle.Tests.Practice;

public sealed class PracticeQuestionDtoTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void PracticeQuestionDto_HasNoCorrectIndexProperty()
    {
        var names = typeof(PracticeQuestionDto).GetProperties().Select(p => p.Name).ToList();

        Assert.DoesNotContain(names, n => n.Contains("correct", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(["QuestionId", "Index", "Text", "Options"], names);
    }

    [Fact]
    public void PracticeSessionDto_Serialized_DoesNotContainCorrectIndexForCurrentQuestion()
    {
        var dto = new PracticeSessionDto(
            Guid.NewGuid(), "Chủ đề", 3, 0, false,
            new PracticeQuestionDto(Guid.NewGuid(), 0, "Câu 0", ["A", "B", "C", "D"]),
            LastResult: null, Summary: null);

        var json = JsonSerializer.Serialize(dto, Web);

        Assert.DoesNotContain("correct", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PracticeSessionDto_LastResult_IsTheOnlyPlaceCarryingCorrectIndex()
    {
        var current = new PracticeQuestionDto(Guid.NewGuid(), 1, "Câu 1", ["A", "B", "C", "D"]);
        var dto = new PracticeSessionDto(
            Guid.NewGuid(), "Chủ đề", 3, 1, false, current,
            new PracticeResultDto(Guid.NewGuid(), true, 2, 5, null), Summary: null);

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(dto, Web));

        Assert.False(doc.RootElement.GetProperty("current").TryGetProperty("correctIndex", out _));
        Assert.Equal(2, doc.RootElement.GetProperty("lastResult").GetProperty("correctIndex").GetInt32());
    }

    [Fact]
    public void PracticeShuffler_SameSeed_SamePermutationContainingEveryId()
    {
        var ids = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToList();

        var a = PracticeShuffler.Shuffle(ids, new Random(42));
        var b = PracticeShuffler.Shuffle(ids, new Random(42));

        Assert.Equal(a, b);
        Assert.Equal(ids.OrderBy(x => x), a.OrderBy(x => x));
    }
}
