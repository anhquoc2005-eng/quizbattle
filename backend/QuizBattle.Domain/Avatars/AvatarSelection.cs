using QuizBattle.Domain.Enums;

namespace QuizBattle.Domain.Avatars;

/// <summary>
/// Nhân vật: mỗi ô một variantKey hoặc null (ô trống). Ô không có trong bản đồ coi như trống.
/// So sánh theo NỘI DUNG 11 ô (không theo tham chiếu của bản đồ), ô vắng mặt bằng ô null.
/// </summary>
public sealed record AvatarSelection(IReadOnlyDictionary<AvatarSlot, string?> Slots)
{
    public static AvatarSelection Empty { get; } = new(new Dictionary<AvatarSlot, string?>());

    public string? Get(AvatarSlot slot) => Slots.GetValueOrDefault(slot);

    public AvatarSelection With(AvatarSlot slot, string? variantKey)
    {
        var copy = new Dictionary<AvatarSlot, string?>(Slots) { [slot] = variantKey };
        return new AvatarSelection(copy);
    }

    public bool Equals(AvatarSelection? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return AvatarSlots.All.All(slot => string.Equals(Get(slot), other.Get(slot), StringComparison.Ordinal));
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var slot in AvatarSlots.All)
        {
            hash.Add(Get(slot), StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }
}
