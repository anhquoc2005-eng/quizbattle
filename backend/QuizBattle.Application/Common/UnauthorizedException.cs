namespace QuizBattle.Application.Common;

/// <summary>Ném khi xác thực thất bại (sai thông tin đăng nhập, token không hợp lệ...). GlobalExceptionHandler map sang 401.</summary>
public sealed class UnauthorizedException(string message) : Exception(message);
