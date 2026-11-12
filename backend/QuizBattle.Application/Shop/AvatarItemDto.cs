namespace QuizBattle.Application.Shop;

/// <summary>Một món trong danh mục. Slot viết camelCase ("hair"), Tier viết thường ("free|common|rare|special"), Price là số Xu server quy định.</summary>
public sealed record AvatarItemDto(string Key, string Slot, string VariantKey, string Name, string Tier, int Price);
