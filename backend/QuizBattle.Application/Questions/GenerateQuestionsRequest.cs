using System.ComponentModel.DataAnnotations;

namespace QuizBattle.Application.Questions;

public sealed class GenerateQuestionsRequest
{
    [Range(5, 20)]
    public int Count { get; init; }
}
