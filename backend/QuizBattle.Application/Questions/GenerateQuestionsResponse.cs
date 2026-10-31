namespace QuizBattle.Application.Questions;

public sealed record GenerateQuestionsResponse(int Requested, int Created, IReadOnlyList<QuestionResponse> Questions);
