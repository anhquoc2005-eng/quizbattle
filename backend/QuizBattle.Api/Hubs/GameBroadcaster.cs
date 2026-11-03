using Microsoft.AspNetCore.SignalR;
using QuizBattle.Application.Game;

namespace QuizBattle.Api.Hubs;

/// <summary>
/// Nơi duy nhất gửi sự kiện trận ra SignalR group - GameHub và GameTimerService cùng dùng để không lặp code
/// (đặc biệt cặp QuestionEnded + LeaderboardUpdated phải luôn đi chung, đúng thứ tự). Singleton: chỉ giữ
/// IHubContext (singleton) và IServiceScopeFactory, không giữ DbContext.
/// </summary>
public sealed class GameBroadcaster(IHubContext<GameHub> hub, IServiceScopeFactory scopeFactory, ILogger<GameBroadcaster> logger)
{
    public async Task QuestionEndedAsync(string code, QuestionEndedDto ended, LeaderboardUpdatedDto leaderboard)
    {
        await hub.Clients.Group(code).SendAsync(GameHubEvents.QUESTION_ENDED, ended);
        await hub.Clients.Group(code).SendAsync(GameHubEvents.LEADERBOARD_UPDATED, leaderboard);
    }

    public Task QuestionStartedAsync(string code, QuestionStartedDto started) =>
        hub.Clients.Group(code).SendAsync(GameHubEvents.QUESTION_STARTED, started);

    public Task NextQuestionAsync(string code, NextQuestionOutcome outcome) =>
        outcome.NextQuestion is not null
            ? QuestionStartedAsync(code, outcome.NextQuestion)
            : hub.Clients.Group(code).SendAsync(GameHubEvents.GAME_ENDED, outcome.GameEnded);

    /// <summary>
    /// Việc phụ sau SubmitAnswer: người cuối vừa trả lời thì kết thúc câu ngay, không chờ vòng 1 giây của
    /// timer. Câu trả lời đã được lưu và ack đã có, nên mọi lỗi ở đây chỉ log - timer sẽ làm bù ở vòng sau.
    /// </summary>
    public async Task TryEndIfAllAnsweredAsync(Guid roomId, string code)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();

            // CancellationToken.None: kết nối của người vừa trả lời có thể đóng ngay sau ack, việc này không được bị hủy theo.
            var result = await gameService.TryAutoEndQuestionAsync(roomId, CancellationToken.None);
            if (result is { } r)
            {
                await QuestionEndedAsync(code, r.Ended, r.Leaderboard);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Tự kết thúc câu sau câu trả lời cuối thất bại (phòng {RoomCode}); timer sẽ thử lại.", code);
        }
    }
}
