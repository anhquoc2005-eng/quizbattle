using QuizBattle.Application.Avatars;

namespace QuizBattle.Application.Game;

public sealed record PlayerJoinedDto(Guid PlayerId, string DisplayName, int? Level, AvatarDto Avatar);
