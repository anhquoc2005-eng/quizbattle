namespace QuizBattle.Application.Questions;

public interface IQuestionService
{
    Task<IReadOnlyList<QuestionResponse>> GetQuestionsAsync(Guid hostUserId, string code, CancellationToken cancellationToken);

    Task<GenerateQuestionsResponse> GenerateAsync(Guid hostUserId, string code, GenerateQuestionsRequest request, CancellationToken cancellationToken);

    Task<QuestionResponse> AddAsync(Guid hostUserId, string code, QuestionInput input, CancellationToken cancellationToken);

    Task<QuestionResponse> UpdateAsync(Guid hostUserId, string code, Guid questionId, QuestionInput input, CancellationToken cancellationToken);

    Task<QuestionResponse> ApproveAsync(Guid hostUserId, string code, Guid questionId, CancellationToken cancellationToken);

    Task DeleteAsync(Guid hostUserId, string code, Guid questionId, CancellationToken cancellationToken);
}
