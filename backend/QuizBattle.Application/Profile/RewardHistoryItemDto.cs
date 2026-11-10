namespace QuizBattle.Application.Profile;

public sealed record RewardHistoryItemDto(string RoomCode, string Topic, DateTimeOffset PlayedAt, int Score, int RewardRank, int Xp, int Coins, int RankBonus);
