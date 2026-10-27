using QuizBattle.Domain.Enums;

namespace QuizBattle.Domain.Entities;

public sealed class Question
{
    public const int OPTION_COUNT = 4;

    public Guid Id { get; set; }

    public Guid RoomId { get; set; }

    public Room? Room { get; set; }

    public int OrderIndex { get; set; }

    public required string Text { get; set; }

    public required string[] Options { get; set; }

    public int CorrectIndex { get; set; }

    public QuestionReviewStatus ReviewStatus { get; set; } = QuestionReviewStatus.Pending;

    public DateTimeOffset? StartedAt { get; set; }

    public ICollection<Answer> Answers { get; set; } = [];
}
