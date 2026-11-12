using QuizBattle.Domain.Enums;

namespace QuizBattle.Domain.Avatars;

/// <summary>Nhân vật: mỗi ô một variantKey hoặc null (ô trống). Ô không có trong bản đồ coi như trống.</summary>
public sealed record AvatarSelection(IReadOnlyDictionary<AvatarSlot, string?> Slots)
{
    public static AvatarSelection Empty { get; } = new(new Dictionary<AvatarSlot, string?>());

    public string? Get(AvatarSlot slot) => Slots.GetValueOrDefault(slot);

    public AvatarSelection With(AvatarSlot slot, string? variantKey)
    {
        var copy = new Dictionary<AvatarSlot, string?>(Slots) { [slot] = variantKey };
        return new AvatarSelection(copy);
    }
}
