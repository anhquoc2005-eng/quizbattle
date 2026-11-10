using QuizBattle.Domain.Enums;

namespace QuizBattle.Domain.Entities;

/// <summary>
/// Kết quả thưởng của một Player có tài khoản trong một trận (bất biến sau khi tạo). Khách không có dòng.
/// PlayerId/RoomId nullable (SET NULL khi xóa phòng) và RoomCode/RoomTopic/FinishedAt là bản sao,
/// để xóa phòng không làm mất thưởng và lịch sử hồ sơ không cần join Rooms.
/// </summary>
public sealed class MatchReward
{
    public Guid Id { get; set; }

    public Guid? PlayerId { get; set; }

    public Guid UserId { get; set; }

    public Guid? RoomId { get; set; }

    public required string RoomCode { get; set; }

    public required string RoomTopic { get; set; }

    public DateTimeOffset FinishedAt { get; set; }

    public RewardStatus Status { get; set; }

    /// <summary>Null khi <see cref="RewardStatus.Awarded"/>.</summary>
    public RewardDenialReason? DenialReason { get; set; }

    public int Score { get; set; }

    public int RewardRank { get; set; }

    public int Xp { get; set; }

    /// <summary>Xu phần điểm (floor(Score / 100)); tổng Xu nhận được = Coins + RankBonus.</summary>
    public int Coins { get; set; }

    public int RankBonus { get; set; }
}
