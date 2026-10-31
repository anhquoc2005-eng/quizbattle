namespace QuizBattle.Application.Common;

/// <summary>Ném khi AI trả kết quả nhưng không dùng được sau khi đã thử lại. GlobalExceptionHandler map sang 502.</summary>
public sealed class BadGatewayException(string message) : Exception(message);
