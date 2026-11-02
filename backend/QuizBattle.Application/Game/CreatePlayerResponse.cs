namespace QuizBattle.Application.Game;

public sealed record CreatePlayerResponse(Guid PlayerId, string SessionToken, string RoomCode);
