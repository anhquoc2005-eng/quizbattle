using QuizBattle.Application.Common;
using QuizBattle.Application.Shop;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Tests.TestHelpers;

/// <summary>
/// Repo giả cho ShopService/AvatarService: dữ liệu do test nạp vào các danh sách. <see cref="PurchaseAsync{TResult}"/> làm đúng
/// vai trò của repo thật (nạp snapshot, gọi work, lưu phần service để lại), còn luật mua nằm ở service.
/// </summary>
public sealed class FakeAvatarRepository : IAvatarRepository
{
    public List<AvatarItem> Catalog { get; } = [];

    public List<User> Users { get; } = [];

    public List<UserAvatarItem> Ownerships { get; } = [];

    public List<CoinTransaction> CoinTransactions { get; } = [];

    public Task<IReadOnlyList<AvatarItem>> GetCatalogAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AvatarItem>>(Catalog.ToList());

    public Task<TResult> PurchaseAsync<TResult>(Guid userId, string itemKey, Func<PurchaseSnapshot, TResult> work, CancellationToken cancellationToken)
    {
        var user = Users.SingleOrDefault(u => u.Id == userId) ?? throw new NotFoundException("Không tìm thấy tài khoản.");
        var item = Catalog.SingleOrDefault(i => i.Key == itemKey);
        var snapshot = new PurchaseSnapshot
        {
            User = user,
            Item = item,
            AlreadyOwned = item is not null && Ownerships.Any(o => o.UserId == userId && o.ItemKey == itemKey),
        };

        var result = work(snapshot); // ném exception thì không lưu gì
        if (snapshot.NewOwnership is not null)
        {
            Ownerships.Add(snapshot.NewOwnership);
        }

        if (snapshot.NewCoinTransaction is not null)
        {
            CoinTransactions.Add(snapshot.NewCoinTransaction);
        }

        return Task.FromResult(result);
    }
}
