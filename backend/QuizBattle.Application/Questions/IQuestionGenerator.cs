namespace QuizBattle.Application.Questions;

/// <summary>Sinh câu hỏi trắc nghiệm bằng AI. Implement ở Infrastructure (gọi Gemini), fake được trong test.</summary>
public interface IQuestionGenerator
{
    /// <summary>
    /// Ném AiUnavailableException nếu không gọi được AI (lỗi mạng/timeout/429/5xx/thiếu cấu hình).
    /// Ném AiResponseFormatException nếu AI trả về nhưng JSON sai định dạng/thiếu field.
    /// </summary>
    Task<IReadOnlyList<QuestionCandidate>> GenerateAsync(string topic, int count, CancellationToken cancellationToken);
}
