using QuizBattle.Domain.Entities;

namespace QuizBattle.Application.Game;

/// <summary>Player là entity đang được EF theo dõi (IGameRepository.SubmitAnswerAsync) - Service sửa Score trực tiếp lên nó.</summary>
public sealed class SubmitAnswerSnapshot
{
    public required Player Player { get; init; }

    public required Room Room { get; init; }

    /// <summary>Câu đang chạy của phòng này (StartedAt có giá trị, EndedAt chưa có) - null nếu không có câu nào đang chạy.</summary>
    public Question? Question { get; init; }

    public int AnsweredCountForCurrentQuestion { get; init; }

    public int TotalPlayers { get; init; }
}
