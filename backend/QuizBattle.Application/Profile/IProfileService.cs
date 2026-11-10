namespace QuizBattle.Application.Profile;

public interface IProfileService
{
    Task<ProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Lịch sử thưởng mới nhất trước; <paramref name="limit"/> phải trong 1-50.</summary>
    Task<IReadOnlyList<RewardHistoryItemDto>> GetRewardsAsync(Guid userId, int limit, CancellationToken cancellationToken);

    Task<IReadOnlyList<XpLeaderboardEntryDto>> GetXpLeaderboardAsync(CancellationToken cancellationToken);
}
