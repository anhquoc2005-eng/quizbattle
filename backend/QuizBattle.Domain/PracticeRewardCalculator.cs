using QuizBattle.Domain.Enums;

namespace QuizBattle.Domain;

/// <summary>Luật thưởng XP của chế độ ôn tập - hàm thuần, không phụ thuộc DB hay đồng hồ.</summary>
public static class PracticeRewardCalculator
{
    /// <summary>
    /// XP mong muốn cho một câu, TRƯỚC khi áp trần ngày. Reason null nghĩa là nhận đủ <paramref name="xpPerCorrect"/>
    /// (kể cả khi xpPerCorrect = 0).
    /// </summary>
    public static (int DesiredXp, PracticeXpReason? Reason) Calculate(bool isCorrect, bool alreadyRewardedTodayForQuestion, int xpPerCorrect)
    {
        if (!isCorrect)
        {
            return (0, PracticeXpReason.Wrong);
        }

        return alreadyRewardedTodayForQuestion ? (0, PracticeXpReason.AlreadyToday) : (xpPerCorrect, null);
    }

    /// <summary>
    /// Áp trần ngày: cộng phần còn lại khi quota nhỏ hơn XP của câu (LEAST). CapReason null nghĩa là không bị cắt
    /// (hoặc desired = 0), người gọi giữ lý do của <see cref="Calculate"/>; khác null thì thay lý do bằng nó.
    /// </summary>
    public static (int Granted, PracticeXpReason? CapReason) ApplyCap(int desiredXp, int usedToday, int dailyCap)
    {
        if (desiredXp <= 0)
        {
            return (0, null);
        }

        var granted = Math.Min(desiredXp, Math.Max(dailyCap - usedToday, 0));
        if (granted == desiredXp)
        {
            return (granted, null);
        }

        return (granted, granted > 0 ? PracticeXpReason.DailyCapPartial : PracticeXpReason.DailyCap);
    }
}
