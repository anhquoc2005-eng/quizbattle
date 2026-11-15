using QuizBattle.Domain.Enums;

namespace QuizBattle.Domain.Avatars;

public sealed record AvatarItemDefinition(AvatarSlot Slot, string VariantKey, string Name, AvatarTier Tier, int Price, int SortOrder)
{
    /// <summary>Khóa món đồ ổn định, dạng "ô.variantKey" (ví dụ "hair.variant12"). Là khóa chính trong DB.</summary>
    public string Key => $"{AvatarSlots.ToKey(Slot)}.{VariantKey}";
}

/// <summary>
/// Danh mục 201 món - nguồn duy nhất cho seed (HasData), nhân vật mặc định và test đồng bộ với file style DiceBear.
/// Sửa danh mục sau khi đã áp migration lên production nghĩa là phải có migration mới.
/// </summary>
public static class AvatarCatalog
{
    /// <summary>Phiên bản @dicebear/styles đã ghim; test so với package.json của gói đã cài.</summary>
    public const string StyleVersion = "10.6.0";

    /// <summary>Nền mặc định (xám), cũng là nền của khách và của ô nền để trống. Là một món miễn phí.</summary>
    public const string DefaultBackgroundHex = "d4d4d4";

    /// <summary>Kiểu áo có chỗ in hình (đo bằng cách vẽ từng áo có/không hình in trên DiceBear 10.6.0).</summary>
    public static IReadOnlyList<string> PrintableClothesVariantKeys { get; } = Variants(1, 10);

    /// <summary>
    /// Khăn quấn đầu và khăn hijab (ô tóc): trang phục tôn giáo, luôn miễn phí và chọn được ở trình chỉnh, nhưng nhân vật mặc định
    /// KHÔNG BAO GIỜ tự mang chúng.
    /// </summary>
    public static IReadOnlyList<string> HeadscarfVariantKeys { get; } = ["variant61", "variant62", "variant63"];

    public static IReadOnlyList<AvatarSlot> RequiredSlots => AvatarSlots.Required;

    public static IReadOnlyList<AvatarItemDefinition> Items { get; } = Build();

    private static readonly Dictionary<string, AvatarItemDefinition> ByKey = Items.ToDictionary(i => i.Key);

    private static readonly Dictionary<AvatarSlot, IReadOnlyList<AvatarItemDefinition>> FreeBySlot = Items
        .Where(i => i.Tier == AvatarTier.Free)
        .GroupBy(i => i.Slot)
        .ToDictionary(g => g.Key, g => (IReadOnlyList<AvatarItemDefinition>)g.OrderBy(i => i.SortOrder).ToList());

