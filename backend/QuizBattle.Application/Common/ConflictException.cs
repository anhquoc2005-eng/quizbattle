namespace QuizBattle.Application.Common;

/// <summary>Ném khi request xung đột với trạng thái hiện có (ví dụ email đã tồn tại). GlobalExceptionHandler map sang 409.</summary>
public sealed class ConflictException(string message) : Exception(message);
