using QuizBattle.Application.Common;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Application.Shop;

/// <summary>
/// Luật mua đồ, áp lên snapshot đã khóa (cùng mẫu với RewardSettlement của Tính năng 1): kiểm theo thứ tự cố định rồi trừ Xu,
/// tạo quyền sở hữu và dòng sổ cái. Repository chỉ lo transaction, khóa, nạp và lưu.
/// </summary>
internal static class PurchaseSettlement
{
    /// <summary>Thứ tự kiểm: 404 không có món → 400 món miễn phí → 409 đã sở hữu → 409 không đủ Xu.</summary>
    public static PurchaseResultDto Settle(PurchaseSnapshot snapshot, DateTimeOffset now)
    {
        var item = snapshot.Item
            ?? throw new NotFoundException("Không tìm thấy món đồ.", "item_not_found");

        if (item.Tier == AvatarTier.Free)
        {
            throw new RequestValidationException("itemKey", "Món này miễn phí, không cần mua.", "item_not_purchasable");
        }

        if (snapshot.AlreadyOwned)
        {
            throw new ConflictException("Bạn đã sở hữu món này.", "already_owned");
        }

        var user = snapshot.User;
        if (user.CoinBalance < item.Price)
        {
            throw new ConflictException("Bạn không đủ Xu để mua món này.", "insufficient_coins");
        }

        var ownership = new UserAvatarItem { Id = Guid.NewGuid(), UserId = user.Id, ItemKey = item.Key, AcquiredAt = now };
        snapshot.NewOwnership = ownership;
        snapshot.NewCoinTransaction = new CoinTransaction
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Amount = -item.Price,
            Reason = CoinTransactionReason.ShopPurchase,
            UserAvatarItemId = ownership.Id,
            CreatedAt = now,
        };
        user.CoinBalance -= item.Price;

        return new PurchaseResultDto(item.Key, item.Price, user.CoinBalance);
    }
}
