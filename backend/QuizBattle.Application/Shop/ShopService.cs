using QuizBattle.Domain.Avatars;

namespace QuizBattle.Application.Shop;

public sealed class ShopService(IAvatarRepository avatarRepository) : IShopService
{
    public async Task<IReadOnlyList<AvatarItemDto>> GetCatalogAsync(CancellationToken cancellationToken)
    {
        var items = await avatarRepository.GetCatalogAsync(cancellationToken);
        return items
            .OrderBy(i => i.Slot)
            .ThenBy(i => i.SortOrder)
            .Select(i => new AvatarItemDto(i.Key, AvatarSlots.ToKey(i.Slot), i.VariantKey, i.Name, i.Tier.ToString().ToLowerInvariant(), i.Price))
            .ToList();
    }
}
