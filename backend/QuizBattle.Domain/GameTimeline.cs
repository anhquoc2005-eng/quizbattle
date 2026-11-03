using QuizBattle.Domain.Entities;

namespace QuizBattle.Domain;

/// <summary>Điều kiện tự kết thúc câu / tự chuyển câu - hàm thuần, thời gian hiện tại truyền vào để test được.</summary>
public static class GameTimeline
{
    /// <summary>Câu đang chạy (chưa kết thúc) và đã quá hạn, hoặc có người chơi và ai cũng đã trả lời.</summary>
    public static bool ShouldEndQuestion(
        DateTimeOffset startedAt, DateTimeOffset? endedAt, int timeLimitSeconds, int answeredCount, int totalPlayers, DateTimeOffset now) =>
        endedAt is null
        && (now >= startedAt.AddSeconds(timeLimitSeconds) || (totalPlayers > 0 && answeredCount >= totalPlayers));

    /// <summary>Câu đã kết thúc và đã hiện kết quả đủ RESULT_DISPLAY_SECONDS.</summary>
    public static bool ShouldAdvance(DateTimeOffset? endedAt, DateTimeOffset now) =>
        endedAt is not null && now >= endedAt.Value.AddSeconds(Room.RESULT_DISPLAY_SECONDS);
}
