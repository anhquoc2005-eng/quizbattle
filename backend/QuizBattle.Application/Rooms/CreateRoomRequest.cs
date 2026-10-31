using System.ComponentModel.DataAnnotations;

namespace QuizBattle.Application.Rooms;

public sealed class CreateRoomRequest : IValidatableObject
{
    [Required, MaxLength(200)]
    public required string Topic { get; init; }

    [Range(5, 120)]
    public int? QuestionTimeLimitSeconds { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrEmpty(Topic) && Topic.Trim().Length == 0)
        {
            yield return new ValidationResult(
                "Topic không được chỉ chứa khoảng trắng.",
                [nameof(Topic)]);
        }
    }
}
