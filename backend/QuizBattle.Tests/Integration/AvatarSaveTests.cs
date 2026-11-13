using Microsoft.EntityFrameworkCore;
using QuizBattle.Application.Avatars;
using QuizBattle.Application.Common;
using QuizBattle.Domain.Avatars;
using QuizBattle.Domain.Entities;
using QuizBattle.Infrastructure.Persistence;

namespace QuizBattle.Tests.Integration;

/// <summary>Đọc và lưu nhân vật trên Postgres thật: upsert, từ chối không lưu một phần, hai lần lưu đầu tiên đồng thời.</summary>
[Collection(DatabaseCollection.Name)]
public sealed class AvatarSaveTests(DatabaseFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 11, 13, 9, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private static SaveAvatarRequest FreeRequest() => new(
        "variant01", "variant01", "variant01", "variant01", "variant01", "variant01", null, null, null, null, null);

    [RequiresDatabaseFact]
    public async Task Save_FirstTime_InsertsRow()
    {
        var user = await SeedUserAsync();

        var result = await SaveAsync(user.Id, FreeRequest() with { Hair = "variant62", Background = null });

        Assert.False(result.IsDefault);
        await using var db = fixture.CreateContext();
        var row = await db.UserAvatars.AsNoTracking().SingleAsync(a => a.UserId == user.Id);
        Assert.Equal(("hair.variant62", "eyes.variant01", null), (row.HairItemKey, row.EyesItemKey, row.BackgroundItemKey));
    }

    [RequiresDatabaseFact]
    public async Task Save_SecondTime_UpdatesSameRow()
    {
        var user = await SeedUserAsync();
        await SaveAsync(user.Id, FreeRequest());

        await SaveAsync(user.Id, FreeRequest() with { Eyes = "variant05", Mouth = "variant30", Background = "d4d4d4" });

        await using var db = fixture.CreateContext();
        var row = await db.UserAvatars.AsNoTracking().SingleAsync(a => a.UserId == user.Id); // vẫn đúng một dòng
        Assert.Equal(("eyes.variant05", "mouth.variant30", "background.d4d4d4"), (row.EyesItemKey, row.MouthItemKey, row.BackgroundItemKey));
    }

    [RequiresDatabaseFact]
    public async Task Save_UnownedPaidItem_RejectedAndRowUnchanged()
    {
        var user = await SeedUserAsync();
        await SaveAsync(user.Id, FreeRequest());

        var ex = await Assert.ThrowsAsync<ConflictException>(() => SaveAsync(user.Id, FreeRequest() with { Eyes = "variant05", Gesture = "ok" }));

        Assert.Equal("item_not_owned", ex.Code);
        await using var db = fixture.CreateContext();
        var row = await db.UserAvatars.AsNoTracking().SingleAsync(a => a.UserId == user.Id);
        Assert.Equal(("eyes.variant01", null), (row.EyesItemKey, row.GestureItemKey)); // ô tốt (mắt) cũng không được lưu
    }

    [RequiresDatabaseFact]
    public async Task Save_UnownedPaidItemOnFirstSave_WritesNoRow()
    {
        var user = await SeedUserAsync();

        await Assert.ThrowsAsync<ConflictException>(() => SaveAsync(user.Id, FreeRequest() with { Gesture = "ok" }));

        await using var db = fixture.CreateContext();
        Assert.False(await db.UserAvatars.AnyAsync(a => a.UserId == user.Id));
    }

    [RequiresDatabaseFact]
    public async Task Save_ConcurrentFirstSaves_BothSucceedWithoutServerError()
    {
        for (var round = 0; round < 8; round++)
        {
            var user = await SeedUserAsync();

            var tasks = Enumerable.Range(0, 4).Select(i => SaveAsync(user.Id, FreeRequest() with { Eyes = $"variant0{i + 1}" }));
            var results = await Task.WhenAll(tasks).WaitAsync(Timeout); // ném lỗi nếu bên thua không được thử lại

            Assert.All(results, r => Assert.False(r.IsDefault));
            await using var db = fixture.CreateContext();
            Assert.Equal(1, await db.UserAvatars.CountAsync(a => a.UserId == user.Id));
        }
    }

    [RequiresDatabaseFact]
    public async Task Save_OwnershipSurvivesAcrossSaves()
    {
        var user = await SeedUserAsync();
        await SeedOwnershipAsync(user.Id, "gesture.ok");

        await SaveAsync(user.Id, FreeRequest() with { Gesture = "ok" });
        await SaveAsync(user.Id, FreeRequest()); // bỏ cử chỉ
        await SaveAsync(user.Id, FreeRequest() with { Gesture = "ok" }); // mặc lại không cần mua lại

        var mine = await GetAsync(user.Id);
        Assert.Equal("ok", mine.Avatar.Gesture);
        Assert.Equal(["gesture.ok"], mine.OwnedItemKeys);
    }

    [RequiresDatabaseFact]
    public async Task Get_AccountCreatedBeforeFeature_ReturnsDefaultWithoutWritingRow()
    {
        var user = await SeedUserAsync();

        var first = await GetAsync(user.Id);
        var second = await GetAsync(user.Id);

        Assert.True(first.IsDefault);
        Assert.Equal(first.Avatar, second.Avatar); // cố định giữa các lần xem
        Assert.Equal(AvatarMapping(DefaultAvatarGenerator.ForAccount(user.Id)), first.Avatar);
        await using var db = fixture.CreateContext();
        Assert.False(await db.UserAvatars.AnyAsync(a => a.UserId == user.Id)); // không sửa dữ liệu cũ
    }

    [RequiresDatabaseFact]
    public async Task Get_AfterSave_ReturnsSavedAndNotDefault()
    {
        var user = await SeedUserAsync();
        await SaveAsync(user.Id, FreeRequest() with { Hair = "variant61", Background = "d4d4d4" });

        var mine = await GetAsync(user.Id);

        Assert.False(mine.IsDefault);
        Assert.Equal(("variant61", "d4d4d4"), (mine.Avatar.Hair, mine.Avatar.Background));
        Assert.Empty(mine.OwnedItemKeys); // món miễn phí không có dòng sở hữu
    }

    [RequiresDatabaseFact]
    public async Task Get_OwnedKeysDoNotIncludeOtherUsersItems()
    {
        var me = await SeedUserAsync();
        var other = await SeedUserAsync();
        await SeedOwnershipAsync(me.Id, "hair.variant03");
        await SeedOwnershipAsync(other.Id, "gesture.ok");

        Assert.Equal(["hair.variant03"], (await GetAsync(me.Id)).OwnedItemKeys);
    }

    // So sánh nhân vật mặc định (Domain) với DTO: chỉ cần các ô bắt buộc và nền.
    private static AvatarDto AvatarMapping(AvatarSelection s) => new(
        s.Get(QuizBattle.Domain.Enums.AvatarSlot.Eyes)!, s.Get(QuizBattle.Domain.Enums.AvatarSlot.Eyebrows)!,
        s.Get(QuizBattle.Domain.Enums.AvatarSlot.Mouth)!, s.Get(QuizBattle.Domain.Enums.AvatarSlot.Nose)!,
        s.Get(QuizBattle.Domain.Enums.AvatarSlot.Hair)!, s.Get(QuizBattle.Domain.Enums.AvatarSlot.Clothes)!,
        null, null, null, null, s.Get(QuizBattle.Domain.Enums.AvatarSlot.Background));

    private async Task<SavedAvatarDto> SaveAsync(Guid userId, SaveAvatarRequest request)
    {
        await Task.Yield();
        await using var db = fixture.CreateContext();
        return await new AvatarService(new AvatarRepository(db), new Tests.TestHelpers.FakeTimeProvider(Now))
            .SaveMyAvatarAsync(userId, request, CancellationToken.None);
    }

    private async Task<MyAvatarDto> GetAsync(Guid userId)
    {
        await using var db = fixture.CreateContext();
        return await new AvatarService(new AvatarRepository(db), new Tests.TestHelpers.FakeTimeProvider(Now))
            .GetMyAvatarAsync(userId, CancellationToken.None);
    }

    private async Task<User> SeedUserAsync()
    {
        var id = Guid.NewGuid();
        var user = new User { Id = id, Email = $"{id:N}@test.local", PasswordHash = "h", DisplayName = "Lưu", CreatedAt = Now };
        await using var db = fixture.CreateContext();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private async Task SeedOwnershipAsync(Guid userId, string itemKey)
    {
        await using var db = fixture.CreateContext();
        db.UserAvatarItems.Add(new UserAvatarItem { Id = Guid.NewGuid(), UserId = userId, ItemKey = itemKey, AcquiredAt = Now });
        await db.SaveChangesAsync();
    }
}
