using QuizBattle.Domain.Enums;

namespace QuizBattle.Domain.Entities;

/// <summary>Một món đồ trong danh mục (bất biến, seed bằng migration từ AvatarCatalog). Key dạng "ô.variantKey".</summary>
public sealed class AvatarItem
{
    public required string Key { get; set; }

    public AvatarSlot Slot { get; set; }

    public required string VariantKey { get; set; }

    public required string Name { get; set; }

    public AvatarTier Tier { get; set; }

    /// <summary>Số Xu, do server quyết định; 0 khi miễn phí.</summary>
    public int Price { get; set; }

    public int SortOrder { get; set; }
}
