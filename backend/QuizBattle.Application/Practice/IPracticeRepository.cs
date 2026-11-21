namespace QuizBattle.Application.Practice;

public interface IPracticeRepository
{
    /// <summary>
    /// Phòng ôn được: Finished AND (chủ phòng OR có Player.UserId = userId) AND có MatchReward Awarded của phòng AND có câu
    /// Approved đã kết thúc (EndedAt != null). Một truy vấn projection, mới nhất trước.
    /// </summary>
    Task<IReadOnlyList<PracticeRoomRow>> ListRoomsAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>XP ôn tập đã nhận trong ngày VN <paramref name="day"/>; 0 nếu chưa có dòng.</summary>
    Task<int> GetXpTodayAsync(Guid userId, DateOnly day, CancellationToken cancellationToken);

    /// <summary>Phòng theo mã nếu user đủ điều kiện ôn (cùng luật với ListRoomsAsync), ngược lại null.</summary>
    Task<PlayableRoom?> FindPlayableRoomAsync(Guid userId, string roomCode, CancellationToken cancellationToken);

    /// <summary>
    /// Mở lượt với thứ tự câu đã xáo, hoặc trả lại lượt đang mở của (user, phòng): một câu INSERT ... ON CONFLICT DO NOTHING
    /// trong transaction READ COMMITTED rồi đọc lại bằng câu lệnh riêng (research R11).
    /// </summary>
    Task<PracticeSessionStart> CreateOrGetSessionAsync(Guid userId, PlayableRoom room, IReadOnlyList<Guid> shuffledQuestionIds, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Trạng thái lượt (ràng buộc UserId); null nếu không có hoặc không thuộc user. Tự đóng lượt khi không còn câu nào để làm.</summary>
    Task<PracticeSessionState?> GetSessionAsync(Guid userId, Guid sessionId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>
    /// Nộp đáp án trong MỘT transaction: khóa Users FOR UPDATE, phát lại nếu đã chấm, chấm, giữ chỗ XP bằng một câu SQL, ghi dòng XP,
    /// cộng TotalXp. Ném NotFoundException (lượt/câu không thuộc user) hoặc ConflictException "not_current_question".
    /// </summary>
    Task<PracticeSubmitOutcome> SubmitAnswerAsync(PracticeSubmitCommand command, CancellationToken cancellationToken);
}
