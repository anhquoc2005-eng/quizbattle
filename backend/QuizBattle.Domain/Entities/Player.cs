namespace QuizBattle.Domain.Entities;

public sealed class Player
{
    public Guid Id { get; set; }

    public Guid RoomId { get; set; }

    public Room? Room { get; set; }

    // Tài khoản của người chơi; null nghĩa là khách (không nhận thưởng).
    public Guid? UserId { get; set; }

    public required string DisplayName { get; set; }

    public required string SessionTokenHash { get; set; }

    public int Score { get; set; }

    public DateTimeOffset JoinedAt { get; set; }

    public ICollection<Answer> Answers { get; set; } = [];
}
