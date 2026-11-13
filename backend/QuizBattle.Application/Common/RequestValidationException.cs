namespace QuizBattle.Application.Common;

/// <summary>
/// Input sai ở một trường (query/body) mà DataAnnotations không bắt được. GlobalExceptionHandler map sang 400 kèm <c>errors</c>.
/// Code (tùy chọn, vd "unknown_variant") để client phân biệt nguyên nhân; có giá trị thì ghi vào ProblemDetails.
/// </summary>
public sealed class RequestValidationException(string field, string message, string? code = null) : Exception(message)
{
    public string Field { get; } = field;

    public string? Code { get; } = code;
}
