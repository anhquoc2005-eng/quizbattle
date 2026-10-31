using Microsoft.EntityFrameworkCore;
using Npgsql;
using QuizBattle.Application.Common;
using QuizBattle.Application.Rooms;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Infrastructure.Persistence;

public sealed class RoomRepository(AppDbContext db) : IRoomRepository
{
    public async Task AddAsync(Room room, CancellationToken cancellationToken)
    {
        db.Rooms.Add(room);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_Rooms_Code" })
        {
            // room vẫn còn được theo dõi ở trạng thái Added: gỡ ra, nếu không lần retry sau sẽ cố insert
            // lại chính entity này (lại trùng mã cũ) cùng với entity mới, lỗi lặp vô hạn.
            db.Entry(room).State = EntityState.Detached;
            throw new RoomCodeCollisionException();
        }
    }

    public async Task<IReadOnlyList<RoomResponse>> ListForUserAsync(Guid hostUserId, CancellationToken cancellationToken)
    {
        var rows = await db.Rooms.AsNoTracking()
            .Where(r => r.HostUserId == hostUserId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.Code,
                r.Topic,
                r.Status,
                r.QuestionTimeLimitSeconds,
                r.CreatedAt,
                QuestionCount = r.Questions.Count,
                PlayerCount = r.Players.Count,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new RoomResponse(r.Id, r.Code, r.Topic, r.Status.ToString(), r.QuestionTimeLimitSeconds, r.CreatedAt, r.QuestionCount, r.PlayerCount))
            .ToList();
    }

    public async Task<RoomResponse?> FindSummaryByCodeForUserAsync(Guid hostUserId, string code, CancellationToken cancellationToken)
    {
        var row = await db.Rooms.AsNoTracking()
            .Where(r => r.HostUserId == hostUserId && r.Code == code)
            .Select(r => new
            {
                r.Id,
                r.Code,
                r.Topic,
                r.Status,
                r.QuestionTimeLimitSeconds,
                r.CreatedAt,
                QuestionCount = r.Questions.Count,
                PlayerCount = r.Players.Count,
            })
            .SingleOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new RoomResponse(row.Id, row.Code, row.Topic, row.Status.ToString(), row.QuestionTimeLimitSeconds, row.CreatedAt, row.QuestionCount, row.PlayerCount);
    }

    public Task<Room?> FindEntityByCodeForUserAsync(Guid hostUserId, string code, CancellationToken cancellationToken) =>
        db.Rooms.AsNoTracking().SingleOrDefaultAsync(r => r.HostUserId == hostUserId && r.Code == code, cancellationToken);

    public Task<int> DeleteDeletableAsync(Guid hostUserId, string code, CancellationToken cancellationToken) =>
        db.Rooms
            .Where(r => r.HostUserId == hostUserId && r.Code == code && (r.Status == RoomStatus.Draft || r.Status == RoomStatus.Finished))
            .ExecuteDeleteAsync(cancellationToken);
}
