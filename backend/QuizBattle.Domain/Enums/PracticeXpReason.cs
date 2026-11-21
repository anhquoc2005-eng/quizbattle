namespace QuizBattle.Domain.Enums;

/// <summary>
/// Vì sao một câu ôn tập không nhận đủ XP. "Nhận đủ" không có giá trị enum: lưu và trả là null.
/// </summary>
public enum PracticeXpReason
{
    Wrong,
    AlreadyToday,
    DailyCap,
    DailyCapPartial,
}

/// <summary>Chuỗi lưu trong DB và trả cho client (snake_case); CHECK CK_PracticeAnswers_XpReason dùng đúng các chuỗi này.</summary>
public static class PracticeXpReasonCodes
{
    public static string ToCode(this PracticeXpReason reason) => reason switch
    {
        PracticeXpReason.Wrong => "wrong",
        PracticeXpReason.AlreadyToday => "already_today",
        PracticeXpReason.DailyCap => "daily_cap",
        PracticeXpReason.DailyCapPartial => "daily_cap_partial",
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null),
    };

    public static PracticeXpReason FromCode(string code) => code switch
    {
        "wrong" => PracticeXpReason.Wrong,
        "already_today" => PracticeXpReason.AlreadyToday,
        "daily_cap" => PracticeXpReason.DailyCap,
        "daily_cap_partial" => PracticeXpReason.DailyCapPartial,
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Mã lý do XP ôn tập không hợp lệ."),
    };
}
