namespace QuizBattle.Application.Game;

/// <summary>
/// Gửi khi 1 câu bắt đầu chạy. KHÔNG có CorrectIndex - cố tình không khai báo thuộc tính này (không chỉ
/// là không gán giá trị) để không có cách nào vô tình serialize đáp án đúng ra trước khi câu kết thúc.
/// </summary>
public sealed record QuestionStartedDto(
    Guid QuestionId,
    int Index,
    int Total,
    string Text,
    string[] Options,
    DateTimeOffset StartedAt,
    DateTimeOffset Deadline,
    // Giống RoomStateDto.ServerTime - client tính offset đồng hồ từ đây. Luôn bằng StartedAt ở DTO này
    // (cùng thời điểm tạo), tách riêng field cho rõ ý nghĩa (đây là "giờ server", không phải nghiệp vụ).
    DateTimeOffset ServerTime);
