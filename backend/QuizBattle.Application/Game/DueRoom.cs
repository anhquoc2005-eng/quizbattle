namespace QuizBattle.Application.Game;

/// <summary>Phòng cần timer xử lý. Code dùng để gửi sự kiện vào đúng SignalR group (group đặt tên theo mã phòng).</summary>
public sealed record DueRoom(Guid Id, string Code);
