using Microsoft.AspNetCore.SignalR;
using QuizBattle.Api.Extensions;
using QuizBattle.Application.Common;
using QuizBattle.Application.Game;

namespace QuizBattle.Api.Hubs;

/// <summary>
/// Hub chỉ nhận lời gọi và chuyển cho IGameService - không chứa logic nghiệp vụ, không giữ state trong
/// field (field dùng chung cho mọi kết nối). playerId/roomCode/isHost gắn vào Context.Items (riêng từng
/// kết nối) sau khi JoinRoom thành công. Exception từ GameService được GameHubExceptionFilter bắt chung,
/// không try/catch trong các method dưới đây.
/// </summary>
public sealed class GameHub(IGameService gameService) : Hub
{
    private const string RoomCodeKey = "roomCode";
    private const string PlayerIdKey = "playerId";
    private const string IsHostKey = "isHost";

    public async Task JoinRoom(string code, string? sessionToken)
    {
        if (!string.IsNullOrEmpty(sessionToken))
        {
            var (state, broadcast) = await gameService.JoinAsPlayerAsync(code, sessionToken, Context.ConnectionAborted);
            Context.Items[RoomCodeKey] = state.Code;
            Context.Items[PlayerIdKey] = state.PlayerId;
            Context.Items[IsHostKey] = false;

            await Groups.AddToGroupAsync(Context.ConnectionId, state.Code);
            await Clients.Caller.SendAsync("RoomState", state);
            await Clients.OthersInGroup(state.Code).SendAsync("PlayerJoined", broadcast);
            return;
        }

        if (Context.User?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedException("Cần đăng nhập để vào phòng với vai trò chủ phòng.");
        }

        var hostState = await gameService.JoinAsHostAsync(Context.User.GetUserId(), code, Context.ConnectionAborted);
        Context.Items[RoomCodeKey] = hostState.Code;
        Context.Items[IsHostKey] = true;

        await Groups.AddToGroupAsync(Context.ConnectionId, hostState.Code);
        await Groups.AddToGroupAsync(Context.ConnectionId, HostGroup(hostState.Code));
        await Clients.Caller.SendAsync("RoomState", hostState);
    }

    public async Task StartGame()
    {
        var code = RequireHostRoomCode();
        var started = await gameService.StartGameAsync(Context.User!.GetUserId(), code, Context.ConnectionAborted);
        await Clients.Group(code).SendAsync("QuestionStarted", started);
    }

    public async Task EndQuestion()
    {
        var code = RequireHostRoomCode();
        var (ended, leaderboard) = await gameService.EndQuestionAsync(Context.User!.GetUserId(), code, Context.ConnectionAborted);
        await Clients.Group(code).SendAsync("QuestionEnded", ended);
        await Clients.Group(code).SendAsync("LeaderboardUpdated", leaderboard);
    }

    public async Task NextQuestion()
    {
        var code = RequireHostRoomCode();
        var outcome = await gameService.NextQuestionAsync(Context.User!.GetUserId(), code, Context.ConnectionAborted);

        if (outcome.NextQuestion is not null)
        {
            await Clients.Group(code).SendAsync("QuestionStarted", outcome.NextQuestion);
        }
        else
        {
            await Clients.Group(code).SendAsync("GameEnded", outcome.GameEnded);
        }
    }

    // Kiểu trả về nullable: khi GameHubExceptionFilter bắt exception, nó trả null (đã gửi Error riêng cho
    // người gọi) - không có AnswerAckDto thật ở nhánh đó.
    public async Task<AnswerAckDto?> SubmitAnswer(Guid questionId, int selectedIndex)
    {
        var playerId = RequirePlayerId();
        var code = RequireRoomCode();

        var (ack, progress) = await gameService.SubmitAnswerAsync(playerId, questionId, selectedIndex, Context.ConnectionAborted);
        await Clients.Group(HostGroup(code)).SendAsync("AnswerProgress", progress);
        return ack;
    }

    public async Task LeaveRoom()
    {
        if (!TryGetRoomCode(out var code))
        {
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, code);

        if (Context.Items.TryGetValue(IsHostKey, out var isHostValue) && isHostValue is true)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, HostGroup(code));
        }
        else if (Context.Items.TryGetValue(PlayerIdKey, out var playerIdValue) && playerIdValue is Guid playerId)
        {
            await Clients.Group(code).SendAsync("PlayerLeft", new PlayerLeftDto(playerId));
        }

        Context.Items.Clear();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Rớt mạng (không chủ động LeaveRoom): KHÔNG xóa Player - chỉ báo cho người khác. Điểm/phiên vẫn
        // giữ nguyên để JoinRoom lại bằng sessionToken cũ.
        if (TryGetRoomCode(out var code)
            && Context.Items.TryGetValue(PlayerIdKey, out var playerIdValue) && playerIdValue is Guid playerId)
        {
            await Clients.Group(code).SendAsync("PlayerLeft", new PlayerLeftDto(playerId));
        }

        await base.OnDisconnectedAsync(exception);
    }

    private bool TryGetRoomCode(out string code)
    {
        if (Context.Items.TryGetValue(RoomCodeKey, out var value) && value is string found)
        {
            code = found;
            return true;
        }

        code = "";
        return false;
    }

    private string RequireRoomCode() =>
        TryGetRoomCode(out var code) ? code : throw new UnauthorizedException("Chưa vào phòng.");

    private string RequireHostRoomCode()
    {
        var code = RequireRoomCode();
        return Context.Items.TryGetValue(IsHostKey, out var isHostValue) && isHostValue is true
            ? code
            : throw new UnauthorizedException("Chỉ chủ phòng mới thao tác được.");
    }

    private Guid RequirePlayerId() =>
        Context.Items.TryGetValue(PlayerIdKey, out var value) && value is Guid playerId
            ? playerId
            : throw new UnauthorizedException("Chưa vào phòng với vai trò người chơi.");

    private static string HostGroup(string code) => $"{code}:host";
}
