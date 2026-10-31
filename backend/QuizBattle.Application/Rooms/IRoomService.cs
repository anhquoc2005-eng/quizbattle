namespace QuizBattle.Application.Rooms;

public interface IRoomService
{
    Task<RoomResponse> CreateRoomAsync(Guid hostUserId, CreateRoomRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<RoomResponse>> GetRoomsAsync(Guid hostUserId, CancellationToken cancellationToken);

    Task<RoomResponse> GetRoomAsync(Guid hostUserId, string code, CancellationToken cancellationToken);

    Task DeleteRoomAsync(Guid hostUserId, string code, CancellationToken cancellationToken);
}
