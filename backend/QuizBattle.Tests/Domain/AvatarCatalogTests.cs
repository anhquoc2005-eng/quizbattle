using System.Text.RegularExpressions;
using QuizBattle.Domain.Avatars;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Tests.Domain;

public sealed class AvatarCatalogTests
{
    private static IEnumerable<AvatarItemDefinition> Of(AvatarSlot slot) => AvatarCatalog.Items.Where(i => i.Slot == slot);

    private static string[] Variants(int from, int to) =>
        Enumerable.Range(from, to - from + 1).Select(n => $"variant{n:00}").ToArray();

    [Fact]
    public void Items_Total_Is201() => Assert.Equal(201, AvatarCatalog.Items.Count);

    [Fact]
    public void Items_FreeCount_Is85_PaidCount_Is116()
    {
        Assert.Equal(85, AvatarCatalog.Items.Count(i => i.Tier == AvatarTier.Free));
        Assert.Equal(116, AvatarCatalog.Items.Count(i => i.Tier != AvatarTier.Free));
    }

    [Fact]
    public void Items_TierCounts_Common53_Rare52_Special11()
    {
        Assert.Equal(53, AvatarCatalog.Items.Count(i => i.Tier == AvatarTier.Common));
        Assert.Equal(52, AvatarCatalog.Items.Count(i => i.Tier == AvatarTier.Rare));
        Assert.Equal(11, AvatarCatalog.Items.Count(i => i.Tier == AvatarTier.Special));
    }

    [Theory]
    [InlineData(AvatarSlot.Eyes, 5)]
    [InlineData(AvatarSlot.Eyebrows, 13)]
    [InlineData(AvatarSlot.Mouth, 30)]
    [InlineData(AvatarSlot.Nose, 20)]
    [InlineData(AvatarSlot.Hair, 64)]
    [InlineData(AvatarSlot.Clothes, 25)]
    [InlineData(AvatarSlot.Glasses, 11)]
    [InlineData(AvatarSlot.Beard, 12)]
    [InlineData(AvatarSlot.Gesture, 10)]
    [InlineData(AvatarSlot.ClothesGraphic, 3)]
    [InlineData(AvatarSlot.Background, 8)]
    public void Items_PerSlotCount_MatchesStyle(AvatarSlot slot, int expected) =>
        Assert.Equal(expected, Of(slot).Count());

    [Fact]
    public void Keys_AreUnique_AndEqualSlotDotVariantKey()
    {
        Assert.Equal(AvatarCatalog.Items.Count, AvatarCatalog.Items.Select(i => i.Key).Distinct().Count());
        Assert.All(AvatarCatalog.Items, i => Assert.Equal($"{AvatarSlots.ToKey(i.Slot)}.{i.VariantKey}", i.Key));
    }

    [Fact]
    public void SortOrder_IsUniqueAndIncreasingWithinSlot()
    {
        foreach (var slot in AvatarSlots.All)
        {
            var orders = Of(slot).Select(i => i.SortOrder).ToList();
            Assert.Equal(orders.Count, orders.Distinct().Count());
            Assert.Equal(orders.OrderBy(o => o), orders);
        }
    }

    [Fact]
    public void FreeItems_MatchApprovedList()
    {
        string[] Free(AvatarSlot slot) => Of(slot).Where(i => i.Tier == AvatarTier.Free).Select(i => i.VariantKey).Order().ToArray();

        Assert.Equal(Variants(1, 5), Free(AvatarSlot.Eyes));
        Assert.Equal(Variants(1, 13), Free(AvatarSlot.Eyebrows));
        Assert.Equal(Variants(1, 30), Free(AvatarSlot.Mouth));
        Assert.Equal(Variants(1, 20), Free(AvatarSlot.Nose));
        Assert.Equal(
            new[] { "variant01", "variant02", "variant05", "variant08", "variant20", "variant23", "variant41", "variant45", "variant61", "variant62", "variant63" },
            Free(AvatarSlot.Hair));
        Assert.Equal(new[] { "variant01", "variant02", "variant07", "variant14", "variant23" }, Free(AvatarSlot.Clothes));
        Assert.Equal(new[] { "d4d4d4" }, Free(AvatarSlot.Background));
        Assert.Empty(Free(AvatarSlot.Glasses));
        Assert.Empty(Free(AvatarSlot.Beard));
        Assert.Empty(Free(AvatarSlot.Gesture));
        Assert.Empty(Free(AvatarSlot.ClothesGraphic));
    }

