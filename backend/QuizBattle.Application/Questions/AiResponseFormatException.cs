namespace QuizBattle.Application.Questions;

/// <summary>
/// Tín hiệu nội bộ: AI trả JSON sai định dạng/thiếu field. QuestionService bắt để thử lại tối đa 1 lần,
/// không bao giờ để lọt ra ngoài (nếu lọt ra GlobalExceptionHandler sẽ trả 500 thay vì 502).
/// </summary>
public sealed class AiResponseFormatException(string message) : Exception(message);
