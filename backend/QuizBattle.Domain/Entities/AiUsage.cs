namespace QuizBattle.Domain.Entities;

/// <summary>Bộ đếm lượt generate AI theo (ngày giờ VN, phạm vi). Phạm vi: "global" hoặc "user:{guid}".</summary>
public sealed class AiUsage
{
    public DateOnly Day { get; set; }

    public string Scope { get; set; } = string.Empty;

    public int Count { get; set; }
}
