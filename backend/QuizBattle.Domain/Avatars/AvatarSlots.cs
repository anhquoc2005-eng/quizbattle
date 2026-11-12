using QuizBattle.Domain.Enums;

namespace QuizBattle.Domain.Avatars;

/// <summary>Tên chuỗi của từng ô (camelCase, trùng tên thành phần của DiceBear) dùng ở khóa món đồ, JSON và hiển thị.</summary>
public static class AvatarSlots
{
    public static IReadOnlyList<AvatarSlot> All { get; } = Enum.GetValues<AvatarSlot>();

    /// <summary>Sáu ô luôn phải có món.</summary>
    public static IReadOnlyList<AvatarSlot> Required { get; } =
        [AvatarSlot.Eyes, AvatarSlot.Eyebrows, AvatarSlot.Mouth, AvatarSlot.Nose, AvatarSlot.Hair, AvatarSlot.Clothes];

    public static bool IsRequired(AvatarSlot slot) => Required.Contains(slot);

    public static string ToKey(AvatarSlot slot) => slot switch
    {
        AvatarSlot.Eyes => "eyes",
        AvatarSlot.Eyebrows => "eyebrows",
        AvatarSlot.Mouth => "mouth",
        AvatarSlot.Nose => "nose",
        AvatarSlot.Hair => "hair",
        AvatarSlot.Clothes => "clothes",
        AvatarSlot.Glasses => "glasses",
        AvatarSlot.Beard => "beard",
        AvatarSlot.Gesture => "gesture",
        AvatarSlot.ClothesGraphic => "clothesGraphic",
        AvatarSlot.Background => "background",
        _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null),
    };

    public static bool TryParse(string? key, out AvatarSlot slot)
    {
        foreach (var candidate in All)
        {
            if (ToKey(candidate) == key)
            {
                slot = candidate;
                return true;
            }
        }

        slot = default;
        return false;
    }
}
