using System.Security.Cryptography;
using System.Text;

namespace QuizBattle.Application.Game;

/// <summary>Băm sessionToken bằng SHA-256 - hàm thuần. DB chỉ lưu kết quả này, không bao giờ lưu token gốc.</summary>
public static class SessionTokenHasher
{
    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
