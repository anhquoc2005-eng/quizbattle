namespace QuizBattle.Domain.Entities;

/// <summary>
/// Nhân vật đã lưu của một tài khoản (1-1 với User). Mỗi ô lưu khóa món đồ ("ô.variantKey"). Tài khoản chưa từng lưu
/// không có dòng và nhân vật được tự sinh bằng DefaultAvatarGenerator. Sáu ô bắt buộc không null; năm ô tùy chọn null = trống.
/// </summary>
public sealed class UserAvatar
{
    public Guid UserId { get; set; }

    public required string EyesItemKey { get; set; }

    public required string EyebrowsItemKey { get; set; }

    public required string MouthItemKey { get; set; }

    public required string NoseItemKey { get; set; }

    public required string HairItemKey { get; set; }

    public required string ClothesItemKey { get; set; }

    public string? GlassesItemKey { get; set; }

    public string? BeardItemKey { get; set; }

    public string? GestureItemKey { get; set; }

    public string? ClothesGraphicItemKey { get; set; }

    public string? BackgroundItemKey { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
