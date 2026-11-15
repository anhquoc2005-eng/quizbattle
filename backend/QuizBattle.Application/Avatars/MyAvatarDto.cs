namespace QuizBattle.Application.Avatars;

/// <summary>IsDefault = tài khoản chưa từng lưu (nhân vật tự sinh). OwnedItemKeys chỉ gồm món bán đã mua, không có món miễn phí.</summary>
public sealed record MyAvatarDto(AvatarDto Avatar, bool IsDefault, IReadOnlyList<string> OwnedItemKeys);
