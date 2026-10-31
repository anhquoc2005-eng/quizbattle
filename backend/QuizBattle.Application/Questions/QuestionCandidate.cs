namespace QuizBattle.Application.Questions;

/// <summary>Một câu hỏi do AI sinh ra, trước khi được validate/xáo đáp án/lưu vào DB.</summary>
public sealed record QuestionCandidate(string Text, string[] Options, int CorrectIndex);
