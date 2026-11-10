using QuizBattle.Application.Profile;
using QuizBattle.Tests.TestHelpers;

namespace QuizBattle.Tests.Profile;

public sealed class XpLeaderboardServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetTop_ExcludesZeroXp()
    {
        var repository = new FakeUserRepository();
        repository.Users.Add(ProfileServiceTests.NewUser("Zero"));
        repository.Users.Add(ProfileServiceTests.NewUser("Has", totalXp: 10));

        var top = await new ProfileService(repository).GetXpLeaderboardAsync(CancellationToken.None);

        Assert.Equal("Has", Assert.Single(top).DisplayName);
    }

    [Fact]
    public async Task GetTop_NobodyHasXp_Empty()
    {
        var repository = new FakeUserRepository();
        repository.Users.Add(ProfileServiceTests.NewUser("Zero"));

        Assert.Empty(await new ProfileService(repository).GetXpLeaderboardAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetTop_OrderedByXpDescThenCreatedAt()
    {
        var repository = new FakeUserRepository();
        repository.Users.Add(ProfileServiceTests.NewUser("TieLate", totalXp: 500, createdAt: Now.AddDays(2)));
        repository.Users.Add(ProfileServiceTests.NewUser("Low", totalXp: 100));
        repository.Users.Add(ProfileServiceTests.NewUser("TieEarly", totalXp: 500, createdAt: Now.AddDays(1)));
        repository.Users.Add(ProfileServiceTests.NewUser("Top", totalXp: 900));

        var top = await new ProfileService(repository).GetXpLeaderboardAsync(CancellationToken.None);

        Assert.Equal(["Top", "TieEarly", "TieLate", "Low"], top.Select(e => e.DisplayName));
        Assert.Equal([1, 2, 3, 4], top.Select(e => e.Rank));
    }

    [Fact]
    public async Task GetTop_Max50()
    {
        var repository = new FakeUserRepository();
        for (var i = 1; i <= 60; i++)
        {
            repository.Users.Add(ProfileServiceTests.NewUser($"U{i}", totalXp: i));
        }

        var top = await new ProfileService(repository).GetXpLeaderboardAsync(CancellationToken.None);

        Assert.Equal(50, top.Count);
        Assert.Equal("U60", top[0].DisplayName);
        Assert.Equal("U11", top[^1].DisplayName);
    }

    [Fact]
    public async Task GetTop_ComputesLevel()
    {
        var repository = new FakeUserRepository();
        repository.Users.Add(ProfileServiceTests.NewUser("A", totalXp: 199)); // cấp 1
        repository.Users.Add(ProfileServiceTests.NewUser("B", totalXp: 200)); // cấp 2

        var top = await new ProfileService(repository).GetXpLeaderboardAsync(CancellationToken.None);

        Assert.Equal(2, top.Single(e => e.DisplayName == "B").Level);
        Assert.Equal(1, top.Single(e => e.DisplayName == "A").Level);
    }
}
