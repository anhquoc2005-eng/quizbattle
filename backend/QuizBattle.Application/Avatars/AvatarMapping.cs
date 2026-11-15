using QuizBattle.Domain.Avatars;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Application.Avatars;

/// <summary>Chuyển qua lại giữa DTO, bộ chọn của Domain và bản ghi đã lưu. Khóa món đồ trong DB là "ô.variantKey".</summary>
internal static class AvatarMapping
{
    public static string ItemKey(AvatarSlot slot, string variantKey) => $"{AvatarSlots.ToKey(slot)}.{variantKey}";

    public static AvatarSelection ToSelection(SaveAvatarRequest r) => new(new Dictionary<AvatarSlot, string?>
    {
        [AvatarSlot.Eyes] = r.Eyes,
        [AvatarSlot.Eyebrows] = r.Eyebrows,
        [AvatarSlot.Mouth] = r.Mouth,
        [AvatarSlot.Nose] = r.Nose,
        [AvatarSlot.Hair] = r.Hair,
        [AvatarSlot.Clothes] = r.Clothes,
        [AvatarSlot.Glasses] = r.Glasses,
        [AvatarSlot.Beard] = r.Beard,
        [AvatarSlot.Gesture] = r.Gesture,
        [AvatarSlot.ClothesGraphic] = r.ClothesGraphic,
        [AvatarSlot.Background] = r.Background,
    });

    public static AvatarSelection ToSelection(UserAvatar a) => new(new Dictionary<AvatarSlot, string?>
    {
        [AvatarSlot.Eyes] = Variant(a.EyesItemKey),
        [AvatarSlot.Eyebrows] = Variant(a.EyebrowsItemKey),
        [AvatarSlot.Mouth] = Variant(a.MouthItemKey),
        [AvatarSlot.Nose] = Variant(a.NoseItemKey),
        [AvatarSlot.Hair] = Variant(a.HairItemKey),
        [AvatarSlot.Clothes] = Variant(a.ClothesItemKey),
        [AvatarSlot.Glasses] = Variant(a.GlassesItemKey),
        [AvatarSlot.Beard] = Variant(a.BeardItemKey),
        [AvatarSlot.Gesture] = Variant(a.GestureItemKey),
        [AvatarSlot.ClothesGraphic] = Variant(a.ClothesGraphicItemKey),
        [AvatarSlot.Background] = Variant(a.BackgroundItemKey),
    });

    /// <summary>Sáu ô bắt buộc phải có giá trị (nhân vật đã lưu và nhân vật mặc định luôn thỏa).</summary>
    public static AvatarDto ToDto(AvatarSelection s) => new(
        s.Get(AvatarSlot.Eyes)!,
        s.Get(AvatarSlot.Eyebrows)!,
        s.Get(AvatarSlot.Mouth)!,
        s.Get(AvatarSlot.Nose)!,
        s.Get(AvatarSlot.Hair)!,
        s.Get(AvatarSlot.Clothes)!,
        s.Get(AvatarSlot.Glasses),
        s.Get(AvatarSlot.Beard),
        s.Get(AvatarSlot.Gesture),
        s.Get(AvatarSlot.ClothesGraphic),
        s.Get(AvatarSlot.Background));

    public static UserAvatar ToEntity(Guid userId, AvatarSelection s, DateTimeOffset now)
    {
        string Key(AvatarSlot slot) => ItemKey(slot, s.Get(slot)!);
        string? OptionalKey(AvatarSlot slot) => s.Get(slot) is { } v ? ItemKey(slot, v) : null;

        return new UserAvatar
        {
            UserId = userId,
            EyesItemKey = Key(AvatarSlot.Eyes),
            EyebrowsItemKey = Key(AvatarSlot.Eyebrows),
            MouthItemKey = Key(AvatarSlot.Mouth),
            NoseItemKey = Key(AvatarSlot.Nose),
            HairItemKey = Key(AvatarSlot.Hair),
            ClothesItemKey = Key(AvatarSlot.Clothes),
            GlassesItemKey = OptionalKey(AvatarSlot.Glasses),
            BeardItemKey = OptionalKey(AvatarSlot.Beard),
            GestureItemKey = OptionalKey(AvatarSlot.Gesture),
            ClothesGraphicItemKey = OptionalKey(AvatarSlot.ClothesGraphic),
            BackgroundItemKey = OptionalKey(AvatarSlot.Background),
            UpdatedAt = now,
        };
    }

    private static string? Variant(string? itemKey) => itemKey is null ? null : itemKey[(itemKey.IndexOf('.') + 1)..];
}
