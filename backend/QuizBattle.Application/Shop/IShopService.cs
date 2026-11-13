namespace QuizBattle.Application.Shop;

public interface IShopService
{
    Task<IReadOnlyList<AvatarItemDto>> GetCatalogAsync(CancellationToken cancellationToken);

    Task<PurchaseResultDto> PurchaseAsync(Guid userId, string? itemKey, CancellationToken cancellationToken);
}
