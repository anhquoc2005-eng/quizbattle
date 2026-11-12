using QuizBattle.Application.Shop;
using QuizBattle.Domain.Avatars;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;
using QuizBattle.Tests.TestHelpers;

namespace QuizBattle.Tests.Shop;

public sealed class ShopServiceCatalogTests
{
    private static AvatarItem Item(AvatarSlot slot, string variantKey, AvatarTier tier, int sortOrder) => new()
    {
        Key = $"{AvatarSlots.ToKey(slot)}.{variantKey}",
        Slot = slot,
        VariantKey = variantKey,
        Name = $"Tên {variantKey}",
        Tier = tier,
        Price = AvatarCatalog.PriceOf(tier),
        SortOrder = sortOrder,
    };

    [Fact]
    public async Task GetCatalog_ReturnsAllItemsOrderedBySlotThenSortOrder()
    {
        var repository = new FakeAvatarRepository();
        repository.Catalog.AddRange(
        [
            Item(AvatarSlot.Hair, "variant02", AvatarTier.Common, 1),
            Item(AvatarSlot.Eyes, "variant01", AvatarTier.Free, 0),
            Item(AvatarSlot.Hair, "variant01", AvatarTier.Free, 0),
        ]);

        var items = await new ShopService(repository).GetCatalogAsync(CancellationToken.None);

        Assert.Equal(["eyes.variant01", "hair.variant01", "hair.variant02"], items.Select(i => i.Key));
    }

    [Fact]
    public async Task GetCatalog_TierIsLowercaseString_PriceFromRepository()
    {
        var repository = new FakeAvatarRepository();
        repository.Catalog.AddRange(
        [
            Item(AvatarSlot.Hair, "variant01", AvatarTier.Free, 0),
            Item(AvatarSlot.Hair, "variant03", AvatarTier.Common, 1),
            Item(AvatarSlot.Gesture, "ok", AvatarTier.Rare, 0),
            Item(AvatarSlot.ClothesGraphic, "galaxy", AvatarTier.Special, 0),
        ]);

        var items = await new ShopService(repository).GetCatalogAsync(CancellationToken.None);

        var byKey = items.ToDictionary(i => i.Key);
        Assert.Equal(("free", 0), (byKey["hair.variant01"].Tier, byKey["hair.variant01"].Price));
        Assert.Equal(("common", 50), (byKey["hair.variant03"].Tier, byKey["hair.variant03"].Price));
        Assert.Equal(("rare", 150), (byKey["gesture.ok"].Tier, byKey["gesture.ok"].Price));
        Assert.Equal(("special", 400), (byKey["clothesGraphic.galaxy"].Tier, byKey["clothesGraphic.galaxy"].Price));
    }

    [Fact]
    public async Task GetCatalog_MapsSlotToCamelCaseKeyAndKeepsNameAndVariant()
    {
        var repository = new FakeAvatarRepository();
        repository.Catalog.Add(Item(AvatarSlot.ClothesGraphic, "saturn", AvatarTier.Special, 2));

        var item = Assert.Single(await new ShopService(repository).GetCatalogAsync(CancellationToken.None));

        Assert.Equal("clothesGraphic", item.Slot);
        Assert.Equal("saturn", item.VariantKey);
        Assert.Equal("Tên saturn", item.Name);
    }
}
