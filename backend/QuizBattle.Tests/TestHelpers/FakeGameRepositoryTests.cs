using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Tests.TestHelpers;

public sealed class FakeGameRepositoryTests
{
    [Fact]
    public async Task WithLockedRoom_WorkThrows_RollsBackEntitiesAndDiscardsNewRewards()
    {
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var userId = Guid.NewGuid();
        var room = new Room { Id = Guid.NewGuid(), Code = "ABCDEF", HostUserId = Guid.NewGuid(), Topic = "T", Status = RoomStatus.InProgress, CreatedAt = now };
        var user = new User { Id = userId, Email = "a@t.local", PasswordHash = "h", DisplayName = "A", CreatedAt = now };
        var player = new Player { Id = Guid.NewGuid(), RoomId = room.Id, UserId = userId, DisplayName = "A", SessionTokenHash = "h", JoinedAt = now };
        var repository = new FakeGameRepository { Room = room };
        repository.Users.Add(user);
        repository.Players.Add(player);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.WithLockedRoomAsync("ABCDEF", snapshot =>
        {
            snapshot.Room.Status = RoomStatus.Finished;
            snapshot.Room.EndedAt = now;
            snapshot.Players[0].Score = 900;
            snapshot.Users[userId].TotalXp = 90;
            snapshot.Users[userId].CoinBalance = 9;
            snapshot.NewMatchRewards.Add(new MatchReward { Id = Guid.NewGuid(), UserId = userId, RoomCode = "ABCDEF", RoomTopic = "T" });
            throw new InvalidOperationException("lỗi giữa chừng");
#pragma warning disable CS0162
            return 0;
#pragma warning restore CS0162
        }, CancellationToken.None));

        Assert.Equal(RoomStatus.InProgress, room.Status);
        Assert.Null(room.EndedAt);
        Assert.Equal(0, player.Score);
        Assert.Equal(0, user.TotalXp);
        Assert.Equal(0, user.CoinBalance);
        Assert.Empty(repository.MatchRewards);
        Assert.Empty(repository.CoinTransactions);
    }

    [Fact]
    public async Task WithLockedRoom_FailBeforeCommit_RollsBackLikeFailedSaveChanges()
    {
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var room = new Room { Id = Guid.NewGuid(), Code = "ABCDEF", HostUserId = Guid.NewGuid(), Topic = "T", Status = RoomStatus.InProgress, CreatedAt = now };
        var repository = new FakeGameRepository { Room = room, FailBeforeCommit = () => throw new InvalidOperationException("SaveChanges lỗi") };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.WithLockedRoomAsync("ABCDEF", snapshot => snapshot.Room.Status = RoomStatus.Finished, CancellationToken.None));

        Assert.Equal(RoomStatus.InProgress, room.Status);
    }
}
