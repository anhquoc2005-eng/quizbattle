using System.Text.Json.Serialization;

namespace QuizBattle.Application.Avatars;

/// <summary>
/// Thay toàn bộ nhân vật. Mọi ô khai báo nullable để thiếu ô bắt buộc trả lỗi mã "validation_failed" của service
/// (không phải lỗi bind mặc định); ô lạ trong JSON bị từ chối (400).
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SaveAvatarRequest(
    string? Eyes,
    string? Eyebrows,
    string? Mouth,
    string? Nose,
    string? Hair,
    string? Clothes,
    string? Glasses,
    string? Beard,
    string? Gesture,
    string? ClothesGraphic,
    string? Background);
