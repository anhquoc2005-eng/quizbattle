namespace QuizBattle.Application.Common;

/// <summary>Ném khi không gọi được dịch vụ AI (thiếu cấu hình, timeout, lỗi mạng, AI trả 429/5xx). GlobalExceptionHandler map sang 503.</summary>
public sealed class AiUnavailableException(string message) : Exception(message);
