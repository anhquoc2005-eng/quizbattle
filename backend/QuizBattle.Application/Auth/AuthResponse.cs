namespace QuizBattle.Application.Auth;

public sealed record AuthResponse(string Token, DateTimeOffset ExpiresAt, UserDto User);
