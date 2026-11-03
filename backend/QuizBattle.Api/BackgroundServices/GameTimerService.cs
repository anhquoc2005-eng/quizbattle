using QuizBattle.Api.Hubs;
using QuizBattle.Application.Game;

namespace QuizBattle.Api.BackgroundServices;

/// <summary>
/// Mỗi giây quét các phòng có câu quá hạn / cần chuyển câu và xử lý. Không giữ timer trong RAM: mọi mốc
/// thời gian đọc từ DB, nên khởi động lại backend không làm mất lượt; nhiều server chạy cùng lúc cũng không
/// kết thúc một câu hai lần nhờ FOR UPDATE + kiểm tra EndedAt trong GameService.
/// </summary>
public sealed class GameTimerService(IServiceScopeFactory scopeFactory, GameBroadcaster broadcaster, ILogger<GameTimerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                // Bọc TOÀN BỘ vòng lặp (kể cả truy vấn danh sách phòng): từ .NET 6, exception chưa bắt trong
                // ExecuteAsync dừng cả host, nên DB chập chờn một lần sẽ làm sập backend. Chỉ thoát khi bị hủy.
                try
                {
                    await TickAsync(stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(ex, "Vòng quét timer trận thất bại, thử lại ở vòng sau.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Backend đang tắt - thoát bình thường.
        }
    }

    private async Task TickAsync(CancellationToken stoppingToken)
    {
        List<DueRoom> dueRooms;
        using (var scope = scopeFactory.CreateScope())
        {
            var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();
            dueRooms = [.. await gameService.FindDueRoomsAsync(stoppingToken)];
        }

        foreach (var room in dueRooms)
        {
            // Lỗi 1 phòng chỉ log: không làm dừng vòng lặp hay ảnh hưởng các phòng còn lại.
            try
            {
                await ProcessRoomAsync(room, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Xử lý timer cho phòng {RoomCode} thất bại.", room.Code);
            }
        }
    }

    private async Task ProcessRoomAsync(DueRoom room, CancellationToken stoppingToken)
    {
        // Scope riêng mỗi phòng: DbContext là scoped (không an toàn dùng chung/giữ lâu), và scope mới đảm bảo
        // entity đã theo dõi của phòng trước không lẫn sang phòng sau.
        using var scope = scopeFactory.CreateScope();
        var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();

        if (await gameService.TryAutoEndQuestionAsync(room.Id, stoppingToken) is { } ended)
        {
            await broadcaster.QuestionEndedAsync(room.Code, ended.Ended, ended.Leaderboard);
        }

        if (await gameService.TryAutoAdvanceAsync(room.Id, stoppingToken) is { } outcome)
        {
            await broadcaster.NextQuestionAsync(room.Code, outcome);
        }
    }
}
