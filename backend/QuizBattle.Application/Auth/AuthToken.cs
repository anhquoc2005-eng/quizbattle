namespace QuizBattle.Application.Auth;

public sealed record AuthToken(string Token, DateTimeOffset ExpiresAt);
