using QuizBattle.Application.Shop;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Tests.TestHelpers;

/// <summary>Repo giả cho ShopService/AvatarService. Danh mục do test nạp vào <see cref="Catalog"/>.</summary>
public sealed class FakeAvatarRepository : IAvatarRepository
{
    public List<AvatarItem> Catalog { get; } = [];

    public Task<IReadOnlyList<AvatarItem>> GetCatalogAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AvatarItem>>(Catalog.ToList());
}
