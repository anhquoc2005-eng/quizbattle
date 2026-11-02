using Microsoft.AspNetCore.SignalR;
using QuizBattle.Application.Common;

namespace QuizBattle.Api.Hubs;

/// <summary>
/// Bắt exception cho toàn bộ method của GameHub ở một chỗ - không try/catch rải rác trong từng method.
/// Exception nghiệp vụ (Conflict/Unauthorized/NotFound) -> gửi Error với message gốc cho đúng người gọi.
/// Exception khác -> log, gửi Error chung chung, không lộ chi tiết (stack trace, thông tin nội bộ).
/// </summary>
public sealed class GameHubExceptionFilter(IHubContext<GameHub> hubContext, ILogger<GameHubExceptionFilter> logger) : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        try
        {
            return await next(invocationContext);
        }
        catch (Exception ex) when (ex is ConflictException or UnauthorizedException or NotFoundException)
        {
            await hubContext.Clients.Client(invocationContext.Context.ConnectionId).SendAsync(GameHubEvents.ERROR, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi chưa xử lý trong GameHub.{Method}", invocationContext.HubMethodName);
            await hubContext.Clients.Client(invocationContext.Context.ConnectionId).SendAsync(GameHubEvents.ERROR, "Đã xảy ra lỗi, vui lòng thử lại.");
            return null;
        }
    }
}
