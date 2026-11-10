namespace QuizBattle.Application.Common;

/// <summary>
/// Ném khi session token của kết nối không còn là token hiện tại của người chơi (tài khoản đã vào lại từ thiết
/// bị khác). Ý nghĩa như 401; GameHubExceptionFilter map sang mã lỗi riêng "session_replaced".
/// </summary>
public sealed class SessionReplacedException(string message) : Exception(message);
