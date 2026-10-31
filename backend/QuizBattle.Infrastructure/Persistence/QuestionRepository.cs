using Microsoft.EntityFrameworkCore;
using QuizBattle.Application.Common;
using QuizBattle.Application.Questions;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Infrastructure.Persistence;

public sealed class QuestionRepository(AppDbContext db) : IQuestionRepository
{
    public async Task<IReadOnlyList<Question>> ListByRoomIdAsync(Guid roomId, CancellationToken cancellationToken) =>
        await db.Questions.AsNoTracking().Where(q => q.RoomId == roomId).ToListAsync(cancellationToken);

    public async Task<Question?> FindByIdAsync(Guid roomId, Guid questionId, CancellationToken cancellationToken) =>
        await db.Questions.SingleOrDefaultAsync(q => q.RoomId == roomId && q.Id == questionId, cancellationToken);

    public Task<int> CountByRoomIdAsync(Guid roomId, CancellationToken cancellationToken) =>
        db.Questions.CountAsync(q => q.RoomId == roomId, cancellationToken);

    public async Task<IReadOnlyList<string>> ListTextsByRoomIdAsync(Guid roomId, CancellationToken cancellationToken) =>
        await db.Questions.AsNoTracking().Where(q => q.RoomId == roomId).Select(q => q.Text).ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    public Task DeleteAsync(Question question, CancellationToken cancellationToken)
    {
        db.Questions.Remove(question);
        return db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Question>> InsertWithRoomLockAsync(
        Guid roomId,
        Func<RoomSnapshot, IReadOnlyList<Question>> buildQuestions,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // SELECT ... FOR UPDATE: khóa dòng Room tới khi transaction này commit/rollback - chặn request
        // khác đang thêm câu hỏi cho CÙNG phòng này cho tới lúc đó. Không có khóa này, 2 request chạy
        // song song có thể cùng đọc "còn 2 chỗ trống" rồi cùng insert, vượt giới hạn MAX_QUESTIONS_PER_ROOM
        // (TOCTOU - giống lỗi trùng email ở Tuần 2, chỉ check-rồi-act không đủ, phải khóa dòng liên quan).
        var lockedRoom = await db.Rooms
            .FromSqlInterpolated($"SELECT * FROM \"Rooms\" WHERE \"Id\" = {roomId} FOR UPDATE")
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy phòng.");

        var questionCount = await db.Questions.CountAsync(q => q.RoomId == roomId, cancellationToken);
        var existingTexts = await db.Questions.AsNoTracking()
            .Where(q => q.RoomId == roomId)
            .Select(q => q.Text)
            .ToListAsync(cancellationToken);
        var maxOrderIndex = await db.Questions
            .Where(q => q.RoomId == roomId)
            .Select(q => (int?)q.OrderIndex)
            .MaxAsync(cancellationToken);

        var snapshot = new RoomSnapshot(lockedRoom.Status, questionCount, existingTexts, (maxOrderIndex ?? -1) + 1);
        var toInsert = buildQuestions(snapshot); // ném exception -> transaction rollback khi Dispose, không insert gì

        for (var i = 0; i < toInsert.Count; i++)
        {
            toInsert[i].RoomId = roomId;
            toInsert[i].OrderIndex = snapshot.NextOrderIndex + i;
        }

        db.Questions.AddRange(toInsert);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return toInsert;
    }
}
