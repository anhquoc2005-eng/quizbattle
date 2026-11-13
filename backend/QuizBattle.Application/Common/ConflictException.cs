namespace QuizBattle.Application.Common;

/// <summary>
/// Ném khi request xung đột với trạng thái hiện có (ví dụ email đã tồn tại). GlobalExceptionHandler map sang 409.
/// Code (tùy chọn, vd "already_owned") để client phân biệt nguyên nhân; có giá trị thì ghi vào ProblemDetails.
/// </summary>
public sealed class ConflictException(string message, string? code = null) : Exception(message)
{
    public string? Code { get; } = code;
}
