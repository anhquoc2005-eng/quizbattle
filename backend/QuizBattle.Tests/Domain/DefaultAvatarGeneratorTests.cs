using QuizBattle.Domain.Avatars;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Tests.Domain;

public sealed class DefaultAvatarGeneratorTests
{
    private static readonly AvatarSlot[] OptionalSlots =
        [AvatarSlot.Glasses, AvatarSlot.Beard, AvatarSlot.Gesture, AvatarSlot.ClothesGraphic];

    private static void AssertSame(AvatarSelection a, AvatarSelection b)
    {
        foreach (var slot in AvatarSlots.All)
        {
            Assert.Equal(a.Get(slot), b.Get(slot));
        }
    }

    private static void AssertOnlyFreeItems(AvatarSelection avatar)
    {
        foreach (var slot in AvatarSlots.All)
        {
            if (avatar.Get(slot) is { } variantKey)
            {
                Assert.Equal(AvatarTier.Free, AvatarCatalog.Find(slot, variantKey)!.Tier);
            }
        }
    }

    [Fact]
    public void ForAccount_SameUserId_ReturnsSameAvatar()
    {
        var id = Guid.Parse("3f2c9a1e-8b47-4d65-9c1a-5e0d7a6b2f10");

        AssertSame(DefaultAvatarGenerator.ForAccount(id), DefaultAvatarGenerator.ForAccount(id));
        AssertSame(DefaultAvatarGenerator.ForAccount(id), DefaultAvatarGenerator.ForAccount(Guid.Parse(id.ToString())));
    }

    [Fact]
    public void ForAccount_OnlyUsesFreeItems() =>
        AssertOnlyFreeItems(DefaultAvatarGenerator.ForAccount(Guid.NewGuid()));

    [Fact]
    public void ForAccount_HasSixRequiredSlots_OptionalSlotsEmpty_DefaultBackground()
    {
        var avatar = DefaultAvatarGenerator.ForAccount(Guid.NewGuid());

        Assert.All(AvatarSlots.Required, slot => Assert.NotNull(avatar.Get(slot)));
        Assert.All(OptionalSlots, slot => Assert.Null(avatar.Get(slot)));
        Assert.Equal(AvatarCatalog.DefaultBackgroundHex, avatar.Get(AvatarSlot.Background));
    }

    [Fact]
    public void ForGuest_SameNameDifferentCaseAndPadding_ReturnsSameAvatar()
    {
        var plain = DefaultAvatarGenerator.ForGuest("Minh Anh");

        AssertSame(plain, DefaultAvatarGenerator.ForGuest("minh anh"));
        AssertSame(plain, DefaultAvatarGenerator.ForGuest("  MINH ANH  "));
    }

    [Fact]
    public void ForGuest_OnlyUsesFreeItems_GreyBackground()
    {
        var avatar = DefaultAvatarGenerator.ForGuest("Khách 1");

        AssertOnlyFreeItems(avatar);
        Assert.Equal("d4d4d4", avatar.Get(AvatarSlot.Background));
        Assert.All(AvatarSlots.Required, slot => Assert.NotNull(avatar.Get(slot)));
    }

    [Fact]
    public void ForGuest_DifferentNames_CanDiffer()
    {
        var signatures = Enumerable.Range(0, 50)
            .Select(i => string.Join('|', AvatarSlots.All.Select(s => DefaultAvatarGenerator.ForGuest($"khach{i}").Get(s))))
            .Distinct()
            .Count();

        Assert.True(signatures > 40, $"chỉ có {signatures} nhân vật khác nhau trong 50 tên");
    }

