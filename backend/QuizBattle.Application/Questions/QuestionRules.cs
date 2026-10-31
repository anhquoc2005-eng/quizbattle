using QuizBattle.Domain.Entities;

namespace QuizBattle.Application.Questions;

/// <summary>Quy tắc validate câu hỏi dùng chung cho nhập tay (QuestionInput) và lọc kết quả AI (QuestionCandidateValidator).</summary>
public static class QuestionRules
{
    public const int MaxTextLength = 500;
    public const int MinOptionLength = 1;
    public const int MaxOptionLength = 200;

    public static bool IsValidText(string? text) =>
        !string.IsNullOrWhiteSpace(text) && text.Trim().Length <= MaxTextLength;

    public static bool IsValidOptions(string[]? options) =>
        options is not null
        && options.Length == Question.OPTION_COUNT
        && options.All(o => !string.IsNullOrWhiteSpace(o) && o.Trim().Length is >= MinOptionLength and <= MaxOptionLength)
        && !HasDuplicateOptions(options);

    public static bool HasDuplicateOptions(string[] options) =>
        options.Select(o => o.Trim().ToUpperInvariant()).Distinct().Count() != options.Length;

    public static bool IsValidCorrectIndex(int correctIndex) =>
        correctIndex >= 0 && correctIndex < Question.OPTION_COUNT;
}
