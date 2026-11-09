using System.Collections.Concurrent;

namespace QuizBattle.Api.Hubs;

/// <summary>
/// Kết nối đang giữ mỗi người chơi (playerId -> connectionId + hash phiên). Chỉ để chủ động ngắt thiết bị cũ khi cùng
/// tài khoản vào lại từ thiết bị khác (cố gắng tối đa, một instance); xác thực thật nằm ở việc so hash phiên với DB
/// trong SubmitAnswer. Singleton, thread-safe.
/// </summary>
public sealed class PlayerConnectionRegistry
{
    private readonly ConcurrentDictionary<Guid, (string ConnectionId, string TokenHash)> _connections = new();

    /// <summary>Đăng ký kết nối mới. Trả connectionId cũ cần đá nếu kết nối cũ dùng hash khác; cùng hash (làm mới trang) trả null.</summary>
    public string? Register(Guid playerId, string connectionId, string tokenHash)
    {
        string? kicked = null;
        _connections.AddOrUpdate(
            playerId,
            _ => (connectionId, tokenHash),
            (_, old) =>
            {
                kicked = old.TokenHash != tokenHash && old.ConnectionId != connectionId ? old.ConnectionId : null;
                return (connectionId, tokenHash);
            });
        return kicked;
    }

    /// <summary>
    /// Gỡ khi kết nối đóng/rời phòng. Chỉ gỡ nếu entry còn thuộc đúng connectionId này; trả false nghĩa là kết nối này đã bị
    /// thay thế (không còn là chủ của người chơi), nên không được phát PlayerLeft.
    /// </summary>
    public bool Unregister(Guid playerId, string connectionId) =>
        _connections.TryGetValue(playerId, out var current)
        && current.ConnectionId == connectionId
        && _connections.TryRemove(new KeyValuePair<Guid, (string, string)>(playerId, current));
}