    [Fact]
    public void HeadscarfHair_61_62_63_AreFreeAndNeutrallyNamed()
    {
        Assert.Equal("Khăn quấn đầu", AvatarCatalog.Find(AvatarSlot.Hair, "variant61")!.Name);
        Assert.Equal("Khăn trùm đầu 1", AvatarCatalog.Find(AvatarSlot.Hair, "variant62")!.Name);
        Assert.Equal("Khăn trùm đầu 2", AvatarCatalog.Find(AvatarSlot.Hair, "variant63")!.Name);
        Assert.All(new[] { "variant61", "variant62", "variant63" }, v =>
        {
            var item = AvatarCatalog.Find(AvatarSlot.Hair, v)!;
            Assert.Equal(AvatarTier.Free, item.Tier);
            Assert.Equal(0, item.Price);
        });
    }

    [Fact]
    public void Gesture_AllRare_150() =>
        Assert.All(Of(AvatarSlot.Gesture), i =>
        {
            Assert.Equal(AvatarTier.Rare, i.Tier);
            Assert.Equal(150, i.Price);
        });

    [Fact]
    public void ClothesGraphic_AllSpecial_400() =>
        Assert.All(Of(AvatarSlot.ClothesGraphic), i =>
        {
            Assert.Equal(AvatarTier.Special, i.Tier);
            Assert.Equal(400, i.Price);
        });

    [Fact]
    public void Prices_Are50_150_400_AndFreeIsZero()
    {
        Assert.Equal(0, AvatarCatalog.PriceOf(AvatarTier.Free));
        Assert.Equal(50, AvatarCatalog.PriceOf(AvatarTier.Common));
        Assert.Equal(150, AvatarCatalog.PriceOf(AvatarTier.Rare));
        Assert.Equal(400, AvatarCatalog.PriceOf(AvatarTier.Special));
        Assert.All(AvatarCatalog.Items, i => Assert.Equal(AvatarCatalog.PriceOf(i.Tier), i.Price));
    }

    [Fact]
    public void PaidHair_Tiers_Common24_Rare21_Special8_SpecialIsHatAndVariant54To60()
    {
        var paid = Of(AvatarSlot.Hair).Where(i => i.Tier != AvatarTier.Free).ToList();
        Assert.Equal(53, paid.Count);

        var common = paid.Where(i => i.Tier == AvatarTier.Common).Select(i => i.VariantKey).Order().ToArray();
        Assert.Equal(24, common.Length);
        Assert.Equal("variant03", common[0]);
        Assert.Equal("variant30", common[^1]);

        var rare = paid.Where(i => i.Tier == AvatarTier.Rare).Select(i => i.VariantKey).Order().ToArray();
        Assert.Equal(21, rare.Length);
        Assert.Equal("variant31", rare[0]);
        Assert.Equal("variant53", rare[^1]);

        var special = paid.Where(i => i.Tier == AvatarTier.Special).Select(i => i.VariantKey).Order().ToArray();
        Assert.Equal(new[] { "hat" }.Concat(Variants(54, 60)).Order().ToArray(), special);
    }

    [Fact]
    public void PaidClothes_Common12_Rare8()
    {
        var common = Of(AvatarSlot.Clothes).Where(i => i.Tier == AvatarTier.Common).Select(i => i.VariantKey).Order().ToArray();
        var rare = Of(AvatarSlot.Clothes).Where(i => i.Tier == AvatarTier.Rare).Select(i => i.VariantKey).Order().ToArray();

        Assert.Equal(
            new[] { "variant03", "variant04", "variant05", "variant06", "variant08", "variant09", "variant10", "variant11", "variant12", "variant13", "variant15", "variant16" },
            common);
        Assert.Equal(
            new[] { "variant17", "variant18", "variant19", "variant20", "variant21", "variant22", "variant24", "variant25" },
            rare);
    }

