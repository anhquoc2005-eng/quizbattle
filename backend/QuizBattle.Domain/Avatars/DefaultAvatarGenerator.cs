using System.Text;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Domain.Avatars;

/// <summary>
/// Nhân vật mặc định cho tài khoản chưa từng chỉnh và cho khách - hàm thuần, cố định theo đầu vào, chỉ dùng món miễn phí
/// và không bao giờ chọn khăn trùm đầu (xem <see cref="AvatarCatalog.DefaultFreeItems"/>).
/// Dùng FNV-1a 64 bit + SplitMix64 (KHÔNG dùng string.GetHashCode: giá trị đó đổi giữa các lần chạy process).
/// Đổi thuật toán hoặc danh sách món miễn phí sẽ đổi hình mặc định của mọi người; test giá trị cố định chặn việc này.
/// </summary>
public static class DefaultAvatarGenerator
{
    private const ulong FnvOffset = 14695981039346656037UL;
    private const ulong FnvPrime = 1099511628211UL;
    private const ulong GoldenGamma = 0x9E3779B97F4A7C15UL;

    public static AvatarSelection ForAccount(Guid userId) => Generate("u:" + userId.ToString("N"));

    public static AvatarSelection ForGuest(string displayName) => Generate("g:" + displayName.Trim().ToLowerInvariant());

    private static AvatarSelection Generate(string key)
    {
        var hash = Fnv1a64(Encoding.UTF8.GetBytes(key));
        var slots = new Dictionary<AvatarSlot, string?>();

        for (var i = 0; i < AvatarSlots.Required.Count; i++)
        {
            var slot = AvatarSlots.Required[i];
            var free = AvatarCatalog.DefaultFreeItems(slot);
            // Mỗi ô lấy một số riêng từ cùng hash để các ô không chọn tương quan với nhau.
            var pick = Mix(unchecked(hash + (ulong)(i + 1) * GoldenGamma)) % (ulong)free.Count;
            slots[slot] = free[(int)pick].VariantKey;
        }

        slots[AvatarSlot.Background] = AvatarCatalog.DefaultBackgroundHex;
        return new AvatarSelection(slots);
    }

    private static ulong Fnv1a64(byte[] data)
    {
        var hash = FnvOffset;
        foreach (var b in data)
        {
            hash = unchecked((hash ^ b) * FnvPrime);
        }

        return hash;
    }

    /// <summary>Bước trộn cuối của SplitMix64.</summary>
    private static ulong Mix(ulong z)
    {
        z = unchecked((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL);
        z = unchecked((z ^ (z >> 27)) * 0x94D049BB133111EBUL);
        return z ^ (z >> 31);
    }
}
