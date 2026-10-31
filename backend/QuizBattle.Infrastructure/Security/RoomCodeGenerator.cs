using System.Security.Cryptography;
using QuizBattle.Application.Rooms;

namespace QuizBattle.Infrastructure.Security;

public sealed class RoomCodeGenerator : IRoomCodeGenerator
{
    // Bỏ 0/O và 1/I/L - dễ đọc nhầm khi người chơi gõ tay trên điện thoại.
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 6;

    public string Generate()
    {
        Span<char> code = stackalloc char[CodeLength];
        for (var i = 0; i < CodeLength; i++)
        {
            // RandomNumberGenerator.GetInt32 tự tránh lệch phân phối (modulo bias), khác lấy byte % length.
            code[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(code);
    }
}
