namespace QuizBattle.Domain;

/// <summary>Tính điểm cho 1 câu trả lời - hàm thuần, không phụ thuộc DB/đồng hồ hệ thống.</summary>
public static class ScoreCalculator
{
    private const int BaseScore = 500;
    private const int BonusScore = 500;

    public static int Calculate(bool isCorrect, DateTimeOffset startedAt, DateTimeOffset answeredAt, int timeLimitSeconds)
    {
        if (!isCorrect)
        {
            return 0;
        }

        var elapsedSeconds = (answeredAt - startedAt).TotalSeconds;
        if (elapsedSeconds > timeLimitSeconds)
        {
            return 0; // quá hạn
        }

        // answeredAt trước startedAt (lệch giờ): coi như trả lời ngay, không cho vượt quá tổng thời gian.
        elapsedSeconds = Math.Clamp(elapsedSeconds, 0, timeLimitSeconds);
        var remainingSeconds = timeLimitSeconds - elapsedSeconds;

        return BaseScore + (int)Math.Round(BonusScore * remainingSeconds / timeLimitSeconds);
    }
}
