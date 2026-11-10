namespace QuizBattle.Application.Profile;

public sealed record ProfileDto(Guid UserId, string DisplayName, int Level, int TotalXp, int XpIntoLevel, int XpForNextLevel, int CoinBalance);
