namespace QuizBattle.Application.Game;

public sealed record QuestionEndedDto(
    Guid QuestionId,
    int CorrectIndex,
    int AnsweredCount,
    int TotalPlayers,
    // Server tự chuyển câu sau Room.RESULT_DISPLAY_SECONDS: client đếm ngược tới NextQuestionAt (đã bù offset đồng hồ).
    DateTimeOffset EndedAt,
    DateTimeOffset NextQuestionAt);
