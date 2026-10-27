using QuizBattle.Domain.Enums;

namespace QuizBattle.Domain.Entities;

public sealed class Room
{
    public const int MAX_PLAYERS = 50;

    public Guid Id { get; set; }

    public required string Code { get; set; }

    public Guid HostUserId { get; set; }

    public User? HostUser { get; set; }

    public required string Topic { get; set; }

    public RoomStatus Status { get; set; } = RoomStatus.Draft;

    public int QuestionTimeLimitSeconds { get; set; } = 20;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? EndedAt { get; set; }

    public ICollection<Question> Questions { get; set; } = [];

    public ICollection<Player> Players { get; set; } = [];
}
