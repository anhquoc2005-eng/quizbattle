using QuizBattle.Application.Auth;
using QuizBattle.Application.Common;
using QuizBattle.Domain;

namespace QuizBattle.Application.Profile;

public sealed class ProfileService(IUserRepository userRepository) : IProfileService
{
    public const int MaxRewardsLimit = 50;
    public const int XpLeaderboardSize = 50;

    public async Task<ProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy tài khoản.");
        var progress = LevelCalculator.Progress(user.TotalXp);
        return new ProfileDto(user.Id, user.DisplayName, progress.Level, user.TotalXp, progress.XpIntoLevel, progress.XpForNext, user.CoinBalance);
    }

    public Task<IReadOnlyList<RewardHistoryItemDto>> GetRewardsAsync(Guid userId, int limit, CancellationToken cancellationToken)
    {
        if (limit is < 1 or > MaxRewardsLimit)
        {
            throw new RequestValidationException("limit", $"limit phải từ 1 đến {MaxRewardsLimit}.");
        }

        return userRepository.GetRewardHistoryAsync(userId, limit, cancellationToken);
    }

    public async Task<IReadOnlyList<XpLeaderboardEntryDto>> GetXpLeaderboardAsync(CancellationToken cancellationToken)
    {
        var rows = await userRepository.GetTopByXpAsync(XpLeaderboardSize, cancellationToken);
        return rows.Select((r, i) => new XpLeaderboardEntryDto(i + 1, r.DisplayName, LevelCalculator.FromXp(r.TotalXp), r.TotalXp)).ToList();
    }
}
