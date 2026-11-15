using QuizBattle.Application.Common;
using QuizBattle.Application.Shop;
using QuizBattle.Domain.Avatars;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Application.Avatars;

/// <summary>
/// Đọc và lưu nhân vật. Luật hợp lệ thuần ở <see cref="AvatarRules"/>; service thêm hai kiểm tra cần dữ liệu: món phải có trong
/// danh mục ở DB và món bán phải thuộc người dùng. Lỗi đầu tiên theo thứ tự ô cố định; một lỗi thì không lưu gì.
/// </summary>
public sealed class AvatarService(IAvatarRepository avatarRepository, TimeProvider timeProvider) : IAvatarService
{
    public async Task<MyAvatarDto> GetMyAvatarAsync(Guid userId, CancellationToken cancellationToken)
    {
        var saved = await avatarRepository.GetUserAvatarAsync(userId, cancellationToken);
        var owned = await avatarRepository.GetOwnedItemKeysAsync(userId, cancellationToken);
        var selection = saved is null ? DefaultAvatarGenerator.ForAccount(userId) : AvatarMapping.ToSelection(saved);
        return new MyAvatarDto(AvatarMapping.ToDto(selection), IsDefault: saved is null, owned);
    }

    public async Task<SavedAvatarDto> SaveMyAvatarAsync(Guid userId, SaveAvatarRequest request, CancellationToken cancellationToken)
    {
        var selection = AvatarMapping.ToSelection(request);

        if (AvatarRules.Validate(selection) is [var first, ..])
        {
            throw ToException(first);
        }

        var chosen = AvatarSlots.All
            .Where(slot => selection.Get(slot) is not null)
            .Select(slot => (Slot: slot, Key: AvatarMapping.ItemKey(slot, selection.Get(slot)!)))
            .ToList();
        var items = (await avatarRepository.GetItemsByKeysAsync(chosen.Select(c => c.Key).ToList(), cancellationToken))
            .ToDictionary(i => i.Key);
        var owned = (await avatarRepository.GetOwnedItemKeysAsync(userId, cancellationToken)).ToHashSet();

        foreach (var (slot, key) in chosen)
        {
            var field = AvatarSlots.ToKey(slot);
            if (!items.TryGetValue(key, out var item))
            {
                throw new RequestValidationException(field, "Kiểu này không có trong danh mục.", AvatarErrorCodes.UnknownVariant);
            }

            if (item.Tier != AvatarTier.Free && !owned.Contains(key))
            {
                throw new ConflictException("Bạn chưa sở hữu món này.", "item_not_owned", field);
            }
        }

        await avatarRepository.SaveUserAvatarAsync(AvatarMapping.ToEntity(userId, selection, timeProvider.GetUtcNow()), cancellationToken);
        return new SavedAvatarDto(AvatarMapping.ToDto(selection), IsDefault: false);
    }

    private static RequestValidationException ToException(AvatarRuleError error) => new(
        AvatarSlots.ToKey(error.Slot),
        error.Code switch
        {
            AvatarErrorCodes.ValidationFailed => "Ô này bắt buộc phải có món.",
            AvatarErrorCodes.UnknownVariant => "Kiểu này không có trong danh mục của ô.",
            AvatarErrorCodes.GraphicIncompatibleClothes => "Hình in chỉ dùng được với áo variant01 đến variant10.",
            _ => "Giá trị không hợp lệ.",
        },
        error.Code);
}
