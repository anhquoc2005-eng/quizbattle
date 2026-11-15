using QuizBattle.Domain.Entities;

namespace QuizBattle.Application.Shop;

public interface IAvatarRepository
{
    /// <summary>Toàn bộ danh mục món đồ, sắp theo ô (thứ tự khai báo) rồi SortOrder. Danh mục bất biến nên đọc không khóa.</summary>
    Task<IReadOnlyList<AvatarItem>> GetCatalogAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Mở transaction, khóa dòng Users của <paramref name="userId"/> bằng SELECT ... FOR UPDATE (theo quy tắc khóa Users của
    /// CLAUDE.md), nạp món đồ và trạng thái sở hữu vào <see cref="PurchaseSnapshot"/>, gọi <paramref name="work"/> để áp luật mua
    /// (ném exception thì rollback, không lưu gì), rồi lưu mọi thay đổi trong cùng một SaveChanges và commit. Vi phạm unique
    /// (UserId, ItemKey) ánh xạ thành ConflictException "already_owned". Không có tài khoản thì ném NotFoundException.
    /// </summary>
    Task<TResult> PurchaseAsync<TResult>(Guid userId, string itemKey, Func<PurchaseSnapshot, TResult> work, CancellationToken cancellationToken);

    /// <summary>Nhân vật đã lưu của tài khoản; null nếu chưa từng lưu (khi đó nhân vật được tự sinh).</summary>
    Task<UserAvatar?> GetUserAvatarAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Khóa các món BÁN tài khoản đã mua (món miễn phí không có dòng sở hữu), sắp theo khóa.</summary>
    Task<IReadOnlyList<string>> GetOwnedItemKeysAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Các món có khóa nằm trong <paramref name="keys"/> (một truy vấn); khóa không có trong danh mục thì không xuất hiện.</summary>
    Task<IReadOnlyList<AvatarItem>> GetItemsByKeysAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken);

    /// <summary>
    /// Ghi đè nhân vật đã lưu (chèn nếu chưa có dòng, cập nhật nếu có). Hai lần lưu đầu tiên đồng thời cùng chèn thì bên thua
    /// (vi phạm khóa chính) được thử lại một lần dưới dạng cập nhật.
    /// </summary>
    Task SaveUserAvatarAsync(UserAvatar avatar, CancellationToken cancellationToken);
}
