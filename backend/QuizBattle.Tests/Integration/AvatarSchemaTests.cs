using Microsoft.EntityFrameworkCore;
using Npgsql;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;
using QuizBattle.Infrastructure.Persistence;

namespace QuizBattle.Tests.Integration;

/// <summary>Schema của nhân vật và cửa hàng trên Postgres thật bằng migration thật: seed, ràng buộc, RLS.</summary>
[Collection(DatabaseCollection.Name)]
public sealed class AvatarSchemaTests(DatabaseFixture fixture)
{
    private const string CheckViolation = "23514";
    private const string UniqueViolation = "23505";
    private const string ForeignKeyViolation = "23503";
    private const string NotNullViolation = "23502";

    [RequiresDatabaseFact]
    public async Task Migration_Seeds201Items_85Free()
    {
        await using var db = fixture.CreateContext();

        Assert.Equal(201, await db.AvatarItems.CountAsync());
        Assert.Equal(85, await db.AvatarItems.CountAsync(i => i.Tier == AvatarTier.Free));
        Assert.Equal(14850, await db.AvatarItems.SumAsync(i => i.Price));
    }

    [RequiresDatabaseFact]
    public async Task UserAvatarItems_DuplicateUserAndItem_ViolatesUnique()
    {
        var user = await SeedUserAsync();
        await using var db = fixture.CreateContext();
        db.UserAvatarItems.Add(NewOwnership(user.Id, "gesture.ok"));
        await db.SaveChangesAsync();

        await using var second = fixture.CreateContext();
        second.UserAvatarItems.Add(NewOwnership(user.Id, "gesture.ok"));

        await AssertViolationAsync(second, UniqueViolation, "IX_UserAvatarItems_UserId_ItemKey");
    }

    [RequiresDatabaseFact]
    public async Task UserAvatars_AllFreeItems_Inserts()
    {
        var user = await SeedUserAsync();
        await using var db = fixture.CreateContext();
        db.UserAvatars.Add(NewAvatar(user.Id));

        await db.SaveChangesAsync();
    }

    [RequiresDatabaseFact]
    public async Task UserAvatars_ItemKeyOfWrongSlot_ViolatesCheck()
    {
        var user = await SeedUserAsync();
        await using var db = fixture.CreateContext();
        var avatar = NewAvatar(user.Id);
        avatar.HairItemKey = "eyes.variant01"; // món có thật nhưng thuộc ô khác
        db.UserAvatars.Add(avatar);

        await AssertViolationAsync(db, CheckViolation, "CK_UserAvatars_hair");
    }

    [RequiresDatabaseFact]
    public async Task UserAvatars_OptionalSlotOfWrongSlot_ViolatesCheck()
    {
        var user = await SeedUserAsync();
        await using var db = fixture.CreateContext();
        var avatar = NewAvatar(user.Id);
        avatar.GlassesItemKey = "gesture.ok";
        db.UserAvatars.Add(avatar);

        await AssertViolationAsync(db, CheckViolation, "CK_UserAvatars_glasses");
    }

    [RequiresDatabaseFact]
    public async Task UserAvatars_UnknownItemKey_ViolatesForeignKey()
    {
        var user = await SeedUserAsync();
        await using var db = fixture.CreateContext();
        var avatar = NewAvatar(user.Id);
        avatar.HairItemKey = "hair.nope"; // qua CHECK theo ô nhưng không có trong danh mục
        db.UserAvatars.Add(avatar);

        await AssertViolationAsync(db, ForeignKeyViolation, "FK_UserAvatars_AvatarItems_HairItemKey");
    }

