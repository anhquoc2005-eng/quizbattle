namespace QuizBattle.Application.Common;

/// <summary>Input sai ở một trường (query/body) mà DataAnnotations không bắt được. GlobalExceptionHandler map sang 400 kèm <c>errors</c>.</summary>
public sealed class RequestValidationException(string field, string message) : Exception(message)
{
    public string Field { get; } = field;
}
