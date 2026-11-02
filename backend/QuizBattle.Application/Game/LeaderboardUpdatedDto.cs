namespace QuizBattle.Application.Game;

public sealed record LeaderboardUpdatedDto(IReadOnlyList<LeaderboardEntryDto> Leaderboard);
