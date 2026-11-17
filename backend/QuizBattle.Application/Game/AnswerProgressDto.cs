namespace QuizBattle.Application.Game;

/// <summary>
/// Gửi riêng cho chủ phòng (nhóm "{code}:host") mỗi khi có người trả lời - không gửi cho người chơi.
/// AnsweredPlayerIds là các answer đã commit mà giao dịch của người vừa nộp nhìn thấy, cộng chính người đó. Hai người nộp
/// đồng thời (phòng chỉ khóa FOR SHARE) có thể mỗi payload thiếu người kia nên client phải GỘP HỢP chứ không ghi đè.
/// </summary>
public sealed record AnswerProgressDto(int AnsweredCount, int TotalPlayers, IReadOnlyList<Guid> AnsweredPlayerIds);
