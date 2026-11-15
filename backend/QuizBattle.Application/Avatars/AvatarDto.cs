namespace QuizBattle.Application.Avatars;

/// <summary>
/// Nhân vật dạng ô → variantKey. Sáu ô bắt buộc luôn có giá trị; năm ô tùy chọn là null khi trống.
/// Tên thuộc tính camelCase trùng tên ô trong hợp đồng (eyes, ..., clothesGraphic, background).
/// </summary>
public sealed record AvatarDto(
    string Eyes,
    string Eyebrows,
    string Mouth,
    string Nose,
    string Hair,
    string Clothes,
    string? Glasses,
    string? Beard,
    string? Gesture,
    string? ClothesGraphic,
    string? Background);
