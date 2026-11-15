using QuizBattle.Application.Auth;
using QuizBattle.Application.Profile;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Tests.TestHelpers;

/// <summary>Repo giả cho ProfileService: lịch sử thưởng đã được xếp sẵn (mới nhất trước), xếp hạng làm đúng quy tắc của repo thật.</summary>
public sealed class FakeUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];

    /// <summary>Nhân vật đã lưu theo UserId (không có khóa = chưa từng lưu).</summary>
    public Dictionary<Guid, UserAvatar> UserAvatars { get; } = [];

    /// <summary>Lịch sử của mọi người dùng, theo đúng thứ tự trả về (mới nhất trước).</summary>
    public List<RewardHistoryItemDto> Rewards { get; } = [];

    public int? LastLimit { get; private set; }

    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(Users.SingleOrDefault(u => u.Email == email));

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Users.SingleOrDefault(u => u.Id == id));

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        Users.Add(user);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RewardHistoryItemDto>> GetRewardHistoryAsync(Guid userId, int limit, CancellationToken cancellationToken)
    {
        LastLimit = limit;
        return Task.FromResult<IReadOnlyList<RewardHistoryItemDto>>(Rewards.Take(limit).ToList());
    }

    public Task<IReadOnlyList<XpRankRow>> GetTopByXpAsync(int count, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<XpRankRow>>(Users
            .Where(u => u.TotalXp > 0)
            .OrderByDescending(u => u.TotalXp).ThenBy(u => u.CreatedAt).ThenBy(u => u.Id)
            .Take(count)
            .Select(u => new XpRankRow(u.Id, u.DisplayName, u.TotalXp, UserAvatars.GetValueOrDefault(u.Id)))
            .ToList());
}
