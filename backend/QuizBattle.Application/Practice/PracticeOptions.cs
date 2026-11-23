namespace QuizBattle.Application.Practice;

/// <summary>XP ôn tập: Practice:XpPerCorrect (mặc định 5) và Practice:DailyXpCap (mặc định 100, tính theo ngày giờ VN).</summary>
public sealed record PracticeOptions(int XpPerCorrect, int DailyXpCap)
{
    public const int DefaultXpPerCorrect = 5;
    public const int DefaultDailyXpCap = 100;

    /// <summary>
    /// Không đặt thì dùng mặc định; đã đặt thì phải là số nguyên &gt;= 0 (0 hợp lệ: tắt thưởng XP ôn tập). Sai thì ném lỗi
    /// nêu tên biến môi trường. Gọi lúc đăng ký DI nên backend không khởi động được khi cấu hình sai, ở mọi môi trường
    /// (cùng cách <c>AiQuotaOptions</c>); không đưa vào ProductionConfigValidator vì có mặc định.
    /// </summary>
    public static PracticeOptions Parse(string? xpPerCorrect, string? dailyXpCap) => new(
        ParseNonNegative(xpPerCorrect, "Practice__XpPerCorrect", DefaultXpPerCorrect),
        ParseNonNegative(dailyXpCap, "Practice__DailyXpCap", DefaultDailyXpCap));

    private static int ParseNonNegative(string? raw, string envName, int fallback)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        if (!int.TryParse(raw, out var value) || value < 0)
        {
            throw new InvalidOperationException($"{envName} phải là số nguyên không âm (đang là '{raw}').");
        }

        return value;
    }
}
