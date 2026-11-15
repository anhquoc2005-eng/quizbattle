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

    /// <summary>Nhân vật đã lưu theo UserId (không có khóa = chưa từng lưu).</summary>
    public Dictionary<Guid, UserAvatar> UserAvatars { get; } = [];

    public int SaveCalls { get; private set; }

    /// <summary>Nạp toàn bộ danh mục thật (201 món) vào <see cref="Catalog"/>.</summary>
    public FakeAvatarRepository WithFullCatalog()
    {
        Catalog.AddRange(QuizBattle.Domain.Avatars.AvatarCatalog.Items.Select(d => new AvatarItem
        {
            Key = d.Key, Slot = d.Slot, VariantKey = d.VariantKey, Name = d.Name, Tier = d.Tier, Price = d.Price, SortOrder = d.SortOrder,
        }));
        return this;
    }

    public Task<IReadOnlyList<AvatarItem>> GetCatalogAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AvatarItem>>(Catalog.ToList());

    public Task<UserAvatar?> GetUserAvatarAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(UserAvatars.GetValueOrDefault(userId));

    public Task<IReadOnlyList<string>> GetOwnedItemKeysAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<string>>(Ownerships.Where(o => o.UserId == userId).Select(o => o.ItemKey).Order().ToList());

    public Task<IReadOnlyList<AvatarItem>> GetItemsByKeysAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AvatarItem>>(Catalog.Where(i => keys.Contains(i.Key)).ToList());

    public Task SaveUserAvatarAsync(UserAvatar avatar, CancellationToken cancellationToken)
    {
        SaveCalls++;
        UserAvatars[avatar.UserId] = avatar;
        return Task.CompletedTask;
    }

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
