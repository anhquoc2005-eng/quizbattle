namespace QuizBattle.Application.Game;

/// <summary>Gửi riêng cho chủ phòng (nhóm "{code}:host") mỗi khi có người trả lời - không gửi cho người chơi.</summary>
public sealed record AnswerProgressDto(int AnsweredCount, int TotalPlayers);
