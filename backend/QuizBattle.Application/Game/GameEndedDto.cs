namespace QuizBattle.Application.Game;

public sealed record GameEndedDto(IReadOnlyList<LeaderboardEntryDto> FinalLeaderboard);
