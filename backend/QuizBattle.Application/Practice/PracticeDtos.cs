namespace QuizBattle.Application.Practice;

/// <summary>
/// Một câu của lượt ôn, gửi cho client. KHÔNG khai báo <c>CorrectIndex</c> (không chỉ là không gán): cùng nguyên tắc với
/// <c>QuestionStartedDto</c>, để không có cách nào vô tình serialize đáp án của câu chưa nộp.
/// </summary>
public sealed record PracticeQuestionDto(Guid QuestionId, int Index, string Text, string[] Options);

/// <summary>Kết quả đã lưu của một câu đã nộp. Có CorrectIndex vì câu đã chấm (không lộ thêm gì). XpReason null = nhận đủ XP.</summary>
public sealed record PracticeResultDto(Guid QuestionId, bool IsCorrect, int CorrectIndex, int XpAwarded, string? XpReason);

public sealed record PracticeSummaryDto(int CorrectCount, int XpGainedInSession, int XpGainedToday, int RemainingToday, DateTimeOffset ResetAt);

public sealed record PracticeSessionDto(
    Guid SessionId,
    string Topic,
    int Total,
    int AnsweredCount,
    bool Completed,
    PracticeQuestionDto? Current,
    PracticeResultDto? LastResult,
    PracticeSummaryDto? Summary);

/// <summary>Trả về khi nộp (kể cả phát lại). XpGainedToday/RemainingToday/Completed là trạng thái HIỆN TẠI, không đóng băng.</summary>
public sealed record PracticeAnswerResultDto(
    bool IsCorrect,
    int CorrectIndex,
    int XpAwarded,
    string? XpReason,
    int XpGainedToday,
    int RemainingToday,
    bool Completed);

public sealed record PracticeRoomDto(string RoomCode, string Topic, DateTimeOffset FinishedAt, int QuestionCount, string Role, Guid? OpenSessionId);

public sealed record PracticeRoomsResponse(
    int XpPerCorrect,
    int DailyXpCap,
    int XpGainedToday,
    int RemainingToday,
    DateTimeOffset ResetAt,
    IReadOnlyList<PracticeRoomDto> Rooms);

public sealed record PracticeSessionRefDto(Guid SessionId, int Total);

/// <summary>Created = true khi vừa tạo (201); false khi trả lại lượt đang mở (200).</summary>
public sealed record PracticeSessionStartDto(PracticeSessionRefDto Session, bool Created);

public sealed record StartPracticeSessionRequest(string RoomCode);

public sealed record SubmitPracticeAnswerRequest(Guid QuestionId, int SelectedIndex);
