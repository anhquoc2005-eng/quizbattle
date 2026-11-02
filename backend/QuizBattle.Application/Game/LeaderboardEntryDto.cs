namespace QuizBattle.Application.Game;

public sealed record LeaderboardEntryDto(Guid PlayerId, string DisplayName, int Score, int Rank);
