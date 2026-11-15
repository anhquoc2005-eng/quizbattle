using QuizBattle.Application.Avatars;

namespace QuizBattle.Application.Profile;

/// <summary>Chỉ dữ liệu công khai: không email, không Id người dùng. Avatar luôn đủ ô (tài khoản chưa lưu nhận nhân vật mặc định).</summary>
public sealed record XpLeaderboardEntryDto(int Rank, string DisplayName, int Level, int TotalXp, AvatarDto Avatar);
