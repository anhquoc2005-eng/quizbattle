using QuizBattle.Application.Common;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Application.Rooms;

public sealed class RoomService(
    IRoomRepository roomRepository,
    IRoomCodeGenerator roomCodeGenerator,
    TimeProvider timeProvider) : IRoomService
{
    private const int DefaultQuestionTimeLimitSeconds = 20;
    private const int MaxCodeAttempts = 5;

    public async Task<RoomResponse> CreateRoomAsync(Guid hostUserId, CreateRoomRequest request, CancellationToken cancellationToken)
    {
        var topic = request.Topic.Trim();
        var questionTimeLimitSeconds = request.QuestionTimeLimitSeconds ?? DefaultQuestionTimeLimitSeconds;

        for (var attempt = 1; attempt <= MaxCodeAttempts; attempt++)
        {
            var room = new Room
            {
                Id = Guid.NewGuid(),
                Code = roomCodeGenerator.Generate(),
                HostUserId = hostUserId,
                Topic = topic,
                Status = RoomStatus.Draft,
                QuestionTimeLimitSeconds = questionTimeLimitSeconds,
                CreatedAt = timeProvider.GetUtcNow(),
            };

            try
            {
                await roomRepository.AddAsync(room, cancellationToken);
                return ToResponse(room, questionCount: 0, playerCount: 0);
            }
            catch (RoomCodeCollisionException) when (attempt < MaxCodeAttempts)
            {
                // Mã trùng: thử lại với mã khác.
            }
        }

        // Hết số lần thử: để lọt ra GlobalExceptionHandler (500 + log), không tự log ở đây.
        throw new RoomCodeCollisionException();
    }

    public Task<IReadOnlyList<RoomResponse>> GetRoomsAsync(Guid hostUserId, CancellationToken cancellationToken) =>
        roomRepository.ListForUserAsync(hostUserId, cancellationToken);

    public async Task<RoomResponse> GetRoomAsync(Guid hostUserId, string code, CancellationToken cancellationToken)
    {
        var room = await roomRepository.FindSummaryByCodeForUserAsync(hostUserId, code.ToUpperInvariant(), cancellationToken);
        return room ?? throw new NotFoundException("Không tìm thấy phòng.");
    }

    public async Task DeleteRoomAsync(Guid hostUserId, string code, CancellationToken cancellationToken)
    {
        var normalizedCode = code.ToUpperInvariant();
        var deletedCount = await roomRepository.DeleteDeletableAsync(hostUserId, normalizedCode, cancellationToken);
        if (deletedCount > 0)
        {
            return;
        }

        // Không xóa được dòng nào: đọc lại để phân biệt "không tồn tại/không phải của mình" (404)
        // với "có nhưng đang Lobby/InProgress" (409).
        var room = await roomRepository.FindSummaryByCodeForUserAsync(hostUserId, normalizedCode, cancellationToken);
        throw room is null
            ? new NotFoundException("Không tìm thấy phòng.")
            : new ConflictException("Chỉ xóa được phòng ở trạng thái Draft hoặc Finished.");
    }

    private static RoomResponse ToResponse(Room room, int questionCount, int playerCount) => new(
        room.Id,
        room.Code,
        room.Topic,
        room.Status.ToString(),
        room.QuestionTimeLimitSeconds,
        room.CreatedAt,
        questionCount,
        playerCount);
}
