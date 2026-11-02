namespace QuizBattle.Application.Game;

/// <summary>Gửi riêng cho người vừa JoinRoom (lần đầu hoặc vào lại sau khi rớt mạng) - toàn bộ trạng thái hiện tại.</summary>
public sealed record RoomStateDto(
    string Code,
    string Status,
    int QuestionTimeLimitSeconds,
    IReadOnlyList<PlayerSummaryDto> Players,
    CurrentQuestionStateDto? CurrentQuestion,
    IReadOnlyList<LeaderboardEntryDto> Leaderboard,
    bool IsHost,
    Guid? PlayerId,
    bool? HasAnsweredCurrentQuestion);
