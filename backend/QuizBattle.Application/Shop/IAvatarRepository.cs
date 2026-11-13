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
}
