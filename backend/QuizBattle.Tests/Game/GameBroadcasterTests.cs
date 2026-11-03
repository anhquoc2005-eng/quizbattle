using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using QuizBattle.Api.Hubs;
using QuizBattle.Application.Game;

namespace QuizBattle.Tests.Game;

public sealed class GameBroadcasterTests
{
    [Fact]
    public async Task TryEndIfAllAnswered_ServiceThrows_DoesNotPropagate()
    {
        // Hub gọi hàm này sau khi đã có ack; lỗi ở đây mà thoát ra ngoài sẽ biến câu trả lời đã lưu thành lỗi cho người chơi.
        var services = new ServiceCollection().AddSingleton<IGameService, ThrowingGameService>().BuildServiceProvider();
        var broadcaster = new GameBroadcaster(new UnusedHubContext(), services.GetRequiredService<IServiceScopeFactory>(), NullLogger<GameBroadcaster>.Instance);

        await broadcaster.TryEndIfAllAnsweredAsync(Guid.NewGuid(), "ABCDEF");
    }

    private sealed class UnusedHubContext : IHubContext<GameHub>
    {
        public IHubClients Clients => throw new NotSupportedException();

        public IGroupManager Groups => throw new NotSupportedException();
    }

    private sealed class ThrowingGameService : IGameService
    {
        public Task<(QuestionEndedDto Ended, LeaderboardUpdatedDto Leaderboard)?> TryAutoEndQuestionAsync(Guid roomId, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("DB chập chờn");

        public Task<QuizBattle.Application.Rooms.RoomResponse> OpenRoomAsync(Guid hostUserId, string code, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<CreatePlayerResponse> CreatePlayerAsync(string code, CreatePlayerRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<RoomStateDto> JoinAsHostAsync(Guid hostUserId, string code, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<(RoomStateDto State, PlayerJoinedDto Broadcast)> JoinAsPlayerAsync(string code, string sessionToken, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<QuestionStartedDto> StartGameAsync(Guid hostUserId, string code, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<(QuestionEndedDto Ended, LeaderboardUpdatedDto Leaderboard)> EndQuestionAsync(Guid hostUserId, string code, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<NextQuestionOutcome> NextQuestionAsync(Guid hostUserId, string code, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<(AnswerAckDto Ack, AnswerProgressDto Progress, Guid RoomId)> SubmitAnswerAsync(Guid playerId, Guid questionId, int selectedIndex, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<NextQuestionOutcome?> TryAutoAdvanceAsync(Guid roomId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<DueRoom>> FindDueRoomsAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
