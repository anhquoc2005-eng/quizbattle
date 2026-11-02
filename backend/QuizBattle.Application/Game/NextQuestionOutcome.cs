namespace QuizBattle.Application.Game;

/// <summary>Kết quả NextQuestion: đúng 1 trong 2 giá trị khác null - còn câu thì NextQuestion, hết câu thì GameEnded.</summary>
public sealed record NextQuestionOutcome(QuestionStartedDto? NextQuestion, GameEndedDto? GameEnded);
