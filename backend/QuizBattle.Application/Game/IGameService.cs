using QuizBattle.Application.Rooms;

namespace QuizBattle.Application.Game;

public interface IGameService
{
    // --- REST ---

    /// <summary>Draft -> Lobby. Cần ít nhất 1 câu Approved, không thì 409.</summary>
    Task<RoomResponse> OpenRoomAsync(Guid hostUserId, string code, CancellationToken cancellationToken);

    /// <summary>
    /// Người chơi vào phòng. userId null = khách: chỉ khi Lobby, chưa đủ 50 người, tên chưa trùng. Có userId: tài khoản đã có
    /// Player trong phòng thì dùng lại (xoay token, bỏ qua tên, cho phép Lobby và InProgress); chưa có thì như khách nhưng gắn UserId.
    /// </summary>
    Task<CreatePlayerResponse> CreatePlayerAsync(string code, Guid? userId, CreatePlayerRequest request, CancellationToken cancellationToken);

    // --- Hub ---

    Task<RoomStateDto> JoinAsHostAsync(Guid hostUserId, string code, CancellationToken cancellationToken);

    Task<(RoomStateDto State, PlayerJoinedDto Broadcast)> JoinAsPlayerAsync(string code, string sessionToken, CancellationToken cancellationToken);

    Task<QuestionStartedDto> StartGameAsync(Guid hostUserId, string code, CancellationToken cancellationToken);

    Task<(QuestionEndedDto Ended, LeaderboardUpdatedDto Leaderboard)> EndQuestionAsync(Guid hostUserId, string code, CancellationToken cancellationToken);

    Task<NextQuestionOutcome> NextQuestionAsync(Guid hostUserId, string code, CancellationToken cancellationToken);

    Task<(AnswerAckDto Ack, AnswerProgressDto Progress, Guid RoomId)> SubmitAnswerAsync(Guid playerId, Guid questionId, int selectedIndex, CancellationToken cancellationToken);

    // --- Thao tác hệ thống (GameTimerService, SubmitAnswer người cuối): không cần host, null = không đủ điều kiện ---

    Task<(QuestionEndedDto Ended, LeaderboardUpdatedDto Leaderboard)?> TryAutoEndQuestionAsync(Guid roomId, CancellationToken cancellationToken);

    Task<NextQuestionOutcome?> TryAutoAdvanceAsync(Guid roomId, CancellationToken cancellationToken);

    /// <summary>Các phòng InProgress có câu quá hạn hoặc kết quả đã hiện đủ lâu - một truy vấn cho mọi phòng.</summary>
    Task<IReadOnlyList<DueRoom>> FindDueRoomsAsync(CancellationToken cancellationToken);
}