    [RequiresDatabaseFact]
    public async Task UserAvatars_RequiredSlotNull_Rejected()
    {
        var user = await SeedUserAsync();
        await using var db = fixture.CreateContext();

        var ex = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"UserAvatars\" (\"UserId\", \"EyesItemKey\", \"EyebrowsItemKey\", \"MouthItemKey\", \"NoseItemKey\", \"HairItemKey\", \"ClothesItemKey\", \"UpdatedAt\") " +
            $"VALUES ('{user.Id}', 'eyes.variant01', 'eyebrows.variant01', 'mouth.variant01', 'nose.variant01', NULL, 'clothes.variant01', now())"));

        Assert.Equal(NotNullViolation, ex.SqlState);
    }

    [RequiresDatabaseFact]
    public async Task AvatarItems_FreeWithPositivePrice_ViolatesCheck()
    {
        await using var db = fixture.CreateContext();
        db.AvatarItems.Add(NewItem("hair.zz1", AvatarSlot.Hair, "zz1", AvatarTier.Free, 10));

        await AssertViolationAsync(db, CheckViolation, "CK_AvatarItems_Price");
    }

    [RequiresDatabaseFact]
    public async Task AvatarItems_PaidWithZeroPrice_ViolatesCheck()
    {
        await using var db = fixture.CreateContext();
        db.AvatarItems.Add(NewItem("hair.zz2", AvatarSlot.Hair, "zz2", AvatarTier.Common, 0));

        await AssertViolationAsync(db, CheckViolation, "CK_AvatarItems_Price");
    }

    [RequiresDatabaseFact]
    public async Task AvatarItems_KeyNotMatchingSlotAndVariant_ViolatesCheck()
    {
        await using var db = fixture.CreateContext();
        db.AvatarItems.Add(NewItem("hair.other", AvatarSlot.Hair, "zz3", AvatarTier.Common, 50));

        await AssertViolationAsync(db, CheckViolation, "CK_AvatarItems_Key");
    }

    [RequiresDatabaseFact]
    public async Task CoinTransactions_ShopPurchaseWithItemAndNegativeAmount_Inserts()
    {
        var (user, ownership) = await SeedOwnershipAsync("gesture.ok");
        await using var db = fixture.CreateContext();
        db.CoinTransactions.Add(NewTransaction(user.Id, -150, CoinTransactionReason.ShopPurchase, ownership.Id));

        await db.SaveChangesAsync();
    }

    [RequiresDatabaseFact]
    public async Task CoinTransactions_TwoPurchasesForSameOwnership_ViolatesUnique()
    {
        var (user, ownership) = await SeedOwnershipAsync("gesture.ok");
        await using var db = fixture.CreateContext();
        db.CoinTransactions.Add(NewTransaction(user.Id, -150, CoinTransactionReason.ShopPurchase, ownership.Id));
        await db.SaveChangesAsync();

        await using var second = fixture.CreateContext();
        second.CoinTransactions.Add(NewTransaction(user.Id, -150, CoinTransactionReason.ShopPurchase, ownership.Id));

        await AssertViolationAsync(second, UniqueViolation, "IX_CoinTransactions_UserAvatarItemId");
    }

    [RequiresDatabaseFact]
    public async Task CoinTransactions_ShopPurchaseWithoutItem_ViolatesReasonShape()
    {
        var user = await SeedUserAsync();
        await using var db = fixture.CreateContext();
        db.CoinTransactions.Add(NewTransaction(user.Id, -50, CoinTransactionReason.ShopPurchase, null));

        await AssertViolationAsync(db, CheckViolation, "CK_CoinTransactions_ReasonShape");
    }

    [RequiresDatabaseFact]
    public async Task CoinTransactions_ShopPurchasePositiveAmount_ViolatesReasonShape()
    {
        var (user, ownership) = await SeedOwnershipAsync("gesture.ok");
        await using var db = fixture.CreateContext();
        db.CoinTransactions.Add(NewTransaction(user.Id, 50, CoinTransactionReason.ShopPurchase, ownership.Id));

        await AssertViolationAsync(db, CheckViolation, "CK_CoinTransactions_ReasonShape");
    }

    [RequiresDatabaseFact]
    public async Task CoinTransactions_MatchRewardWithItem_ViolatesReasonShape()
    {
        var (user, ownership) = await SeedOwnershipAsync("gesture.ok");
        await using var db = fixture.CreateContext();
        db.CoinTransactions.Add(NewTransaction(user.Id, 50, CoinTransactionReason.MatchReward, ownership.Id));

        await AssertViolationAsync(db, CheckViolation, "CK_CoinTransactions_ReasonShape");
    }

    [RequiresDatabaseFact]
    public async Task CoinTransactions_MatchRewardNegativeAmount_ViolatesReasonShape()
    {
        var user = await SeedUserAsync();
        await using var db = fixture.CreateContext();
        db.CoinTransactions.Add(NewTransaction(user.Id, -5, CoinTransactionReason.MatchReward, null));

        await AssertViolationAsync(db, CheckViolation, "CK_CoinTransactions_ReasonShape");
    }

    [RequiresDatabaseFact]
    public async Task CoinTransactions_ExistingStyleMatchReward_StillValid()
    {
        // Dạng dòng mà RewardSettlement đã ghi từ Tính năng 1: thưởng dương, không gắn món đồ.
        var user = await SeedUserAsync();
        await using var db = fixture.CreateContext();
        db.CoinTransactions.Add(NewTransaction(user.Id, 30, CoinTransactionReason.MatchReward, null));

        await db.SaveChangesAsync();
    }

    [RequiresDatabaseFact]
    public async Task DeleteUser_WithPurchase_CascadesAvatarOwnershipAndLedgerWithoutForeignKeyError()
    {
        var (user, ownership) = await SeedOwnershipAsync("gesture.ok");
        await using (var db = fixture.CreateContext())
        {
            db.UserAvatars.Add(NewAvatar(user.Id));
            db.CoinTransactions.Add(NewTransaction(user.Id, -150, CoinTransactionReason.ShopPurchase, ownership.Id));
            await db.SaveChangesAsync();
        }

        await using var delete = fixture.CreateContext();
        await delete.Users.Where(u => u.Id == user.Id).ExecuteDeleteAsync();

        Assert.False(await delete.UserAvatarItems.AnyAsync(i => i.UserId == user.Id));
        Assert.False(await delete.UserAvatars.AnyAsync(a => a.UserId == user.Id));
        Assert.False(await delete.CoinTransactions.AnyAsync(c => c.UserId == user.Id));
    }

    [RequiresDatabaseFact]
    public async Task NewTables_HaveRowLevelSecurityEnabled()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT relname, relrowsecurity FROM pg_class WHERE relname IN ('AvatarItems', 'UserAvatars', 'UserAvatarItems') ORDER BY relname",
            connection);
        await using var reader = await command.ExecuteReaderAsync();

        var result = new Dictionary<string, bool>();
        while (await reader.ReadAsync())
        {
            result[reader.GetString(0)] = reader.GetBoolean(1);
        }

        Assert.Equal(3, result.Count);
        Assert.All(result, kv => Assert.True(kv.Value, $"{kv.Key} chưa bật RLS"));
    }

    private static async Task AssertViolationAsync(AppDbContext db, string sqlState, string constraintName)
    {
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var pg = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal(sqlState, pg.SqlState);
        Assert.Equal(constraintName, pg.ConstraintName);
    }

    private async Task<User> SeedUserAsync()
    {
        var id = Guid.NewGuid();
        var user = new User
        {
            Id = id,
            Email = $"{id:N}@test.local",
            PasswordHash = "hash",
            DisplayName = "Tester",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        await using var db = fixture.CreateContext();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private async Task<(User User, UserAvatarItem Ownership)> SeedOwnershipAsync(string itemKey)
    {
        var user = await SeedUserAsync();
        var ownership = NewOwnership(user.Id, itemKey);
        await using var db = fixture.CreateContext();
        db.UserAvatarItems.Add(ownership);
        await db.SaveChangesAsync();
        return (user, ownership);
    }

    private static UserAvatarItem NewOwnership(Guid userId, string itemKey) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        ItemKey = itemKey,
        AcquiredAt = DateTimeOffset.UtcNow,
    };

    private static UserAvatar NewAvatar(Guid userId) => new()
    {
        UserId = userId,
        EyesItemKey = "eyes.variant01",
        EyebrowsItemKey = "eyebrows.variant01",
        MouthItemKey = "mouth.variant01",
        NoseItemKey = "nose.variant01",
        HairItemKey = "hair.variant01",
        ClothesItemKey = "clothes.variant01",
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    private static AvatarItem NewItem(string key, AvatarSlot slot, string variantKey, AvatarTier tier, int price) => new()
    {
        Key = key,
        Slot = slot,
        VariantKey = variantKey,
        Name = "Thử",
        Tier = tier,
        Price = price,
        SortOrder = 999,
    };

    private static CoinTransaction NewTransaction(Guid userId, int amount, CoinTransactionReason reason, Guid? ownershipId) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Amount = amount,
        Reason = reason,
        UserAvatarItemId = ownershipId,
        CreatedAt = DateTimeOffset.UtcNow,
    };
}
