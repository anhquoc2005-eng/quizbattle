using QuizBattle.Application.Rooms;

namespace QuizBattle.Application.Game;

public interface IGameService
{
    // --- REST ---

    /// <summary>Draft -> Lobby. Cần ít nhất 1 câu Approved, không thì 409.</summary>
    Task<RoomResponse> OpenRoomAsync(Guid hostUserId, string code, CancellationToken cancellationToken);

    /// <summary>Người chơi vào phòng (ẩn danh) - chỉ khi Lobby, chưa đủ 50 người, tên chưa trùng trong phòng.</summary>
    Task<CreatePlayerResponse> CreatePlayerAsync(string code, CreatePlayerRequest request, CancellationToken cancellationToken);

    // --- Hub ---

    Task<RoomStateDto> JoinAsHostAsync(Guid hostUserId, string code, CancellationToken cancellationToken);

    Task<(RoomStateDto State, PlayerJoinedDto Broadcast)> JoinAsPlayerAsync(string code, string sessionToken, CancellationToken cancellationToken);

    Task<QuestionStartedDto> StartGameAsync(Guid hostUserId, string code, CancellationToken cancellationToken);

    Task<(QuestionEndedDto Ended, LeaderboardUpdatedDto Leaderboard)> EndQuestionAsync(Guid hostUserId, string code, CancellationToken cancellationToken);

    Task<NextQuestionOutcome> NextQuestionAsync(Guid hostUserId, string code, CancellationToken cancellationToken);

    Task<(AnswerAckDto Ack, AnswerProgressDto Progress)> SubmitAnswerAsync(Guid playerId, Guid questionId, int selectedIndex, CancellationToken cancellationToken);
}
