using QuizBattle.Domain.Enums;

namespace QuizBattle.Application.Practice;

/// <summary>Phòng ôn được của một người dùng (Role: "host" hoặc "player").</summary>
public sealed record PracticeRoomRow(string Code, string Topic, DateTimeOffset FinishedAt, int QuestionCount, string Role, Guid? OpenSessionId);

/// <summary>Phòng đủ điều kiện ôn kèm các câu đã chơi theo thứ tự gốc (chưa xáo).</summary>
public sealed record PlayableRoom(Guid RoomId, string Code, string Topic, IReadOnlyList<Guid> QuestionIds);

public sealed record PracticeSessionStart(Guid SessionId, int Total, bool Created);

/// <summary>Trạng thái một lượt đọc từ DB (Total chỉ đếm câu còn tồn tại).</summary>
public sealed record PracticeSessionState(
    Guid SessionId,
    string Topic,
    int Total,
    int AnsweredCount,
    bool Completed,
    PracticeQuestionDto? Current,
    PracticeResultDto? LastResult,
    int CorrectCount,
    int XpGainedInSession);

/// <summary>Lệnh nộp đáp án. Day = VietnamDay.DayOf(Now), do service tính (hàm thuần).</summary>
public sealed record PracticeSubmitCommand(
    Guid UserId,
    Guid SessionId,
    Guid QuestionId,
    int SelectedIndex,
    DateTimeOffset Now,
    DateOnly Day,
    int XpPerCorrect,
    int DailyXpCap);

/// <summary>Replayed = true khi câu đã được chấm trước đó và đây là kết quả đã lưu.</summary>
public sealed record PracticeSubmitOutcome(bool IsCorrect, int CorrectIndex, int XpAwarded, PracticeXpReason? XpReason, bool Completed, bool Replayed);