    // Giá trị cố định (tính độc lập bằng JavaScript/BigInt, không chỉ tự khẳng định; tóc chỉ chọn trong 8 kiểu, không có khăn trùm đầu): nếu ai đổi hash hoặc danh sách món miễn phí thì nhân vật mặc định của MỌI người đổi hình,
    // test này fail để việc đó phải là quyết định có chủ đích. Cũng là bằng chứng không dùng string.GetHashCode()
    // (giá trị đó đổi giữa các lần chạy process).
    public static TheoryData<string, string> Golden => new()
    {
        { "u:11111111-1111-1111-1111-111111111111", "eyes=variant01,eyebrows=variant01,mouth=variant13,nose=variant19,hair=variant02,clothes=variant14,glasses=-,beard=-,gesture=-,clothesGraphic=-,background=d4d4d4" },
        { "u:3f2c9a1e-8b47-4d65-9c1a-5e0d7a6b2f10", "eyes=variant04,eyebrows=variant01,mouth=variant05,nose=variant11,hair=variant02,clothes=variant07,glasses=-,beard=-,gesture=-,clothesGraphic=-,background=d4d4d4" },
        { "u:00000000-0000-0000-0000-000000000000", "eyes=variant02,eyebrows=variant10,mouth=variant22,nose=variant03,hair=variant20,clothes=variant07,glasses=-,beard=-,gesture=-,clothesGraphic=-,background=d4d4d4" },
        { "g:Minh Anh", "eyes=variant01,eyebrows=variant09,mouth=variant08,nose=variant04,hair=variant02,clothes=variant01,glasses=-,beard=-,gesture=-,clothesGraphic=-,background=d4d4d4" },
        { "g:An", "eyes=variant04,eyebrows=variant04,mouth=variant22,nose=variant19,hair=variant08,clothes=variant02,glasses=-,beard=-,gesture=-,clothesGraphic=-,background=d4d4d4" },
        { "g:Khách 1", "eyes=variant04,eyebrows=variant12,mouth=variant11,nose=variant02,hair=variant05,clothes=variant01,glasses=-,beard=-,gesture=-,clothesGraphic=-,background=d4d4d4" },
    };

    [Theory]
    [MemberData(nameof(Golden))]
    public void ForAccountAndGuest_GoldenValues(string input, string expected)
    {
        var avatar = input.StartsWith("u:") ? DefaultAvatarGenerator.ForAccount(Guid.Parse(input[2..])) : DefaultAvatarGenerator.ForGuest(input[2..]);

        Assert.Equal(expected, Describe(avatar));
    }

    internal static string Describe(AvatarSelection avatar) =>
        string.Join(',', AvatarSlots.All.Select(s => $"{AvatarSlots.ToKey(s)}={avatar.Get(s) ?? "-"}"));

    [Fact]
    public void ForAccount_TenThousandAccounts_NeverWearHeadscarf()
    {
        // Khăn trùm đầu là trang phục tôn giáo: miễn phí và chọn được ở trình chỉnh, nhưng nhân vật mặc định không bao giờ tự mang.
        var rng = new Random(2024);
        var bytes = new byte[16];
        var hairs = new HashSet<string>();

        for (var i = 0; i < 10_000; i++)
        {
            rng.NextBytes(bytes);
            hairs.Add(DefaultAvatarGenerator.ForAccount(new Guid(bytes)).Get(AvatarSlot.Hair)!);
        }

        Assert.DoesNotContain("variant61", hairs);
        Assert.DoesNotContain("variant62", hairs);
        Assert.DoesNotContain("variant63", hairs);
        Assert.Equal(8, hairs.Count); // đủ 8 kiểu tóc miễn phí thường đều xuất hiện
    }

    [Fact]
    public void ForGuest_TenThousandNames_NeverWearHeadscarf()
    {
        var hairs = Enumerable.Range(0, 10_000)
            .Select(i => DefaultAvatarGenerator.ForGuest($"người chơi {i}").Get(AvatarSlot.Hair)!)
            .ToHashSet();

        Assert.DoesNotContain("variant61", hairs);
        Assert.DoesNotContain("variant62", hairs);
        Assert.DoesNotContain("variant63", hairs);
        Assert.Equal(8, hairs.Count);
    }

    [Fact]
    public void ForAccount_TenThousandIds_AllValidAndWellDistributed()
    {
        var seen = AvatarSlots.Required.ToDictionary(s => s, _ => new HashSet<string>());
        var rng = new Random(12345);
        var bytes = new byte[16];

        for (var i = 0; i < 10_000; i++)
        {
            rng.NextBytes(bytes);
            var avatar = DefaultAvatarGenerator.ForAccount(new Guid(bytes));

            Assert.Empty(AvatarRules.Validate(avatar));
            AssertOnlyFreeItems(avatar);
            foreach (var slot in AvatarSlots.Required)
            {
                seen[slot].Add(avatar.Get(slot)!);
            }
        }

        foreach (var slot in AvatarSlots.Required)
        {
            var freeCount = AvatarCatalog.DefaultFreeItems(slot).Count;
            Assert.True(seen[slot].Count >= Math.Ceiling(freeCount * 0.9), $"{slot}: chỉ thấy {seen[slot].Count}/{freeCount} kiểu miễn phí");
        }
    }
}
