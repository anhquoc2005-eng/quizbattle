using QuizBattle.Domain.Enums;

namespace QuizBattle.Application.Questions;

/// <summary>Trạng thái phòng đọc được ngay sau khi khóa dòng Room (SELECT ... FOR UPDATE) trong transaction.</summary>
public sealed record RoomSnapshot(RoomStatus Status, int QuestionCount, IReadOnlyList<string> ExistingTexts, int NextOrderIndex);
