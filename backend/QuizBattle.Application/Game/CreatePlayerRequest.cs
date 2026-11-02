using System.ComponentModel.DataAnnotations;

namespace QuizBattle.Application.Game;

public sealed class CreatePlayerRequest : IValidatableObject
{
    public required string DisplayName { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var trimmed = DisplayName?.Trim() ?? string.Empty;
        if (trimmed.Length is < 1 or > 50)
        {
            yield return new ValidationResult("Tên hiển thị phải có 1-50 ký tự.", [nameof(DisplayName)]);
        }
    }
}
