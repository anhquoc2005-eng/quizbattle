namespace QuizBattle.Domain.Entities;

/// <summary>Quyền sở hữu vĩnh viễn một món bán đã mua. Chỉ thêm, không xóa; mỗi (UserId, ItemKey) tối đa một dòng.</summary>
public sealed class UserAvatarItem
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public required string ItemKey { get; set; }

    public DateTimeOffset AcquiredAt { get; set; }
}
