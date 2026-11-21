namespace QuizBattle.Application.Practice;

public interface IPracticeService
{
    Task<PracticeRoomsResponse> GetRoomsAsync(Guid userId, CancellationToken cancellationToken);

    Task<PracticeSessionStartDto> StartSessionAsync(Guid userId, StartPracticeSessionRequest request, CancellationToken cancellationToken);

    Task<PracticeSessionDto> GetSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);

    Task<PracticeAnswerResultDto> SubmitAnswerAsync(Guid userId, Guid sessionId, SubmitPracticeAnswerRequest request, CancellationToken cancellationToken);
}
