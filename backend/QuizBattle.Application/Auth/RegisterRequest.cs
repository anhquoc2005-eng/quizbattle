using System.ComponentModel.DataAnnotations;

namespace QuizBattle.Application.Auth;

public sealed class RegisterRequest : IValidatableObject
{
    [Required, EmailAddress, MaxLength(256)]
    public required string Email { get; init; }

    [Required, MinLength(8), MaxLength(128)]
    public required string Password { get; init; }

    [Required, MinLength(1), MaxLength(50)]
    public required string DisplayName { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrEmpty(DisplayName) && DisplayName.Trim().Length == 0)
        {
            yield return new ValidationResult(
                "DisplayName không được chỉ chứa khoảng trắng.",
                [nameof(DisplayName)]);
        }
    }
}
