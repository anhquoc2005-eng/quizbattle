namespace QuizBattle.Application.Common;

/// <summary>Ném khi không tìm thấy tài nguyên, hoặc tài nguyên thuộc về người khác (để không lộ sự tồn tại). GlobalExceptionHandler map sang 404.</summary>
public sealed class NotFoundException(string message) : Exception(message);
