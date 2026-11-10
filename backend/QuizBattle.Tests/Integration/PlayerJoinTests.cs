using Microsoft.EntityFrameworkCore;
using QuizBattle.Application.Game;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;
using QuizBattle.Infrastructure.Persistence;
using QuizBattle.Tests.TestHelpers;

namespace QuizBattle.Tests.Integration;

/// <summary>Vào phòng có tài khoản trên Postgres thật: khóa Room, index (RoomId, UserId) và index tên.</summary>
[Collection(DatabaseCollection.Name)]
public sealed class PlayerJoinTests(DatabaseFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [RequiresDatabaseFact]
    public async Task Players_RejoinWithOtherName_DoesNotTouchNameIndex()
    {
        var (room, alice) = await SeedLobbyAsync();
        await JoinAsync(room.Code, alice.Id, "Alice");
        await using (var seed = fixture.CreateContext())
        {
            seed.Players.Add(new Player { Id = Guid.NewGuid(), RoomId = room.Id, DisplayName = "Bob", SessionTokenHash = Guid.NewGuid().ToString("N"), JoinedAt = Now });
            await seed.SaveChangesAsync();
        }

        // Vào lại với tên đã bị người khác dùng: tên bị bỏ qua nên không INSERT, không vi phạm IX_Players_RoomId_lower_DisplayName.
        var again = await JoinAsync(room.Code, alice.Id, "bob");

        await using var db = fixture.CreateContext();
        var players = await db.Players.AsNoTracking().Where(p => p.RoomId == room.Id).ToListAsync();
        Assert.Equal(2, players.Count);
        var mine = players.Single(p => p.UserId == alice.Id);
        Assert.Equal(again.PlayerId, mine.Id);
        Assert.Equal("Alice", mine.DisplayName);
    }

    [RequiresDatabaseFact]
    public async Task JoinPlayer_TwoConcurrentFirstJoinsSameAccount_OnePlayer()
    {
        var (room, alice) = await SeedLobbyAsync();

        var results = await Task.WhenAll(
            JoinAsync(room.Code, alice.Id, "Alice"),
            JoinAsync(room.Code, alice.Id, "Alice"));

        await using var db = fixture.CreateContext();
        var player = await db.Players.AsNoTracking().SingleAsync(p => p.RoomId == room.Id && p.UserId == alice.Id);
        Assert.Equal(results[0].PlayerId, results[1].PlayerId);
        Assert.Equal(player.Id, results[0].PlayerId);
        // Khóa Room tuần tự hóa hai lần vào: lần sau xoay token, chỉ token của lần sau còn dùng được.
        var winner = results.Single(r => SessionTokenHasher.Hash(r.SessionToken) == player.SessionTokenHash);
        Assert.NotNull(winner);
    }

    private async Task<CreatePlayerResponse> JoinAsync(string code, Guid userId, string displayName)
    {
        await using var db = fixture.CreateContext();
        var service = new GameService(new GameRepository(db), new RandomTokenGenerator(), new FakeTimeProvider(Now));
        return await service.CreatePlayerAsync(code, userId, new CreatePlayerRequest { DisplayName = displayName }, CancellationToken.None);
    }

    private async Task<(Room Room, User User)> SeedLobbyAsync()
    {
        var host = NewUser("host");
        var user = NewUser("alice");
        var room = new Room
        {
            Id = Guid.NewGuid(), Code = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(), HostUserId = host.Id,
            Topic = "Chủ đề", Status = RoomStatus.Lobby, CreatedAt = Now,
        };
        await using var db = fixture.CreateContext();
        db.Users.AddRange(host, user);
        db.Rooms.Add(room);
        await db.SaveChangesAsync();
        return (room, user);
    }

    private static User NewUser(string name)
    {
        var id = Guid.NewGuid();
        return new User { Id = id, Email = $"{name}-{id:N}@test.local", PasswordHash = "h", DisplayName = name, CreatedAt = Now };
    }

    private sealed class RandomTokenGenerator : ISessionTokenGenerator
    {
        public string Generate() => Guid.NewGuid().ToString("N");
    }
}
