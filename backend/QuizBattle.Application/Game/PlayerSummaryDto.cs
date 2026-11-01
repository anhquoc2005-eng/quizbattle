namespace QuizBattle.Application.Game;

public sealed record PlayerSummaryDto(Guid PlayerId, string DisplayName, int Score);
