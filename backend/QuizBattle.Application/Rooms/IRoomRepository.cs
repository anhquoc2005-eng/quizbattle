using QuizBattle.Domain.Entities;

namespace QuizBattle.Application.Rooms;

public interface IRoomRepository
{
    /// <summary>Lưu phòng mới. Ném RoomCodeCollisionException nếu Code đã tồn tại (IX_Rooms_Code).</summary>
    Task AddAsync(Room room, CancellationToken cancellationToken);

    Task<IReadOnlyList<RoomResponse>> ListForUserAsync(Guid hostUserId, CancellationToken cancellationToken);

    Task<RoomResponse?> FindSummaryByCodeForUserAsync(Guid hostUserId, string code, CancellationToken cancellationToken);

    /// <summary>Lấy entity Room (không phải DTO) để Service đọc Id/Status/Topic khi xử lý câu hỏi.</summary>
    Task<Room?> FindEntityByCodeForUserAsync(Guid hostUserId, string code, CancellationToken cancellationToken);

    /// <summary>Xóa nếu thuộc đúng user, đúng mã và đang Draft/Finished. Trả số dòng đã xóa (0 hoặc 1).</summary>
    Task<int> DeleteDeletableAsync(Guid hostUserId, string code, CancellationToken cancellationToken);
}
