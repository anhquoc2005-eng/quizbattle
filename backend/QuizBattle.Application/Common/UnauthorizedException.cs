namespace QuizBattle.Application.Common;

/// <summary>
/// Ném khi xác thực thất bại (sai thông tin đăng nhập, token không hợp lệ...). GlobalExceptionHandler map sang 401.
/// Code (tùy chọn, vd "token_expired") để client phân biệt nguyên nhân; có giá trị thì ghi vào ProblemDetails.
/// </summary>
public sealed class UnauthorizedException(string message, string? code = null) : Exception(message)
{
    public string? Code { get; } = code;
}
