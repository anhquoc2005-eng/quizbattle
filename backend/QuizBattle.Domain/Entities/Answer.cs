namespace QuizBattle.Domain.Entities;

public sealed class Answer
{
    public Guid Id { get; set; }

    public Guid PlayerId { get; set; }

    public Player? Player { get; set; }

    public Guid QuestionId { get; set; }

    public Question? Question { get; set; }

    public int SelectedIndex { get; set; }

    public DateTimeOffset AnsweredAt { get; set; }

    public bool IsCorrect { get; set; }

    public int PointsAwarded { get; set; }
}
