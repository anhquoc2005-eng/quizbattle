namespace QuizBattle.Application.Game;

public sealed record QuestionEndedDto(Guid QuestionId, int CorrectIndex, int AnsweredCount, int TotalPlayers);
