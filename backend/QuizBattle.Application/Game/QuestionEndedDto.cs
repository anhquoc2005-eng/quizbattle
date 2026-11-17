namespace QuizBattle.Application.Game;

public sealed record QuestionEndedDto(
    Guid QuestionId,
    int CorrectIndex,
    int AnsweredCount,
    int TotalPlayers,
    // Server tự chuyển câu sau Room.RESULT_DISPLAY_SECONDS: client đếm ngược tới NextQuestionAt (đã bù offset đồng hồ).
    DateTimeOffset EndedAt,
    DateTimeOffset NextQuestionAt,
    // Người trả lời đúng câu này (gửi cả phòng - câu đã kết thúc nên đúng/sai không còn là bí mật). Người rời phòng/rớt mạng
    // không bị xóa khỏi trận nên vẫn có mặt nếu đã trả lời đúng.
    IReadOnlyList<Guid> CorrectPlayerIds);
