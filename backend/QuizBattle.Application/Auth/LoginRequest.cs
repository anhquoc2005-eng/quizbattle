using System.ComponentModel.DataAnnotations;

namespace QuizBattle.Application.Auth;

public sealed class LoginRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public required string Email { get; init; }

    // Giới hạn độ dài để tránh gửi mật khẩu cực dài làm PasswordHasher tốn CPU bất thường (DoS).
    [Required, MaxLength(128)]
    public required string Password { get; init; }
}
