using Microsoft.AspNetCore.Identity;
using QuizBattle.Application.Auth;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Infrastructure.Security;

/// <summary>Bọc PasswordHasher&lt;User&gt; chuẩn của ASP.NET Identity. Tham số user không được dùng bởi hasher mặc định.</summary>
public sealed class PasswordHasherAdapter : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(default!, password);

    public bool Verify(string hash, string password) =>
        _hasher.VerifyHashedPassword(default!, hash, password) != PasswordVerificationResult.Failed;
}
