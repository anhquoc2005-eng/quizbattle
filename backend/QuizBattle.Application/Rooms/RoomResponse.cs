namespace QuizBattle.Application.Rooms;

public sealed record RoomResponse(
    Guid Id,
    string Code,
    string Topic,
    string Status,
    int QuestionTimeLimitSeconds,
    DateTimeOffset CreatedAt,
    int QuestionCount,
    int PlayerCount);
