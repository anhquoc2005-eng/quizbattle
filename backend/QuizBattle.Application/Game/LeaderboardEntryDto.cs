using QuizBattle.Application.Avatars;

namespace QuizBattle.Application.Game;

public sealed record LeaderboardEntryDto(Guid PlayerId, string DisplayName, int Score, int Rank, int? Level, AvatarDto Avatar);
