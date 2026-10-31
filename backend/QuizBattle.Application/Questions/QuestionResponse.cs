namespace QuizBattle.Application.Questions;

public sealed record QuestionResponse(
    Guid Id,
    int OrderIndex,
    string Text,
    string[] Options,
    int CorrectIndex,
    string ReviewStatus);
