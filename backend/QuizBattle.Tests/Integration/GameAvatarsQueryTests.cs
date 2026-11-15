using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using QuizBattle.Application.Game;
using QuizBattle.Application.Profile;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;
using QuizBattle.Infrastructure.Persistence;
using QuizBattle.Tests.TestHelpers;

namespace QuizBattle.Tests.Integration;

/// <summary>
/// Nhân vật của cả phòng được nạp bằng ĐÚNG MỘT truy vấn thêm, không phụ thuộc số người chơi (không N+1), không khóa
/// FOR UPDATE, và không làm đổi thứ tự khóa Room → Users. Đếm lệnh SQL thật bằng một DbCommandInterceptor.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class GameAvatarsQueryTests(DatabaseFixture fixture)
{
    // Số lệnh SQL của FindSnapshotByCodeAsync TRƯỚC khi có nhân vật (đo trên mã chưa có truy vấn UserAvatars, 2026-11-13).
    private const int CommandsBeforeAvatarFeature = 5;

    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [RequiresDatabaseFact]
    public async Task Snapshot_SqlCommandCount_IsSameForTwoAndFiftyPlayers()
    {
        var small = await SeedRoomAsync(playerCount: 2);
        var large = await SeedRoomAsync(playerCount: 50);

        var (_, smallCommands) = await CountSnapshotCommandsAsync(small);
        var (_, largeCommands) = await CountSnapshotCommandsAsync(large);

        Assert.Equal(smallCommands.Count, largeCommands.Count);
    }

    [RequiresDatabaseFact]
    public async Task Snapshot_AvatarsLoadedWithExactlyOneExtraQuery()
    {
        var room = await SeedRoomAsync(playerCount: 50);

        var (_, commands) = await CountSnapshotCommandsAsync(room);

        Assert.Equal(CommandsBeforeAvatarFeature + 1, commands.Count);
        Assert.Equal(1, commands.Count(c => c.Contains("\"UserAvatars\"")));
    }

    [RequiresDatabaseFact]
    public async Task Snapshot_ReturnsSavedAvatarsOfAccountPlayersOnly()
    {
        var room = await SeedRoomAsync(playerCount: 6, savedAvatarForAccounts: true);

        var (snapshot, _) = await CountSnapshotCommandsAsync(room);

        var accountIds = snapshot.Players.Where(p => p.UserId is not null).Select(p => p.UserId!.Value).ToHashSet();
        Assert.NotEmpty(accountIds);
        Assert.Equal(accountIds, snapshot.Avatars.Keys.ToHashSet());
    }

    [RequiresDatabaseFact]
    public async Task RoomState_ThroughRealRepository_ShowsSavedAvatarForAccountAndDefaultForGuest()
    {
        var room = await SeedRoomAsync(playerCount: 4, savedAvatarForAccounts: true);
        await using var db = fixture.CreateContext();
        var service = new GameService(new GameRepository(db), new UnusedTokenGenerator(), new FakeTimeProvider(Now));

        var state = await service.JoinAsHostAsync(room.HostUserId, room.Code, CancellationToken.None);

        var account = state.Players.Single(p => p.DisplayName == "Người 1"); // chơi có tài khoản: nhân vật đã lưu
        var guest = state.Players.Single(p => p.DisplayName == "Người 2");
        Assert.Equal("variant62", account.Avatar.Hair);
        Assert.Equal("ok", account.Avatar.Gesture);
        Assert.Null(guest.Avatar.Gesture); // khách: mặc định
        Assert.Equal("d4d4d4", guest.Avatar.Background);
    }

    [RequiresDatabaseFact]
    public async Task Snapshot_AvatarQuery_NotLockedForUpdate_AndLockOrderIsRoomThenUsers()
    {
        var room = await SeedRoomAsync(playerCount: 6, finalAdvance: true);
        var commands = new List<string>();
        await using var db = CreateCountingContext(commands);
        var repository = new GameRepository(db);

        await repository.WithLockedRoomAsync(room.Code, snapshot => snapshot.Players.Count, CancellationToken.None);

        var avatarQuery = Assert.Single(commands, c => c.Contains("\"UserAvatars\""));
        Assert.DoesNotContain("FOR UPDATE", avatarQuery, StringComparison.OrdinalIgnoreCase);
        var roomLock = commands.FindIndex(c => c.Contains("\"Rooms\"") && c.Contains("FOR UPDATE"));
        var usersLock = commands.FindIndex(c => c.Contains("\"Users\"") && c.Contains("FOR UPDATE"));
        Assert.True(roomLock >= 0 && usersLock > roomLock, "phải khóa Room trước rồi mới khóa Users");
        Assert.Contains("ORDER BY \"Id\"", commands[usersLock]); // Users khóa theo thứ tự Id như cũ
    }

    [RequiresDatabaseFact]
    public async Task XpLeaderboard_FiftyRows_SingleQuery_WithAvatars()
    {
        for (var i = 0; i < 50; i++)
        {
            await SeedUserAsync($"xp{i}-{Guid.NewGuid():N}", totalXp: 100_000 + i, savedAvatar: i % 2 == 0);
        }

        var commands = new List<string>();
        await using var db = CreateCountingContext(commands);
        var service = new ProfileService(new UserRepository(db));

        var top = await service.GetXpLeaderboardAsync(CancellationToken.None);

        Assert.Equal(50, top.Count);
        Assert.Single(commands);
        Assert.Contains("\"UserAvatars\"", commands[0]);
        Assert.Contains(top, e => e.Avatar.Hair == "variant62"); // có người đã lưu nhân vật
        Assert.Contains(top, e => e.Avatar.Hair != "variant62"); // có người nhận mặc định
    }

    // ---- hạ tầng đếm lệnh ----

    private async Task<(GameSnapshot Snapshot, List<string> Commands)> CountSnapshotCommandsAsync(SeededRoom room)
    {
        var commands = new List<string>();
        await using var db = CreateCountingContext(commands);
        var snapshot = await new GameRepository(db).FindSnapshotByCodeAsync(room.Code, CancellationToken.None);
        return (snapshot!, commands);
    }

    private AppDbContext CreateCountingContext(List<string> commands)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(new CommandRecorder(commands))
            .Options;
        return new AppDbContext(options);
    }

    private sealed class CommandRecorder(List<string> commands) : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            commands.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            commands.Add(command.CommandText);
            return base.ReaderExecuting(command, eventData, result);
        }
    }

    // ---- dữ liệu ----

    private sealed record SeededRoom(string Code, Guid HostUserId);

    private async Task<User> SeedUserAsync(string name, int totalXp = 0, bool savedAvatar = false)
    {
        var id = Guid.NewGuid();
        var user = new User { Id = id, Email = $"{name}-{id:N}@test.local", PasswordHash = "h", DisplayName = name, CreatedAt = Now, TotalXp = totalXp };
        await using var db = fixture.CreateContext();
        db.Users.Add(user);
        if (savedAvatar)
        {
            db.UserAvatars.Add(new UserAvatar
            {
                UserId = id, EyesItemKey = "eyes.variant02", EyebrowsItemKey = "eyebrows.variant03", MouthItemKey = "mouth.variant04",
                NoseItemKey = "nose.variant05", HairItemKey = "hair.variant62", ClothesItemKey = "clothes.variant07",
                GestureItemKey = null, UpdatedAt = Now,
            });
            db.UserAvatarItems.Add(new UserAvatarItem { Id = Guid.NewGuid(), UserId = id, ItemKey = "gesture.ok", AcquiredAt = Now });
            await db.SaveChangesAsync();
            db.UserAvatars.Single(a => a.UserId == id).GestureItemKey = "gesture.ok";
        }

        await db.SaveChangesAsync();
        return user;
    }

    // Nửa số người chơi là tài khoản (lẻ: "Người 1", "Người 3"...), nửa là khách. Người chơi 1 luôn là tài khoản.
    private async Task<SeededRoom> SeedRoomAsync(int playerCount, bool savedAvatarForAccounts = false, bool finalAdvance = false)
    {
        var host = await SeedUserAsync("host");
        var room = new Room
        {
            Id = Guid.NewGuid(), Code = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(), HostUserId = host.Id, Topic = "Chủ đề",
            Status = finalAdvance ? RoomStatus.InProgress : RoomStatus.Lobby, CreatedAt = Now, StartedAt = finalAdvance ? Now : null,
        };
        var players = new List<Player>();
        for (var i = 1; i <= playerCount; i++)
        {
            var isAccount = i % 2 == 1;
            var userId = isAccount ? (await SeedUserAsync($"acc{i}", savedAvatar: savedAvatarForAccounts)).Id : (Guid?)null;
            players.Add(new Player
            {
                Id = Guid.NewGuid(), RoomId = room.Id, UserId = userId, DisplayName = $"Người {i}",
                SessionTokenHash = Guid.NewGuid().ToString("N"), JoinedAt = Now,
            });
        }

        await using var db = fixture.CreateContext();
        db.Rooms.Add(room);
        if (finalAdvance)
        {
            db.Questions.Add(new Question
            {
                Id = Guid.NewGuid(), RoomId = room.Id, OrderIndex = 0, Text = "Câu 1", Options = ["A", "B", "C", "D"], CorrectIndex = 0,
                ReviewStatus = QuestionReviewStatus.Approved, StartedAt = Now, EndedAt = Now.AddSeconds(10),
            });
        }

        db.Players.AddRange(players);
        await db.SaveChangesAsync();
        return new SeededRoom(room.Code, host.Id);
    }

    private sealed class UnusedTokenGenerator : ISessionTokenGenerator
    {
        public string Generate() => throw new NotSupportedException();
    }
}
