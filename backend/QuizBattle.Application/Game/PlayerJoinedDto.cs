namespace QuizBattle.Application.Game;

public sealed record PlayerJoinedDto(Guid PlayerId, string DisplayName);
