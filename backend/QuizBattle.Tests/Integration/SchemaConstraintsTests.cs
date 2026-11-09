using Microsoft.EntityFrameworkCore;
using Npgsql;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;
using QuizBattle.Infrastructure.Persistence;

namespace QuizBattle.Tests.Integration;

/// <summary>Kiểm tra ràng buộc schema (CHECK, unique index lọc) trên Postgres thật bằng migration thật.</summary>
[Collection(DatabaseCollection.Name)]
public sealed class SchemaConstraintsTests(DatabaseFixture fixture)
{
    private const string CheckViolation = "23514";
    private const string UniqueViolation = "23505";

    [RequiresDatabaseFact]
    public async Task Users_NegativeCoinBalance_ViolatesCheck()
    {
        await using var db = fixture.CreateContext();
        db.Users.Add(NewUser(coinBalance: -1));

        await AssertViolationAsync(db, CheckViolation, "CK_Users_CoinBalance_NonNegative");
    }

    [RequiresDatabaseFact]
    public async Task Users_NegativeTotalXp_ViolatesCheck()
    {
        await using var db = fixture.CreateContext();
        db.Users.Add(NewUser(totalXp: -1));

        await AssertViolationAsync(db, CheckViolation, "CK_Users_TotalXp_NonNegative");
    }

    [RequiresDatabaseFact]
    public async Task MatchRewards_DuplicatePlayerId_ViolatesUniqueIndex()
    {
        var (user, room, player) = await SeedPlayerAsync(withUser: true);
        await using var db = fixture.CreateContext();
        db.MatchRewards.Add(NewReward(user.Id, room.Id, player.Id));
        await db.SaveChangesAsync();

        await using var second = fixture.CreateContext();
        second.MatchRewards.Add(NewReward(user.Id, room.Id, player.Id));

        await AssertViolationAsync(second, UniqueViolation, "IX_MatchRewards_PlayerId");
    }

    [RequiresDatabaseFact]
    public async Task MatchRewards_MultipleNullPlayerId_AllowedByUniqueIndex()
    {
        var user = NewUser();
        await using var db = fixture.CreateContext();
        db.Users.Add(user);
        db.MatchRewards.Add(NewReward(user.Id, roomId: null, playerId: null));
        db.MatchRewards.Add(NewReward(user.Id, roomId: null, playerId: null));

        await db.SaveChangesAsync();
    }

    [RequiresDatabaseFact]
    public async Task Players_SameUserSameRoom_ViolatesPartialUnique()
    {
        var (user, room, _) = await SeedPlayerAsync(withUser: true);
        await using var db = fixture.CreateContext();
        db.Players.Add(NewPlayer(room.Id, user.Id));

        await AssertViolationAsync(db, UniqueViolation, "IX_Players_RoomId_UserId");
    }

    [RequiresDatabaseFact]
    public async Task Players_ManyGuestsSameRoom_Allowed()
    {
        var (_, room, _) = await SeedPlayerAsync(withUser: false);
        await using var db = fixture.CreateContext();
        db.Players.Add(NewPlayer(room.Id, userId: null));
        db.Players.Add(NewPlayer(room.Id, userId: null));

        await db.SaveChangesAsync();
    }

    [RequiresDatabaseFact]
    public async Task CoinTransactions_ZeroAmount_ViolatesCheck()
    {
        var user = NewUser();
        await using var db = fixture.CreateContext();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        db.CoinTransactions.Add(new CoinTransaction
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Amount = 0,
            Reason = CoinTransactionReason.MatchReward,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await AssertViolationAsync(db, CheckViolation, null);
    }

    private static async Task AssertViolationAsync(AppDbContext db, string sqlState, string? constraintName)
    {
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var pg = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal(sqlState, pg.SqlState);
        if (constraintName is not null)
        {
            Assert.Equal(constraintName, pg.ConstraintName);
        }
    }

    private async Task<(User User, Room Room, Player Player)> SeedPlayerAsync(bool withUser)
    {
        var user = NewUser();
        var room = new Room
        {
            Id = Guid.NewGuid(),
            Code = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(),
            HostUserId = user.Id,
            Topic = "Chủ đề",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var player = NewPlayer(room.Id, withUser ? user.Id : null);

        await using var db = fixture.CreateContext();
        db.Users.Add(user);
        db.Rooms.Add(room);
        db.Players.Add(player);
        await db.SaveChangesAsync();
        return (user, room, player);
    }

    private static User NewUser(int totalXp = 0, int coinBalance = 0)
    {
        var id = Guid.NewGuid();
        return new User
        {
            Id = id,
            Email = $"{id:N}@test.local",
            PasswordHash = "hash",
            DisplayName = "Tester",
            CreatedAt = DateTimeOffset.UtcNow,
            TotalXp = totalXp,
            CoinBalance = coinBalance,
        };
    }

    private static Player NewPlayer(Guid roomId, Guid? userId) => new()
    {
        Id = Guid.NewGuid(),
        RoomId = roomId,
        UserId = userId,
        DisplayName = $"P-{Guid.NewGuid():N}"[..20],
        SessionTokenHash = Guid.NewGuid().ToString("N"),
        JoinedAt = DateTimeOffset.UtcNow,
    };

    private static MatchReward NewReward(Guid userId, Guid? roomId, Guid? playerId) => new()
    {
        Id = Guid.NewGuid(),
        PlayerId = playerId,
        UserId = userId,
        RoomId = roomId,
        RoomCode = "ABC123",
        RoomTopic = "Chủ đề",
        FinishedAt = DateTimeOffset.UtcNow,
        Status = RewardStatus.Awarded,
        DenialReason = null,
        Score = 500,
        RewardRank = 1,
        Xp = 50,
        Coins = 5,
        RankBonus = 30,
    };
}
