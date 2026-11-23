namespace QuizBattle.Domain.Entities;

/// <summary>Bộ đếm XP ôn tập đã nhận của một người dùng trong một ngày giờ VN; luôn bằng SUM(PracticeAnswers.XpAwarded) theo (UserId, Day).</summary>
public sealed class PracticeXpDaily
{
    public Guid UserId { get; set; }

    public DateOnly Day { get; set; }

    public int Xp { get; set; }
}
