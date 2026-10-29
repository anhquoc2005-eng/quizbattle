using System.Text;
using Microsoft.Extensions.Configuration;

namespace QuizBattle.Infrastructure.Security;

public sealed record JwtOptions(string Key, string Issuer, string Audience)
{
    private const int MinKeyLengthBytes = 32;

    /// <summary>Đọc và validate cấu hình Jwt từ config. Ném lỗi rõ ngay lúc khởi động nếu thiếu/sai.</summary>
    public static JwtOptions Load(IConfiguration configuration)
    {
        var key = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < MinKeyLengthBytes)
        {
            throw new InvalidOperationException(
                $"Jwt:Key thiếu hoặc quá ngắn (cần >= {MinKeyLengthBytes} byte). Xem hướng dẫn trong README.");
        }

        var issuer = configuration["Jwt:Issuer"];
        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException("Thiếu Jwt:Issuer trong cấu hình.");
        }

        var audience = configuration["Jwt:Audience"];
        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException("Thiếu Jwt:Audience trong cấu hình.");
        }

        return new JwtOptions(key, issuer, audience);
    }
}
