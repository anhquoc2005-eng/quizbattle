namespace QuizBattle.Domain.Entities;

/// <summary>
/// Một lượt ôn tập của một người dùng trên một phòng đã kết thúc. QuestionIds là thứ tự câu hỏi (đã xáo) chốt lúc tạo.
/// RoomId nullable (SET NULL khi xóa phòng); RoomCode/RoomTopic là bản sao để không cần join Rooms.
/// Mỗi (UserId, RoomId) chỉ có tối đa một lượt đang mở (CompletedAt null).
/// </summary>
public sealed class PracticeSession
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid? RoomId { get; set; }

    public required string RoomCode { get; set; }

    public required string RoomTopic { get; set; }

    public required Guid[] QuestionIds { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}
