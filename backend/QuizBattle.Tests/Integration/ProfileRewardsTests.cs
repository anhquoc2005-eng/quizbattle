using Microsoft.EntityFrameworkCore;
using QuizBattle.Application.Profile;
using QuizBattle.Application.Rooms;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;
using QuizBattle.Infrastructure.Persistence;

namespace QuizBattle.Tests.Integration;

/// <summary>Hồ sơ và lịch sử thưởng trên Postgres thật, kể cả khi phòng đã bị chủ phòng xóa.</summary>
[Collection(DatabaseCollection.Name)]
public sealed class ProfileRewardsTests(DatabaseFixture fixture)
{
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [RequiresDatabaseFact]
    public async Task DeleteFinishedRoom_RewardsKeptAndProfileAndInvariantsUnchanged()
    {
        var (host, player, room) = await SeedFinishedRoomWithRewardAsync();
        var before = await GetSnapshotAsync(player.Id);
        Assert.Equal(80, before.Profile.TotalXp);
        Assert.Equal(room.Code, Assert.Single(before.Rewards).RoomCode);

        await using (var db = fixture.CreateContext())
        {
            var roomService = new RoomService(new RoomRepository(db), new UnusedRoomCodeGenerator(), TimeProvider.System);
            await roomService.DeleteRoomAsync(host.Id, room.Code, CancellationToken.None);
        }

        await using var check = fixture.CreateContext();
        Assert.False(await check.Rooms.AnyAsync(r => r.Id == room.Id));
        var reward = await check.MatchRewards.AsNoTracking().SingleAsync(r => r.UserId == player.Id);
        Assert.Null(reward.RoomId);
        Assert.Null(reward.PlayerId);

        var after = await GetSnapshotAsync(player.Id);
        Assert.Equal(before.Profile, after.Profile);
        var item = Assert.Single(after.Rewards);
        Assert.Equal(before.Rewards[0], item); // roomCode, topic, playedAt vẫn đủ
        Assert.Equal("ABC123", item.RoomCode);
        Assert.Equal("Lịch sử Việt Nam", item.Topic);
        Assert.Equal(Start, item.PlayedAt);

        var user = await check.Users.AsNoTracking().SingleAsync(u => u.Id == player.Id);
        Assert.Equal(await check.MatchRewards.Where(r => r.UserId == player.Id).SumAsync(r => r.Xp), user.TotalXp);
        Assert.Equal(await check.CoinTransactions.Where(c => c.UserId == player.Id).SumAsync(c => c.Amount), user.CoinBalance);
    }

    [RequiresDatabaseFact]
    public async Task Rewards_OnlyAwardedNewestFirstAndLimited()
    {
        var user = await SeedUserAsync("History", totalXp: 30);
        await using (var db = fixture.CreateContext())
        {
            db.MatchRewards.AddRange(
                NewReward(user.Id, "OLD111", Start, RewardStatus.Awarded, xp: 10),
                NewReward(user.Id, "NEW222", Start.AddDays(2), RewardStatus.Awarded, xp: 20),
                NewReward(user.Id, "DENIED", Start.AddDays(3), RewardStatus.NotAwarded, xp: 0));
            await db.SaveChangesAsync();
        }

        await using var read = fixture.CreateContext();
        var service = new ProfileService(new UserRepository(read));

        var all = await service.GetRewardsAsync(user.Id, 20, CancellationToken.None);
        var limited = await service.GetRewardsAsync(user.Id, 1, CancellationToken.None);

        Assert.Equal(["NEW222", "OLD111"], all.Select(r => r.RoomCode));
        Assert.Equal("NEW222", Assert.Single(limited).RoomCode);
    }

    [RequiresDatabaseFact]
    public async Task Profile_AccountWithNoMatches_Level1AndEmptyHistory()
    {
        var user = await SeedUserAsync("Fresh", totalXp: 0);
        await using var read = fixture.CreateContext();
        var service = new ProfileService(new UserRepository(read));

        var profile = await service.GetProfileAsync(user.Id, CancellationToken.None);

        Assert.Equal(new ProfileDto(user.Id, "Fresh", 1, 0, 0, 200, 0), profile);
        Assert.Empty(await service.GetRewardsAsync(user.Id, 20, CancellationToken.None));
    }

    private async Task<(ProfileDto Profile, IReadOnlyList<RewardHistoryItemDto> Rewards)> GetSnapshotAsync(Guid userId)
    {
        await using var db = fixture.CreateContext();
        var service = new ProfileService(new UserRepository(db));
        return (await service.GetProfileAsync(userId, CancellationToken.None), await service.GetRewardsAsync(userId, 20, CancellationToken.None));
    }

    private async Task<User> SeedUserAsync(string name, int totalXp = 0, int coins = 0)
    {
        var id = Guid.NewGuid();
        var user = new User
        {
            Id = id, Email = $"{name}-{id:N}@test.local".ToLowerInvariant(), PasswordHash = "h", DisplayName = name, CreatedAt = Start,
            TotalXp = totalXp, CoinBalance = coins,
        };
        await using var db = fixture.CreateContext();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static MatchReward NewReward(Guid userId, string code, DateTimeOffset finishedAt, RewardStatus status, int xp) => new()
    {
        Id = Guid.NewGuid(), UserId = userId, RoomCode = code, RoomTopic = "Chủ đề", FinishedAt = finishedAt,
        Status = status, DenialReason = status == RewardStatus.Awarded ? null : RewardDenialReason.NoAnswers,
        Score = xp * 10, RewardRank = 1, Xp = xp,
    };

    // Phòng Finished với một người chơi đã nhận 80 XP và 8 Xu (đúng bất biến TotalXp = SUM(Xp), CoinBalance = SUM(Amount)).
    private async Task<(User Host, User Player, Room Room)> SeedFinishedRoomWithRewardAsync()
    {
        var host = await SeedUserAsync("host");
        var player = await SeedUserAsync("player", totalXp: 80, coins: 8);
        var room = new Room
        {
            Id = Guid.NewGuid(), Code = "ABC123", HostUserId = host.Id, Topic = "Lịch sử Việt Nam",
            Status = RoomStatus.Finished, CreatedAt = Start, StartedAt = Start, EndedAt = Start,
        };
        var seat = new Player
        {
            Id = Guid.NewGuid(), RoomId = room.Id, UserId = player.Id, DisplayName = "player",
            SessionTokenHash = Guid.NewGuid().ToString("N"), Score = 800, JoinedAt = Start,
        };
        var reward = NewReward(player.Id, room.Code, Start, RewardStatus.Awarded, xp: 80);
        reward.RoomId = room.Id;
        reward.PlayerId = seat.Id;
        reward.RoomTopic = room.Topic;
        reward.Score = 800;
        reward.Coins = 8;

        await using var db = fixture.CreateContext();
        db.Rooms.Add(room);
        db.Players.Add(seat);
        db.MatchRewards.Add(reward);
        db.CoinTransactions.Add(new CoinTransaction
        {
            Id = Guid.NewGuid(), UserId = player.Id, Amount = 8, Reason = CoinTransactionReason.MatchReward,
            RoomId = room.Id, MatchRewardId = reward.Id, CreatedAt = Start,
        });
        await db.SaveChangesAsync();
        return (host, player, room);
    }

    private sealed class UnusedRoomCodeGenerator : IRoomCodeGenerator
    {
        public string Generate() => throw new NotSupportedException();
    }
}
