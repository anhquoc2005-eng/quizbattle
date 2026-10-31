using System.ComponentModel.DataAnnotations;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Application.Questions;

/// <summary>DTO dùng chung cho thêm câu thủ công và sửa câu - toàn bộ quy tắc nằm trong Validate().</summary>
public sealed class QuestionInput : IValidatableObject
{
    public required string Text { get; init; }

    public required string[] Options { get; init; }

    public int CorrectIndex { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!QuestionRules.IsValidText(Text))
        {
            yield return new ValidationResult(
                $"Text phải có 1-{QuestionRules.MaxTextLength} ký tự, không chỉ chứa khoảng trắng.", [nameof(Text)]);
        }

        if (Options is null || Options.Length != Question.OPTION_COUNT || Options.Any(string.IsNullOrWhiteSpace))
        {
            yield return new ValidationResult($"Phải có đúng {Question.OPTION_COUNT} lựa chọn, không để trống.", [nameof(Options)]);
        }
        else if (Options.Any(o => o.Trim().Length is < QuestionRules.MinOptionLength or > QuestionRules.MaxOptionLength))
        {
            yield return new ValidationResult($"Mỗi lựa chọn phải có {QuestionRules.MinOptionLength}-{QuestionRules.MaxOptionLength} ký tự.", [nameof(Options)]);
        }
        else if (QuestionRules.HasDuplicateOptions(Options))
        {
            yield return new ValidationResult("Các lựa chọn không được trùng nhau.", [nameof(Options)]);
        }

        if (!QuestionRules.IsValidCorrectIndex(CorrectIndex))
        {
            yield return new ValidationResult($"CorrectIndex phải trong khoảng 0-{Question.OPTION_COUNT - 1}.", [nameof(CorrectIndex)]);
        }
    }
}
