namespace QuizBattle.Domain;

public readonly record struct LevelProgress(int Level, int XpIntoLevel, int XpForNext);

/// <summary>Cấp độ suy ra từ tổng XP - hàm thuần. Lên từ cấp n sang n+1 cần 200 * n XP.</summary>
public static class LevelCalculator
{
    /// <summary>Tổng XP tối thiểu để đạt cấp n: 100 * n * (n - 1).</summary>
    public static int XpForLevel(int level) => 100 * level * (level - 1);

    public static int FromXp(int xp)
    {
        // Vòng lặp đơn giản và chính xác; cấp tối đa với int chỉ vài nghìn.
        var level = 1;
        // So sánh bằng long: XpForLevel(level + 1) có thể tràn int khi xp gần int.MaxValue.
        while (100L * (level + 1) * level <= xp)
        {
            level++;
        }

        return level;
    }

    public static LevelProgress Progress(int xp)
    {
        var level = FromXp(xp);
        return new LevelProgress(level, Math.Max(xp, 0) - XpForLevel(level), 200 * level);
    }
}
