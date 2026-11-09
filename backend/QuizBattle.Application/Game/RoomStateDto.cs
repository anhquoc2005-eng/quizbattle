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
    bool? HasAnsweredCurrentQuestion,
    // Đồng hồ client có thể lệch vài giây - client tự tính offset = ServerTime - Date.now() lúc nhận, rồi
    // dùng offset đó để suy ra "server nghĩ bây giờ là mấy giờ" thay vì tin thẳng đồng hồ máy mình.
    DateTimeOffset ServerTime,
    // Có giá trị khi phòng Finished (đọc từ thưởng đã lưu, không tính lại); null ở trạng thái khác.
    IReadOnlyList<PlayerRewardDto>? Rewards);
