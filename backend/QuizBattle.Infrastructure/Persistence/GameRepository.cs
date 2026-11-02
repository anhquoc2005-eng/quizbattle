using Microsoft.EntityFrameworkCore;
using Npgsql;
using QuizBattle.Application.Common;
using QuizBattle.Application.Game;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Infrastructure.Persistence;

public sealed class GameRepository(AppDbContext db) : IGameRepository
{
    public async Task<GameSnapshot?> FindSnapshotByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var room = await db.Rooms.AsNoTracking().SingleOrDefaultAsync(r => r.Code == code, cancellationToken);
        return room is null ? null : await LoadSnapshotAsync(room, cancellationToken);
    }

    public async Task<Player?> FindPlayerByTokenHashAsync(string sessionTokenHash, CancellationToken cancellationToken) =>
        await db.Players.AsNoTracking().SingleOrDefaultAsync(p => p.SessionTokenHash == sessionTokenHash, cancellationToken);

    public async Task<TResult> WithLockedRoomAsync<TResult>(string code, Func<GameSnapshot, TResult> work, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // SELECT ... FOR UPDATE: khóa độc quyền dòng Room - chặn 2 lệnh open/start/end/next cùng lúc trên
        // cùng 1 phòng (không làm nhảy 2 câu), và phải đợi mọi SubmitAnswer đang giữ FOR SHARE (đang lưu
        // dở) trên dòng này commit xong mới lấy được khóa - đảm bảo không có câu trả lời "lọt" vào sau khi
        // đã tính xong QuestionEnded/LeaderboardUpdated để gửi đi.
        var room = await db.Rooms
            .FromSqlInterpolated($"SELECT * FROM \"Rooms\" WHERE \"Code\" = {code} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy phòng.");

        var snapshot = await LoadSnapshotAsync(room, cancellationToken);
        var result = work(snapshot); // ném exception -> rollback khi transaction Dispose, không lưu gì

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<Player> AddPlayerAsync(string code, Func<GameSnapshot, Player> buildPlayer, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var room = await db.Rooms
            .FromSqlInterpolated($"SELECT * FROM \"Rooms\" WHERE \"Code\" = {code} FOR UPDATE")
            .AsNoTracking() // không sửa Room khi thêm người chơi, chỉ cần khóa để đếm/kiểm tra cho đúng
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy phòng.");

        var snapshot = await LoadSnapshotAsync(room, cancellationToken);
        var player = buildPlayer(snapshot); // ném exception -> rollback, không insert gì

        db.Players.Add(player);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return player;
    }

    public async Task<TResult> SubmitAnswerAsync<TResult>(
        Guid playerId,
        Func<SubmitAnswerSnapshot, (Answer Answer, TResult Result)> work,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var player = await db.Players.SingleOrDefaultAsync(p => p.Id == playerId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy người chơi.");

        // SELECT ... FOR SHARE: nhiều người trả lời cùng lúc giữ khóa chia sẻ này đồng thời, không xếp
        // hàng chờ nhau (khác FOR UPDATE độc quyền của WithLockedRoomAsync - xem giải thích ở đó).
        var room = await db.Rooms
            .FromSqlInterpolated($"SELECT * FROM \"Rooms\" WHERE \"Id\" = {player.RoomId} FOR SHARE")
            .AsNoTracking()
            .SingleAsync(cancellationToken);

        var question = await db.Questions.AsNoTracking()
            .SingleOrDefaultAsync(q => q.RoomId == room.Id && q.StartedAt != null && q.EndedAt == null, cancellationToken);

        var answeredCount = question is null ? 0 : await db.Answers.CountAsync(a => a.QuestionId == question.Id, cancellationToken);
        var totalPlayers = await db.Players.CountAsync(p => p.RoomId == room.Id, cancellationToken);

        var snapshot = new SubmitAnswerSnapshot
        {
            Player = player,
            Room = room,
            Question = question,
            AnsweredCountForCurrentQuestion = answeredCount,
            TotalPlayers = totalPlayers,
        };

        var (answer, result) = work(snapshot); // ném exception -> rollback, không insert gì

        db.Answers.Add(answer);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_Answers_PlayerId_QuestionId" })
        {
            throw new ConflictException("Bạn đã trả lời câu này.");
        }

        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private async Task<GameSnapshot> LoadSnapshotAsync(Room room, CancellationToken cancellationToken)
    {
        var questions = await db.Questions
            .Where(q => q.RoomId == room.Id)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync(cancellationToken);
        var approvedQuestions = questions.Where(q => q.ReviewStatus == QuestionReviewStatus.Approved).ToList();

        var players = await db.Players.Where(p => p.RoomId == room.Id).ToListAsync(cancellationToken);
        var answers = await db.Answers.Where(a => a.Player!.RoomId == room.Id).ToListAsync(cancellationToken);

        var current = approvedQuestions
            .Where(q => q.StartedAt is not null)
            .OrderByDescending(q => q.StartedAt)
            .FirstOrDefault();
        var answeredCount = current is null ? 0 : answers.Count(a => a.QuestionId == current.Id);

        return new GameSnapshot
        {
            Room = room,
            ApprovedQuestions = approvedQuestions,
            Players = players,
            Answers = answers,
            TotalQuestionCount = questions.Count,
            AnsweredCountForCurrentQuestion = answeredCount,
        };
    }
}
