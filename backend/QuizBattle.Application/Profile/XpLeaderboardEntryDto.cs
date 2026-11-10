namespace QuizBattle.Application.Profile;

/// <summary>Chỉ dữ liệu công khai: không email, không Id người dùng.</summary>
public sealed record XpLeaderboardEntryDto(int Rank, string DisplayName, int Level, int TotalXp);
