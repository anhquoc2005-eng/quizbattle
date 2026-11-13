using QuizBattle.Application.Avatars;
using QuizBattle.Application.Common;
using QuizBattle.Domain.Avatars;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;
using QuizBattle.Tests.TestHelpers;

namespace QuizBattle.Tests.Avatars;

public sealed class AvatarServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 13, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    private static (AvatarService Service, FakeAvatarRepository Repository) Setup()
    {
        var repository = new FakeAvatarRepository().WithFullCatalog();
        return (new AvatarService(repository, new FakeTimeProvider(Now)), repository);
    }

    // Nhân vật hợp lệ chỉ gồm món miễn phí; áo variant01 in được hình.
    private static SaveAvatarRequest FreeRequest() => new(
        "variant01", "variant01", "variant01", "variant01", "variant01", "variant01", null, null, null, null, null);

    private static void Own(FakeAvatarRepository repository, string itemKey) =>
        repository.Ownerships.Add(new UserAvatarItem { Id = Guid.NewGuid(), UserId = UserId, ItemKey = itemKey, AcquiredAt = Now });

    [Fact]
    public async Task GetMyAvatar_NeverSaved_ReturnsDefaultAvatarFromGeneratorAndIsDefaultTrue()
    {
        var (service, _) = Setup();

        var result = await service.GetMyAvatarAsync(UserId, CancellationToken.None);

        var expected = DefaultAvatarGenerator.ForAccount(UserId);
        Assert.True(result.IsDefault);
        Assert.Equal(expected.Get(AvatarSlot.Hair), result.Avatar.Hair);
        Assert.Equal(expected.Get(AvatarSlot.Eyes), result.Avatar.Eyes);
        Assert.Equal(expected.Get(AvatarSlot.Clothes), result.Avatar.Clothes);
        Assert.Equal(AvatarCatalog.DefaultBackgroundHex, result.Avatar.Background);
        Assert.Null(result.Avatar.Glasses);
        Assert.Null(result.Avatar.ClothesGraphic);
    }

    [Fact]
    public async Task GetMyAvatar_Saved_ReturnsSavedAndIsDefaultFalse()
    {
        var (service, repository) = Setup();
        Own(repository, "gesture.ok");
        repository.UserAvatars[UserId] = new UserAvatar
        {
            UserId = UserId, EyesItemKey = "eyes.variant02", EyebrowsItemKey = "eyebrows.variant03", MouthItemKey = "mouth.variant04",
            NoseItemKey = "nose.variant05", HairItemKey = "hair.variant62", ClothesItemKey = "clothes.variant07",
            GestureItemKey = "gesture.ok", BackgroundItemKey = "background.b9c6d6", UpdatedAt = Now,
        };

        var result = await service.GetMyAvatarAsync(UserId, CancellationToken.None);

        Assert.False(result.IsDefault);
        Assert.Equal(
            new AvatarDto("variant02", "variant03", "variant04", "variant05", "variant62", "variant07", null, null, "ok", null, "b9c6d6"),
            result.Avatar);
    }

    [Fact]
    public async Task GetMyAvatar_OwnedKeysListPaidItemsOnly()
    {
        var (service, repository) = Setup();
        Own(repository, "hair.variant03");
        Own(repository, "gesture.ok");

        var result = await service.GetMyAvatarAsync(UserId, CancellationToken.None);

        Assert.Equal(["gesture.ok", "hair.variant03"], result.OwnedItemKeys);
    }

    [Fact]
    public async Task Save_AllFreeItems_Succeeds()
    {
        var (service, repository) = Setup();

        var result = await service.SaveMyAvatarAsync(UserId, FreeRequest(), CancellationToken.None);

        Assert.False(result.IsDefault);
        Assert.Equal("variant01", result.Avatar.Hair);
        var saved = repository.UserAvatars[UserId];
        Assert.Equal(("eyes.variant01", "hair.variant01", "clothes.variant01", Now), (saved.EyesItemKey, saved.HairItemKey, saved.ClothesItemKey, saved.UpdatedAt));
        Assert.Null(saved.GlassesItemKey);
    }

    [Fact]
    public async Task Save_HeadscarfIsFreeAndSelectable()
    {
        var (service, repository) = Setup();

        await service.SaveMyAvatarAsync(UserId, FreeRequest() with { Hair = "variant62" }, CancellationToken.None);

        Assert.Equal("hair.variant62", repository.UserAvatars[UserId].HairItemKey);
    }

    [Theory]
    [InlineData("eyes")]
    [InlineData("eyebrows")]
    [InlineData("mouth")]
    [InlineData("nose")]
    [InlineData("hair")]
    [InlineData("clothes")]
    public async Task Save_MissingRequiredSlot_Throws400ValidationFailed(string slot)
    {
        var (service, repository) = Setup();
        var request = slot switch
        {
            "eyes" => FreeRequest() with { Eyes = null },
            "eyebrows" => FreeRequest() with { Eyebrows = null },
            "mouth" => FreeRequest() with { Mouth = null },
            "nose" => FreeRequest() with { Nose = null },
            "hair" => FreeRequest() with { Hair = null },
            _ => FreeRequest() with { Clothes = null },
        };

        var ex = await Assert.ThrowsAsync<RequestValidationException>(() => service.SaveMyAvatarAsync(UserId, request, CancellationToken.None));

        Assert.Equal((slot, "validation_failed"), (ex.Field, ex.Code));
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task Save_UnknownVariant_Throws400UnknownVariant()
    {
        var (service, repository) = Setup();

        var ex = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.SaveMyAvatarAsync(UserId, FreeRequest() with { Eyes = "variant99" }, CancellationToken.None));

        Assert.Equal(("eyes", "unknown_variant"), (ex.Field, ex.Code));
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task Save_PaidItemNotOwned_Throws409ItemNotOwned()
    {
        var (service, repository) = Setup();

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => service.SaveMyAvatarAsync(UserId, FreeRequest() with { Gesture = "ok" }, CancellationToken.None));

        Assert.Equal(("item_not_owned", "gesture"), (ex.Code, ex.Field));
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task Save_PaidItemOwned_Succeeds()
    {
        var (service, repository) = Setup();
        Own(repository, "gesture.ok");
        Own(repository, "background.a9d3dc");

        await service.SaveMyAvatarAsync(UserId, FreeRequest() with { Gesture = "ok", Background = "a9d3dc" }, CancellationToken.None);

        var saved = repository.UserAvatars[UserId];
        Assert.Equal(("gesture.ok", "background.a9d3dc"), (saved.GestureItemKey, saved.BackgroundItemKey));
    }

    [Fact]
    public async Task Save_OwnedBySomeoneElse_StillNotOwned()
    {
        var (service, repository) = Setup();
        repository.Ownerships.Add(new UserAvatarItem { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), ItemKey = "gesture.ok", AcquiredAt = Now });

        await Assert.ThrowsAsync<ConflictException>(
            () => service.SaveMyAvatarAsync(UserId, FreeRequest() with { Gesture = "ok" }, CancellationToken.None));
    }

    [Fact]
    public async Task Save_GraphicOnNonPrintableClothes_Throws400GraphicIncompatibleClothes()
    {
        var (service, repository) = Setup();
        Own(repository, "clothesGraphic.galaxy");

        var ex = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.SaveMyAvatarAsync(UserId, FreeRequest() with { Clothes = "variant14", ClothesGraphic = "galaxy" }, CancellationToken.None));

        Assert.Equal(("clothesGraphic", "graphic_incompatible_clothes"), (ex.Field, ex.Code));
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task Save_GraphicOnPrintableClothes_Succeeds()
    {
        var (service, repository) = Setup();
        Own(repository, "clothesGraphic.galaxy");
        Own(repository, "clothes.variant10"); // áo 10 in được hình nhưng là áo bán (Thường)

        await service.SaveMyAvatarAsync(UserId, FreeRequest() with { Clothes = "variant10", ClothesGraphic = "galaxy" }, CancellationToken.None);

        Assert.Equal("clothesGraphic.galaxy", repository.UserAvatars[UserId].ClothesGraphicItemKey);
    }

    [Fact]
    public async Task Save_OneBadSlot_RejectsWholeRequestAndSavesNothing()
    {
        var (service, repository) = Setup();
        Own(repository, "gesture.ok");
        // Một ô tốt (gesture đã sở hữu) cộng một ô xấu (kính chưa sở hữu): không lưu một phần nào.
        var request = FreeRequest() with { Gesture = "ok", Glasses = "variant01" };

        await Assert.ThrowsAsync<ConflictException>(() => service.SaveMyAvatarAsync(UserId, request, CancellationToken.None));

        Assert.Empty(repository.UserAvatars);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task Save_OptionalSlotsNull_ClearsThem()
    {
        var (service, repository) = Setup();
        Own(repository, "gesture.ok");
        await service.SaveMyAvatarAsync(UserId, FreeRequest() with { Gesture = "ok" }, CancellationToken.None);

        var result = await service.SaveMyAvatarAsync(UserId, FreeRequest(), CancellationToken.None);

        Assert.Null(result.Avatar.Gesture);
        Assert.Null(repository.UserAvatars[UserId].GestureItemKey);
    }

    [Fact]
    public async Task Save_ErrorsReportedInFixedSlotOrder()
    {
        var (service, _) = Setup();

        // Hai ô lỗi: báo ô đứng trước theo thứ tự cố định (mắt trước tóc), bất kể thứ tự trong request.
        var ex = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.SaveMyAvatarAsync(UserId, FreeRequest() with { Hair = "variant99", Eyes = "variant99" }, CancellationToken.None));

        Assert.Equal("eyes", ex.Field);
    }

    [Fact]
    public async Task Save_RulesErrorWinsOverNotOwned()
    {
        var (service, _) = Setup();

        // Kính chưa sở hữu (409) nhưng mắt sai (400): lỗi dữ liệu đi trước lỗi sở hữu.
        var ex = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.SaveMyAvatarAsync(UserId, FreeRequest() with { Eyes = "variant99", Glasses = "variant01" }, CancellationToken.None));

        Assert.Equal("eyes", ex.Field);
    }

    [Fact]
    public async Task Save_ItemMissingFromDatabaseCatalog_Throws400UnknownVariant()
    {
        // Danh mục trong DB là nguồn kiểm cuối cùng: món có trong mã nhưng thiếu trong DB thì từ chối.
        var (service, repository) = Setup();
        repository.Catalog.RemoveAll(i => i.Key == "eyes.variant05");

        var ex = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.SaveMyAvatarAsync(UserId, FreeRequest() with { Eyes = "variant05" }, CancellationToken.None));

        Assert.Equal(("eyes", "unknown_variant"), (ex.Field, ex.Code));
    }
}