    public static int PriceOf(AvatarTier tier) => tier switch
    {
        AvatarTier.Free => 0,
        AvatarTier.Common => 50,
        AvatarTier.Rare => 150,
        AvatarTier.Special => 400,
        _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null),
    };

    public static AvatarItemDefinition? Find(AvatarSlot slot, string variantKey) =>
        ByKey.GetValueOrDefault($"{AvatarSlots.ToKey(slot)}.{variantKey}");

    public static AvatarItemDefinition? FindByKey(string key) => ByKey.GetValueOrDefault(key);

    /// <summary>Các món miễn phí của ô, theo SortOrder (rỗng nếu ô không có món miễn phí).</summary>
    public static IReadOnlyList<AvatarItemDefinition> FreeItems(AvatarSlot slot) =>
        FreeBySlot.GetValueOrDefault(slot) ?? [];

    /// <summary>
    /// Món miễn phí mà nhân vật mặc định được phép chọn: như <see cref="FreeItems"/> nhưng bỏ khăn trùm đầu ở ô tóc.
    /// </summary>
    public static IReadOnlyList<AvatarItemDefinition> DefaultFreeItems(AvatarSlot slot) =>
        slot == AvatarSlot.Hair
            ? FreeItems(slot).Where(i => !HeadscarfVariantKeys.Contains(i.VariantKey)).ToList()
            : FreeItems(slot);

    private static string[] Variants(int from, int to) =>
        Enumerable.Range(from, to - from + 1).Select(n => $"variant{n:00}").ToArray();

    private static List<AvatarItemDefinition> Build()
    {
        var items = new List<AvatarItemDefinition>();

        void Add(AvatarSlot slot, string variantKey, string name, AvatarTier tier) =>
            items.Add(new AvatarItemDefinition(slot, variantKey, name, tier, PriceOf(tier), items.Count(i => i.Slot == slot)));

        static string Number(string variantKey) => variantKey["variant".Length..];

        // Mắt, lông mày, miệng, mũi: miễn phí toàn bộ.
        foreach (var v in Variants(1, 5)) Add(AvatarSlot.Eyes, v, $"Mắt {Number(v)}", AvatarTier.Free);
        foreach (var v in Variants(1, 13)) Add(AvatarSlot.Eyebrows, v, $"Lông mày {Number(v)}", AvatarTier.Free);
        foreach (var v in Variants(1, 30)) Add(AvatarSlot.Mouth, v, $"Miệng {Number(v)}", AvatarTier.Free);
        foreach (var v in Variants(1, 20)) Add(AvatarSlot.Nose, v, $"Mũi {Number(v)}", AvatarTier.Free);

        // Tóc: 8 kiểu miễn phí + 3 khăn trùm đầu (trang phục tôn giáo, luôn miễn phí). Phần còn lại theo thứ tự chỉ số:
        // 24 Thường, 21 Hiếm, phần cuối (variant54..60) cùng "hat" là Đặc biệt.
        string[] freeHair = ["variant01", "variant02", "variant05", "variant08", "variant20", "variant23", "variant41", "variant45"];
        string[] headscarfNames = ["Khăn quấn đầu", "Khăn trùm đầu 1", "Khăn trùm đầu 2"];
        var paidHair = Variants(1, 60).Where(v => !freeHair.Contains(v)).ToList(); // 52 kiểu
        foreach (var v in Variants(1, 63))
        {
            var scarf = HeadscarfVariantKeys.ToList().IndexOf(v);
            if (scarf >= 0)
            {
                Add(AvatarSlot.Hair, v, headscarfNames[scarf], AvatarTier.Free);
                continue;
            }

            var tier = freeHair.Contains(v) ? AvatarTier.Free
                : paidHair.IndexOf(v) < 24 ? AvatarTier.Common
                : paidHair.IndexOf(v) < 45 ? AvatarTier.Rare
                : AvatarTier.Special;
            Add(AvatarSlot.Hair, v, $"Tóc {Number(v)}", tier);
        }

        Add(AvatarSlot.Hair, "hat", "Mũ", AvatarTier.Special);

        // Áo: 5 kiểu miễn phí; 20 kiểu bán chia theo thứ tự chỉ số: 12 Thường rồi 8 Hiếm.
        string[] freeClothes = ["variant01", "variant02", "variant07", "variant14", "variant23"];
        var paidClothes = Variants(1, 25).Where(v => !freeClothes.Contains(v)).ToList();
        foreach (var v in Variants(1, 25))
        {
            var tier = freeClothes.Contains(v) ? AvatarTier.Free
                : paidClothes.IndexOf(v) < 12 ? AvatarTier.Common
                : AvatarTier.Rare;
            Add(AvatarSlot.Clothes, v, $"Áo {Number(v)}", tier);
        }

        for (var n = 1; n <= 11; n++) Add(AvatarSlot.Glasses, $"variant{n:00}", $"Kính {n:00}", n <= 6 ? AvatarTier.Common : AvatarTier.Rare);
        for (var n = 1; n <= 12; n++) Add(AvatarSlot.Beard, $"variant{n:00}", $"Râu {n:00}", n <= 6 ? AvatarTier.Common : AvatarTier.Rare);

        // Cử chỉ: toàn bộ Hiếm. Tên khóa là chữ (theo file style), tên hiển thị tiếng Việt.
        (string Key, string Name)[] gestures =
        [
            ("hand", "Giơ tay"),
            ("handPhone", "Cầm điện thoại"),
            ("ok", "OK"),
            ("okLongArm", "OK tay dài"),
            ("point", "Chỉ tay"),
            ("pointLongArm", "Chỉ tay dài"),
            ("waveLongArm", "Vẫy tay dài"),
            ("waveLongArms", "Vẫy hai tay dài"),
            ("waveOkLongArms", "Vẫy tay và OK"),
            ("wavePointLongArms", "Vẫy tay và chỉ"),
        ];
        foreach (var (key, name) in gestures) Add(AvatarSlot.Gesture, key, name, AvatarTier.Rare);

        // Hình in trên áo: toàn bộ Đặc biệt.
        Add(AvatarSlot.ClothesGraphic, "electric", "Tia chớp", AvatarTier.Special);
        Add(AvatarSlot.ClothesGraphic, "galaxy", "Thiên hà", AvatarTier.Special);
        Add(AvatarSlot.ClothesGraphic, "saturn", "Sao Thổ", AvatarTier.Special);

        // Nền: màu phẳng; variantKey là mã hex chữ thường 6 ký tự, không có '#'.
        (string Hex, string Name, AvatarTier Tier)[] backgrounds =
        [
            ("d4d4d4", "Xám mặc định", AvatarTier.Free),
            ("b9c6d6", "Xanh sương", AvatarTier.Common),
            ("b8d4be", "Xanh rêu", AvatarTier.Common),
            ("e3d5b8", "Cát", AvatarTier.Common),
            ("e8c4c4", "Hồng bụi", AvatarTier.Common),
            ("cfc4e3", "Tím oải hương", AvatarTier.Common),
            ("a9d3dc", "Xanh biển", AvatarTier.Rare),
            ("ebc4a0", "Đất nung", AvatarTier.Rare),
        ];
        foreach (var (hex, name, tier) in backgrounds) Add(AvatarSlot.Background, hex, name, tier);

        return items;
    }
}
