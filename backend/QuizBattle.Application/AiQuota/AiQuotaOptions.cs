namespace QuizBattle.Application.AiQuota;

/// <summary>Trần lượt generate AI mỗi ngày: Ai:DailyLimitPerUser (mặc định 20) và Ai:DailyLimitGlobal (mặc định 300).</summary>
public sealed record AiQuotaOptions(int DailyLimitPerUser, int DailyLimitGlobal)
{
    public const int DefaultDailyLimitPerUser = 20;
    public const int DefaultDailyLimitGlobal = 300;

    /// <summary>
    /// Không đặt thì dùng mặc định; đã đặt thì phải là số nguyên &gt; 0 (trần 0 sẽ lách được câu giữ chỗ: lần INSERT đầu
    /// trong ngày luôn tạo Count = 1). Sai thì ném lỗi nêu tên biến môi trường để thấy ngay lúc khởi động.
    /// </summary>
    public static AiQuotaOptions Parse(string? perUser, string? global) => new(
        ParseLimit(perUser, "Ai__DailyLimitPerUser", DefaultDailyLimitPerUser),
        ParseLimit(global, "Ai__DailyLimitGlobal", DefaultDailyLimitGlobal));

    private static int ParseLimit(string? raw, string envName, int fallback)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        if (!int.TryParse(raw, out var value) || value <= 0)
        {
            throw new InvalidOperationException($"{envName} phải là số nguyên lớn hơn 0 (đang là '{raw}').");
        }

        return value;
    }
}
