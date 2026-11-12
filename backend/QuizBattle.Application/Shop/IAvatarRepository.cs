using QuizBattle.Domain.Entities;

namespace QuizBattle.Application.Shop;

public interface IAvatarRepository
{
    /// <summary>Toàn bộ danh mục món đồ, sắp theo ô (thứ tự khai báo) rồi SortOrder. Danh mục bất biến nên đọc không khóa.</summary>
    Task<IReadOnlyList<AvatarItem>> GetCatalogAsync(CancellationToken cancellationToken);
}
