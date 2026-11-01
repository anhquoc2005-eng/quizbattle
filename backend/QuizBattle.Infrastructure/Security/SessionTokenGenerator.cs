using System.Security.Cryptography;
using QuizBattle.Application.Game;

namespace QuizBattle.Infrastructure.Security;

public sealed class SessionTokenGenerator : ISessionTokenGenerator
{
    private const int TokenLengthBytes = 32;

    public string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenLengthBytes);
        // base64url: thay +/ bằng -_ và bỏ padding '=' để dùng an toàn trong URL/JSON mà không cần encode thêm.
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
