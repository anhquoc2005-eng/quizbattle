using QuizBattle.Domain.Enums;

namespace QuizBattle.Domain.Entities;

/// <summary>
/// Kết quả chấm một câu ôn tập (bất biến sau khi tạo) và cũng là dòng ghi nhận XP ôn tập:
/// <c>Users.TotalXp = SUM(MatchRewards.Xp) + SUM(PracticeAnswers.XpAwarded)</c>. Chỉ cascade theo User; SessionId/QuestionId
/// nullable (SET NULL) để xóa phòng không làm mất XP đã cộng.
/// </summary>
public sealed class PracticeAnswer
{
    public Guid Id { get; set; }

    public Guid? SessionId { get; set; }

    public Guid UserId { get; set; }

    public Guid? QuestionId { get; set; }

    public int SelectedIndex { get; set; }

    public bool IsCorrect { get; set; }

    public int XpAwarded { get; set; }

    /// <summary>Null nghĩa là nhận đủ XpPerCorrect (chỉ hợp lệ với câu đúng).</summary>
    public PracticeXpReason? XpReason { get; set; }

    /// <summary>Ngày giờ VN của thời điểm nộp.</summary>
    public DateOnly Day { get; set; }

    public DateTimeOffset AnsweredAt { get; set; }
}
