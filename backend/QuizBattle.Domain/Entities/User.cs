namespace QuizBattle.Domain.Entities;

public sealed class User
{
    public Guid Id { get; set; }

    // Luôn lưu dạng lowercase (normalize ở AuthService); DB có check constraint chặn trường hợp sót.
    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public required string DisplayName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Room> HostedRooms { get; set; } = [];
}
