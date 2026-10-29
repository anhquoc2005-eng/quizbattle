using System.ComponentModel.DataAnnotations;

namespace QuizBattle.Application.Auth;

public sealed class LoginRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public required string Email { get; init; }

    [Required]
    public required string Password { get; init; }
}
