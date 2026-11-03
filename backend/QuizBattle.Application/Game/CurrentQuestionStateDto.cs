namespace QuizBattle.Application.Game;

/// <summary>
/// Dùng trong RoomState khi có người vào lại phòng. CorrectIndex CHỈ có giá trị khi Ended = true (câu đã
/// kết thúc) - câu đang chạy không bao giờ lộ đáp án đúng qua DTO này.
/// </summary>
public sealed record CurrentQuestionStateDto(
    Guid QuestionId,
    int Index,
    int Total,
    string Text,
    string[] Options,
    DateTimeOffset StartedAt,
    DateTimeOffset Deadline,
    bool Ended,
    int? CorrectIndex,
    DateTimeOffset? NextQuestionAt);
