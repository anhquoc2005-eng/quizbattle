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
public sealed class GameHub(IGameService gameService, GameBroadcaster broadcaster, PlayerConnectionRegistry connections) : Hub
{
    private const string RoomCodeKey = "roomCode";
    private const string PlayerIdKey = "playerId";
    private const string IsHostKey = "isHost";
    private const string SessionTokenHashKey = "sessionTokenHash";

    public async Task JoinRoom(string code, string? sessionToken)
    {
        if (!string.IsNullOrEmpty(sessionToken))
        {
            var (state, broadcast, sessionTokenHash) = await gameService.JoinAsPlayerAsync(code, sessionToken, Context.ConnectionAborted);
            Context.Items[RoomCodeKey] = state.Code;
            Context.Items[PlayerIdKey] = state.PlayerId;
            Context.Items[IsHostKey] = false;
            Context.Items[SessionTokenHashKey] = sessionTokenHash;

            // Cùng tài khoản vừa vào từ thiết bị khác (hash khác): gỡ kết nối cũ khỏi group và báo cho nó. Hub không Abort
            // được kết nối khác, nên "ngắt" = hết nhận broadcast + mọi hành động ghi bị từ chối (so hash ở SubmitAnswer).
            var replaced = connections.Register(state.PlayerId!.Value, Context.ConnectionId, sessionTokenHash);
            if (replaced is not null)
            {
                await Groups.RemoveFromGroupAsync(replaced, state.Code);
                await Clients.Client(replaced).SendAsync(
                    GameHubEvents.ERROR,
                    new GameErrorDto(nameof(JoinRoom), "session_replaced", "Bạn đã vào phòng từ thiết bị khác."));
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, state.Code);
            await Clients.Caller.SendAsync(GameHubEvents.ROOM_STATE, state);
            await Clients.OthersInGroup(state.Code).SendAsync(GameHubEvents.PLAYER_JOINED, broadcast);
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
        await Clients.Caller.SendAsync(GameHubEvents.ROOM_STATE, hostState);
    }

    public async Task StartGame()
    {
        var code = RequireHostRoomCode();
        var started = await gameService.StartGameAsync(Context.User!.GetUserId(), code, Context.ConnectionAborted);
        await broadcaster.QuestionStartedAsync(code, started);
    }

    public async Task EndQuestion()
    {
        var code = RequireHostRoomCode();
        var (ended, leaderboard) = await gameService.EndQuestionAsync(Context.User!.GetUserId(), code, Context.ConnectionAborted);
        await broadcaster.QuestionEndedAsync(code, ended, leaderboard);
    }

    public async Task NextQuestion()
    {
        var code = RequireHostRoomCode();
        var outcome = await gameService.NextQuestionAsync(Context.User!.GetUserId(), code, Context.ConnectionAborted);
        await broadcaster.NextQuestionAsync(code, outcome);
    }

    // Kiểu trả về nullable: khi GameHubExceptionFilter bắt exception, nó trả null (đã gửi Error riêng cho
    // người gọi) - không có AnswerAckDto thật ở nhánh đó.
    public async Task<AnswerAckDto?> SubmitAnswer(Guid questionId, int selectedIndex)
    {
        var playerId = RequirePlayerId();
        var code = RequireRoomCode();

        var sessionTokenHash = Context.Items.TryGetValue(SessionTokenHashKey, out var hashValue) && hashValue is string hash
            ? hash
            : throw new UnauthorizedException("Chưa vào phòng với vai trò người chơi.");

        var (ack, progress, roomId) = await gameService.SubmitAnswerAsync(playerId, sessionTokenHash, questionId, selectedIndex, Context.ConnectionAborted);
        await Clients.Group(HostGroup(code)).SendAsync(GameHubEvents.ANSWER_PROGRESS, progress);

        if (progress.AnsweredCount >= progress.TotalPlayers)
        {
            // Việc phụ, tự bắt và log lỗi bên trong - không await để ack trả ngay, và lỗi không bao giờ tới người chơi.
            _ = broadcaster.TryEndIfAllAnsweredAsync(roomId, code);
        }

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
        else if (Context.Items.TryGetValue(PlayerIdKey, out var playerIdValue) && playerIdValue is Guid playerId
            && connections.Unregister(playerId, Context.ConnectionId))
        {
            // Unregister = false: kết nối này đã bị thiết bị khác thay thế, người chơi vẫn online ở đó - không phát PlayerLeft.
            await Clients.Group(code).SendAsync(GameHubEvents.PLAYER_LEFT, new PlayerLeftDto(playerId));
        }

        Context.Items.Clear();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Rớt mạng (không chủ động LeaveRoom): KHÔNG xóa Player - chỉ báo cho người khác. Điểm/phiên vẫn
        // giữ nguyên để JoinRoom lại bằng sessionToken cũ.
        if (TryGetRoomCode(out var code)
            && Context.Items.TryGetValue(PlayerIdKey, out var playerIdValue) && playerIdValue is Guid playerId
            && connections.Unregister(playerId, Context.ConnectionId))
        {
            await Clients.Group(code).SendAsync(GameHubEvents.PLAYER_LEFT, new PlayerLeftDto(playerId));
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
