namespace QuizBattle.Application.Shop;

public interface IShopService
{
    Task<IReadOnlyList<AvatarItemDto>> GetCatalogAsync(CancellationToken cancellationToken);
}
