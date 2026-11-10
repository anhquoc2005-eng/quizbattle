using QuizBattle.Application.Profile;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Application.Auth;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Lưu user mới. Ném ConflictException nếu email đã tồn tại (bắt unique violation 23505 ở đây).</summary>
    Task AddAsync(User user, CancellationToken cancellationToken);

    /// <summary>Thưởng đã nhận (chỉ trận Awarded), mới nhất trước, đọc từ bản sao trong MatchRewards nên không cần phòng còn tồn tại.</summary>
    Task<IReadOnlyList<RewardHistoryItemDto>> GetRewardHistoryAsync(Guid userId, int limit, CancellationToken cancellationToken);

    /// <summary>Top theo TotalXp giảm dần (hòa: tạo sớm hơn trước), bỏ tài khoản 0 XP.</summary>
    Task<IReadOnlyList<XpRankRow>> GetTopByXpAsync(int count, CancellationToken cancellationToken);
}

public sealed record XpRankRow(string DisplayName, int TotalXp);
