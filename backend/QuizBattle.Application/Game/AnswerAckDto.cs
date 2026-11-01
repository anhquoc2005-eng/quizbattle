namespace QuizBattle.Application.Game;

/// <summary>Trả về trực tiếp cho người vừa gọi SubmitAnswer - chỉ xác nhận đã nhận, không tiết lộ đúng/sai.</summary>
public sealed record AnswerAckDto(bool Received);