    [Fact]
    public void GlassesAndBeard_FirstSixCommon_RestRare()
    {
        Assert.Equal(6, Of(AvatarSlot.Glasses).Count(i => i.Tier == AvatarTier.Common));
        Assert.Equal(5, Of(AvatarSlot.Glasses).Count(i => i.Tier == AvatarTier.Rare));
        Assert.Equal(6, Of(AvatarSlot.Beard).Count(i => i.Tier == AvatarTier.Common));
        Assert.Equal(6, Of(AvatarSlot.Beard).Count(i => i.Tier == AvatarTier.Rare));
        Assert.Equal(AvatarTier.Common, AvatarCatalog.Find(AvatarSlot.Glasses, "variant06")!.Tier);
        Assert.Equal(AvatarTier.Rare, AvatarCatalog.Find(AvatarSlot.Glasses, "variant07")!.Tier);
        Assert.Equal(AvatarTier.Common, AvatarCatalog.Find(AvatarSlot.Beard, "variant06")!.Tier);
        Assert.Equal(AvatarTier.Rare, AvatarCatalog.Find(AvatarSlot.Beard, "variant07")!.Tier);
    }

    [Fact]
    public void Backgrounds_HexMatchesLowercase6Hex_5Common2RareOneFree()
    {
        var backgrounds = Of(AvatarSlot.Background).ToList();
        Assert.All(backgrounds, b => Assert.Matches("^[0-9a-f]{6}$", b.VariantKey));
        Assert.Equal(1, backgrounds.Count(b => b.Tier == AvatarTier.Free));
        Assert.Equal(5, backgrounds.Count(b => b.Tier == AvatarTier.Common));
        Assert.Equal(2, backgrounds.Count(b => b.Tier == AvatarTier.Rare));
        Assert.Equal(
            new[] { "d4d4d4", "b9c6d6", "b8d4be", "e3d5b8", "e8c4c4", "cfc4e3", "a9d3dc", "ebc4a0" }.Order(),
            backgrounds.Select(b => b.VariantKey).Order());
    }

    [Fact]
    public void TotalPriceOfAllPaidItems_Is14850() =>
        Assert.Equal(14850, AvatarCatalog.Items.Sum(i => i.Price));

    [Fact]
    public void PrintableClothes_IsVariant01To10()
    {
        Assert.Equal(Variants(1, 10), AvatarCatalog.PrintableClothesVariantKeys.Order().ToArray());
        Assert.All(AvatarCatalog.PrintableClothesVariantKeys, v => Assert.NotNull(AvatarCatalog.Find(AvatarSlot.Clothes, v)));
    }

    [Fact]
    public void StyleVersion_Is10_6_0() => Assert.Equal("10.6.0", AvatarCatalog.StyleVersion);

    [Fact]
    public void DefaultBackgroundHex_Is_d4d4d4_AndIsAFreeItem()
    {
        Assert.Equal("d4d4d4", AvatarCatalog.DefaultBackgroundHex);
        Assert.Equal(AvatarTier.Free, AvatarCatalog.Find(AvatarSlot.Background, AvatarCatalog.DefaultBackgroundHex)!.Tier);
    }

    [Fact]
    public void Names_AreNonEmptyAndWithin60Chars() =>
        Assert.All(AvatarCatalog.Items, i =>
        {
            Assert.False(string.IsNullOrWhiteSpace(i.Name));
            Assert.True(i.Name.Length <= 60);
        });

    [Fact]
    public void Find_UnknownOrWrongSlot_ReturnsNull()
    {
        Assert.Null(AvatarCatalog.Find(AvatarSlot.Eyes, "variant99"));
        Assert.Null(AvatarCatalog.Find(AvatarSlot.Glasses, "ok")); // "ok" là của cử chỉ
        Assert.NotNull(AvatarCatalog.Find(AvatarSlot.Gesture, "ok"));
        Assert.NotNull(AvatarCatalog.FindByKey("hair.variant12"));
        Assert.Null(AvatarCatalog.FindByKey("hair.nope"));
    }
}
