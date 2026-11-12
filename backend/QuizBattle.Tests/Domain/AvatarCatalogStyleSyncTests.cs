using System.Text.Json;
using QuizBattle.Domain.Avatars;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Tests.Domain;

/// <summary>
/// Danh mục phải khớp từng variantKey với file định nghĩa style Notionists của đúng phiên bản @dicebear/styles đã ghim.
/// Đọc thẳng từ frontend/node_modules. Thiếu node_modules thì FAIL (chạy `npm ci` trong frontend); chỉ bỏ qua khi
/// đặt QB_SKIP_STYLE_SYNC=1 (ngoại lệ có chủ đích, ví dụ môi trường chỉ build backend).
/// </summary>
public sealed class AvatarCatalogStyleSyncTests
{
    private const string SkipVariable = "QB_SKIP_STYLE_SYNC";

    private static readonly Lazy<(string Version, JsonElement Components)?> Style = new(Load);

    private static (string Version, JsonElement Components)? Load()
    {
        var root = AppContext.BaseDirectory;
        string? stylesDir = null;
        for (var dir = new DirectoryInfo(root); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "frontend", "node_modules", "@dicebear", "styles");
            if (Directory.Exists(candidate))
            {
                stylesDir = candidate;
                break;
            }
        }

        if (stylesDir is null)
        {
            if (Environment.GetEnvironmentVariable(SkipVariable) == "1")
            {
                Console.Error.WriteLine($"CẢNH BÁO: bỏ qua test đồng bộ danh mục với file style vì {SkipVariable}=1 và không thấy frontend/node_modules.");
                return null;
            }

            throw new InvalidOperationException(
                "Không thấy frontend/node_modules/@dicebear/styles. Chạy `npm ci` trong thư mục frontend rồi chạy lại test " +
                $"(hoặc đặt {SkipVariable}=1 nếu cố ý bỏ qua).");
        }

        using var package = JsonDocument.Parse(File.ReadAllText(Path.Combine(stylesDir, "package.json")));
        var version = package.RootElement.GetProperty("version").GetString()!;

        // JsonDocument bị giải phóng khi thoát using, nên Clone để dùng lại phần components.
        var style = JsonDocument.Parse(File.ReadAllText(Path.Combine(stylesDir, "dist", "notionists.min.json")));
        return (version, style.RootElement.GetProperty("components").Clone());
    }

    private static (string Version, JsonElement Components)? Require() => Style.Value;

    [Fact]
    public void StyleVersion_MatchesInstalledPackage()
    {
        if (Require() is not { } style)
        {
            return;
        }

        Assert.Equal("10.6.0", AvatarCatalog.StyleVersion);
        Assert.Equal(AvatarCatalog.StyleVersion, style.Version);
    }

    [Fact]
    public void EveryDiceBearSlot_VariantKeysEqualStyleVariants_BothDirections()
    {
        if (Require() is not { } style)
        {
            return;
        }

        foreach (var slot in AvatarSlots.All.Where(s => s != AvatarSlot.Background))
        {
            var name = AvatarSlots.ToKey(slot);
            var inStyle = style.Components.GetProperty(name).GetProperty("variants").EnumerateObject().Select(p => p.Name).Order().ToArray();
            var inCatalog = AvatarCatalog.Items.Where(i => i.Slot == slot).Select(i => i.VariantKey).Order().ToArray();

            Assert.True(
                inStyle.SequenceEqual(inCatalog),
                $"Ô {name}: thiếu trong danh mục [{string.Join(", ", inStyle.Except(inCatalog))}], thừa so với style [{string.Join(", ", inCatalog.Except(inStyle))}]");
        }
    }

    [Fact]
    public void SlotNames_ExistInStyleComponents()
    {
        if (Require() is not { } style)
        {
            return;
        }

        var components = style.Components.EnumerateObject().Select(p => p.Name).ToHashSet();
        foreach (var slot in AvatarSlots.All.Where(s => s != AvatarSlot.Background))
        {
            Assert.Contains(AvatarSlots.ToKey(slot), components);
        }
    }

    [Fact]
    public void PrintableClothes_AreSubsetOfStyleClothes()
    {
        if (Require() is not { } style)
        {
            return;
        }

        var clothes = style.Components.GetProperty("clothes").GetProperty("variants").EnumerateObject().Select(p => p.Name).ToHashSet();

        Assert.All(AvatarCatalog.PrintableClothesVariantKeys, key => Assert.Contains(key, clothes));
    }

    [Fact]
    public void Backgrounds_VariantKeysAreLowercase6Hex_AndNotPartOfStyle()
    {
        // Ô nền là của dự án (không thuộc DiceBear) nên không đối chiếu với file style; chỉ kiểm định dạng.
        Assert.All(
            AvatarCatalog.Items.Where(i => i.Slot == AvatarSlot.Background),
            i => Assert.Matches("^[0-9a-f]{6}$", i.VariantKey));
    }
}
