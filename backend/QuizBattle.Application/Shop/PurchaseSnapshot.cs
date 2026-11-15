using QuizBattle.Domain.Entities;

namespace QuizBattle.Application.Shop;

/// <summary>
/// Trạng thái đọc được khi khóa dòng Users (SELECT ... FOR UPDATE) cho một lần mua. <see cref="User"/> là entity đang được
/// EF theo dõi: service sửa trực tiếp CoinBalance, rồi gán <see cref="NewOwnership"/> và <see cref="NewCoinTransaction"/>;
/// repository lưu cả ba trong cùng một SaveChanges (một transaction).
/// </summary>
public sealed class PurchaseSnapshot
{
    public required User User { get; init; }

    /// <summary>Món đồ theo khóa client gửi; null nếu không có trong danh mục.</summary>
    public AvatarItem? Item { get; init; }

    /// <summary>Người dùng đã sở hữu món này (chỉ có nghĩa khi <see cref="Item"/> không null).</summary>
    public bool AlreadyOwned { get; init; }

    public UserAvatarItem? NewOwnership { get; set; }

    public CoinTransaction? NewCoinTransaction { get; set; }
}
