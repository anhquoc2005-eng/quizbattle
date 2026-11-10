using QuizBattle.Application.Common;
using QuizBattle.Application.Profile;
using QuizBattle.Domain.Entities;
using QuizBattle.Tests.TestHelpers;

namespace QuizBattle.Tests.Profile;

public sealed class ProfileServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetProfile_ComputesLevelAndProgressFromTotalXp()
    {
        var repository = new FakeUserRepository();
        var user = NewUser("Alice", totalXp: 640, coins: 125);
        repository.Users.Add(user);

        var profile = await new ProfileService(repository).GetProfileAsync(user.Id, CancellationToken.None);

        // 640 XP: cấp 3 bắt đầu ở 600, cần 600 XP để lên cấp 4.
        Assert.Equal(new ProfileDto(user.Id, "Alice", 3, 640, 40, 600, 125), profile);
    }

    [Fact]
    public async Task GetProfile_NewUser_Level1ZeroXpZeroCoins()
    {
        var repository = new FakeUserRepository();
        var user = NewUser("Newbie");
        repository.Users.Add(user);

        var profile = await new ProfileService(repository).GetProfileAsync(user.Id, CancellationToken.None);

        Assert.Equal(new ProfileDto(user.Id, "Newbie", 1, 0, 0, 200, 0), profile);
    }

    [Fact]
    public async Task GetProfile_UserNotFound_NotFound()
    {
        var service = new ProfileService(new FakeUserRepository());

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetProfileAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task GetRewards_OnlyAwardedNewestFirst()
    {
        // Lọc Awarded và sắp xếp nằm ở truy vấn (kiểm bằng test tích hợp); service chỉ chuyển nguyên thứ tự repo trả về.
        var repository = new FakeUserRepository();
        repository.Rewards.Add(new RewardHistoryItemDto("NEW111", "Mới", Now.AddDays(1), 900, 1, 90, 9, 50));
        repository.Rewards.Add(new RewardHistoryItemDto("OLD222", "Cũ", Now, 500, 2, 50, 5, 20));

        var rewards = await new ProfileService(repository).GetRewardsAsync(Guid.NewGuid(), 20, CancellationToken.None);

        Assert.Equal(["NEW111", "OLD222"], rewards.Select(r => r.RoomCode));
        Assert.Equal(20, repository.LastLimit);
    }

    [Fact]
    public async Task GetRewards_NoMatches_EmptyList()
    {
        var rewards = await new ProfileService(new FakeUserRepository()).GetRewardsAsync(Guid.NewGuid(), 20, CancellationToken.None);

        Assert.Empty(rewards);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(51)]
    public async Task GetRewards_LimitOutOfRange_Validation(int limit)
    {
        var repository = new FakeUserRepository();
        var service = new ProfileService(repository);

        var ex = await Assert.ThrowsAsync<RequestValidationException>(() => service.GetRewardsAsync(Guid.NewGuid(), limit, CancellationToken.None));

        Assert.Equal("limit", ex.Field);
        Assert.Null(repository.LastLimit); // không chạm DB khi input sai
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    public async Task GetRewards_LimitAtBounds_Allowed(int limit)
    {
        var repository = new FakeUserRepository();

        await new ProfileService(repository).GetRewardsAsync(Guid.NewGuid(), limit, CancellationToken.None);

        Assert.Equal(limit, repository.LastLimit);
    }

    internal static User NewUser(string name, int totalXp = 0, int coins = 0, DateTimeOffset? createdAt = null) => new()
    {
        Id = Guid.NewGuid(), Email = $"{name}@test.local", PasswordHash = "h", DisplayName = name,
        CreatedAt = createdAt ?? Now, TotalXp = totalXp, CoinBalance = coins,
    };
}
