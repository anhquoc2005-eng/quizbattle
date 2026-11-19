namespace QuizBattle.Domain.AiQuota;

/// <summary>
/// "Ngày" của bộ đếm lượt AI tính theo giờ Việt Nam. Hàm thuần: thời gian truyền vào, không đọc đồng hồ máy.
/// Dùng độ lệch cố định +7h (Việt Nam không có giờ mùa hè) thay vì TimeZoneInfo, vì ID múi giờ khác nhau giữa
/// Windows và Linux và container có thể thiếu tzdata.
/// </summary>
public static class VietnamDay
{
    private static readonly TimeSpan Offset = TimeSpan.FromHours(7);

    /// <summary>Ngày (giờ VN) chứa thời điểm <paramref name="utcNow"/>; đầu vào mang offset nào cũng được.</summary>
    public static DateOnly DayOf(DateTimeOffset utcNow) => DateOnly.FromDateTime(utcNow.ToOffset(Offset).DateTime);

    /// <summary>00:00 giờ VN của ngày kế tiếp - thời điểm bộ đếm của <paramref name="day"/> được làm mới.</summary>
    public static DateTimeOffset NextResetAt(DateOnly day) =>
        new(day.AddDays(1).ToDateTime(TimeOnly.MinValue), Offset);
}
