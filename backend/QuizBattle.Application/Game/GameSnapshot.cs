using QuizBattle.Domain.Entities;

namespace QuizBattle.Application.Game;

/// <summary>
/// Trạng thái phòng đọc được khi khóa dòng Room (SELECT ... FOR UPDATE) hoặc khi đọc không khóa cho
/// JoinRoom. Room/ApprovedQuestions/Players là entity đang được EF theo dõi (trong IGameRepository.WithLockedRoomAsync) -
/// Service sửa trực tiếp lên chúng, repository tự lưu sau khi gọi callback.
/// </summary>
public sealed class GameSnapshot
{
    public required Room Room { get; init; }

    /// <summary>Câu đã duyệt, theo đúng OrderIndex.</summary>
    public required IReadOnlyList<Question> ApprovedQuestions { get; init; }

    public required IReadOnlyList<Player> Players { get; init; }

    /// <summary>Toàn bộ đáp án đã nộp trong phòng (để tính bảng xếp hạng, hòa điểm theo tổng thời gian trả lời đúng).</summary>
    public required IReadOnlyList<Answer> Answers { get; init; }

    /// <summary>Tổng số câu hỏi trong phòng (cả Pending lẫn Approved) - dùng để map RoomResponse.QuestionCount.</summary>
    public int TotalQuestionCount { get; init; }

    public int AnsweredCountForCurrentQuestion { get; init; }

    /// <summary>
    /// Câu mới bắt đầu gần nhất - có thể đang chạy (EndedAt null) hoặc vừa kết thúc nhưng chủ phòng chưa
    /// bấm câu tiếp (EndedAt có giá trị). Dùng broadened definition này để người vào lại phòng ngay sau khi
    /// EndQuestion vẫn thấy đúng câu đó (kèm đáp án đúng), không bị null.
    /// </summary>
    public Question? CurrentQuestion => ApprovedQuestions
        .Where(q => q.StartedAt is not null)
        .OrderByDescending(q => q.StartedAt)
        .FirstOrDefault();
}
