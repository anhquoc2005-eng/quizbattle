using System.Text.RegularExpressions;
using QuizBattle.Application.Common;
using QuizBattle.Domain.Avatars;

namespace QuizBattle.Application.Shop;

public sealed partial class ShopService(IAvatarRepository avatarRepository, TimeProvider timeProvider) : IShopService
{
    private const int MaxItemKeyLength = 80;

    public async Task<IReadOnlyList<AvatarItemDto>> GetCatalogAsync(CancellationToken cancellationToken)
    {
        var items = await avatarRepository.GetCatalogAsync(cancellationToken);
        return items
            .OrderBy(i => i.Slot)
            .ThenBy(i => i.SortOrder)
            .Select(i => new AvatarItemDto(i.Key, AvatarSlots.ToKey(i.Slot), i.VariantKey, i.Name, i.Tier.ToString().ToLowerInvariant(), i.Price))
            .ToList();
    }

    public Task<PurchaseResultDto> PurchaseAsync(Guid userId, string? itemKey, CancellationToken cancellationToken)
    {
        if (itemKey is null || itemKey.Length > MaxItemKeyLength || !ItemKeyFormat().IsMatch(itemKey))
        {
            throw new RequestValidationException("itemKey", "Mã món đồ không hợp lệ (dạng \"ô.kiểu\", ví dụ hair.variant12).", "validation_failed");
        }

        var now = timeProvider.GetUtcNow();
        return avatarRepository.PurchaseAsync(userId, itemKey, snapshot => PurchaseSettlement.Settle(snapshot, now), cancellationToken);
    }

    [GeneratedRegex(@"^[A-Za-z]+\.[A-Za-z0-9]+$")]
    private static partial Regex ItemKeyFormat();
}
