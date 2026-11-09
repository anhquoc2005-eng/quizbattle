using Microsoft.EntityFrameworkCore;
using Npgsql;
using QuizBattle.Application.Common;
using QuizBattle.Application.Game;
using QuizBattle.Domain;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Infrastructure.Persistence;

public sealed class GameRepository(AppDbContext db) : IGameRepository
{
    public async Task<GameSnapshot?> FindSnapshotByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var room = await db.Rooms.AsNoTracking().SingleOrDefaultAsync(r => r.Code == code, cancellationToken);
        return room is null ? null : await LoadSnapshotAsync(room, lockUsersIfFinalAdvance: false, cancellationToken);
    }

    public async Task<Player?> FindPlayerByTokenHashAsync(string sessionTokenHash, CancellationToken cancellationToken) =>
        await db.Players.AsNoTracking().SingleOrDefaultAsync(p => p.SessionTokenHash == sessionTokenHash, cancellationToken);

    public Task<TResult> WithLockedRoomAsync<TResult>(string code, Func<GameSnapshot, TResult> work, CancellationToken cancellationToken) =>
        LockRoomAsync($"SELECT * FROM \"Rooms\" WHERE \"Code\" = {code} FOR UPDATE", work, cancellationToken);

    public Task<TResult> WithLockedRoomByIdAsync<TResult>(Guid roomId, Func<GameSnapshot, TResult> work, CancellationToken cancellationToken) =>
        LockRoomAsync($"SELECT * FROM \"Rooms\" WHERE \"Id\" = {roomId} FOR UPDATE", work, cancellationToken);

    public async Task<IReadOnlyList<DueRoom>> FindDueRoomsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        // Projection + correlated subquery trong 1 câu SQL (không N+1). Phép cộng giây vào cột không dịch được
        // sang SQL ổn định, nên lọc thời gian ở bộ nhớ bằng GameTimeline - tập kết quả chỉ gồm phòng InProgress.
        var rooms = await db.Rooms.AsNoTracking()
            .Where(r => r.Status == RoomStatus.InProgress)
            .Select(r => new
            {
                r.Id,
                r.Code,
                r.QuestionTimeLimitSeconds,
                PlayerCount = r.Players.Count,
                Current = r.Questions
                    .Where(q => q.ReviewStatus == QuestionReviewStatus.Approved && q.StartedAt != null)
                    .OrderByDescending(q => q.StartedAt)
                    .Select(q => new { q.StartedAt, q.EndedAt, AnsweredCount = q.Answers.Count })
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        return rooms
            .Where(r => r.Current is { } c
                && (GameTimeline.ShouldEndQuestion(c.StartedAt!.Value, c.EndedAt, r.QuestionTimeLimitSeconds, c.AnsweredCount, r.PlayerCount, now)
                    || GameTimeline.ShouldAdvance(c.EndedAt, now)))
            .Select(r => new DueRoom(r.Id, r.Code))
            .ToList();
    }

    private async Task<TResult> LockRoomAsync<TResult>(FormattableString lockRoomSql, Func<GameSnapshot, TResult> work, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // SELECT ... FOR UPDATE: khóa độc quyền dòng Room - chặn 2 lệnh open/start/end/next cùng lúc trên
        // cùng 1 phòng (không làm nhảy 2 câu), và phải đợi mọi SubmitAnswer đang giữ FOR SHARE (đang lưu
        // dở) trên dòng này commit xong mới lấy được khóa - đảm bảo không có câu trả lời "lọt" vào sau khi
        // đã tính xong QuestionEnded/LeaderboardUpdated để gửi đi. Cũng là thứ chặn timer và chủ phòng (hoặc
        // 2 server) cùng kết thúc/chuyển một câu: người đến sau thấy EndedAt đã đổi nên không làm gì.
        var room = await db.Rooms
            .FromSqlInterpolated(lockRoomSql)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy phòng.");

        var snapshot = await LoadSnapshotAsync(room, lockUsersIfFinalAdvance: true, cancellationToken);
        var result = work(snapshot); // ném exception -> rollback khi transaction Dispose, không lưu gì

        // Thưởng cuối trận đi cùng SaveChanges (1 transaction) với Room.Status = Finished và cộng Users.
        db.MatchRewards.AddRange(snapshot.NewMatchRewards);
        db.CoinTransactions.AddRange(snapshot.NewCoinTransactions);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<Player> JoinPlayerAsync(string code, Guid? userId, Func<GameSnapshot, Player> buildOrReusePlayer, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var room = await db.Rooms
            .FromSqlInterpolated($"SELECT * FROM \"Rooms\" WHERE \"Code\" = {code} FOR UPDATE")
            .AsNoTracking() // không sửa Room khi thêm người chơi, chỉ cần khóa để đếm/kiểm tra cho đúng
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy phòng.");

        var snapshot = await LoadSnapshotAsync(room, lockUsersIfFinalAdvance: false, cancellationToken, extraUserId: userId);
        var player = buildOrReusePlayer(snapshot); // ném exception -> rollback, không lưu gì

        // Vào lại: Player đã được EF theo dõi (nạp trong snapshot), SaveChanges chỉ lưu phần đã sửa.
        if (db.Entry(player).State == EntityState.Detached)
        {
            db.Players.Add(player);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            // Đã khóa phòng nên hiếm khi tới đây; vẫn trả cùng lỗi như kiểm tra ở GameService.
            throw pg.ConstraintName switch
            {
                "IX_Players_RoomId_lower_DisplayName" => new ConflictException("Tên hiển thị đã có người dùng trong phòng."),
                "IX_Players_RoomId_UserId" => new ConflictException("Bạn đã ở trong phòng này."),
                _ => ex,
            };
        }

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

    private async Task<GameSnapshot> LoadSnapshotAsync(Room room, bool lockUsersIfFinalAdvance, CancellationToken cancellationToken, Guid? extraUserId = null)
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

        // Đúng điều kiện để lần AdvanceFrom kế tiếp rơi vào nhánh kết thúc trận.
        var isFinalAdvance = room.Status == RoomStatus.InProgress
            && approvedQuestions.LastOrDefault() is { StartedAt: not null, EndedAt: not null };

        // extraUserId: tài khoản đang vào phòng (chưa có Player) - để kiểm tra nó còn tồn tại.
        var userIds = players.Where(p => p.UserId is not null).Select(p => p.UserId!.Value)
            .Concat(extraUserId is { } extra ? [extra] : []).Distinct().ToArray();
        List<User> users;
        if (userIds.Length == 0)
        {
            users = [];
        }
        else if (isFinalAdvance && lockUsersIfFinalAdvance)
        {
            // Khóa dòng Users theo thứ tự Id cố định (sau khi đã khóa Room): hai phòng cùng kết thúc cho cùng một
            // tài khoản không ghi đè số dư của nhau và không deadlock. Có theo dõi để service cộng trực tiếp.
            users = await db.Users
                .FromSqlInterpolated($"SELECT * FROM \"Users\" WHERE \"Id\" = ANY({userIds}) ORDER BY \"Id\" FOR UPDATE")
                .ToListAsync(cancellationToken);
        }
        else
        {
            users = await db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToListAsync(cancellationToken);
        }

        var rewards = room.Status == RoomStatus.Finished
            ? await db.MatchRewards.AsNoTracking().Where(r => r.RoomId == room.Id).ToListAsync(cancellationToken)
            : [];

        return new GameSnapshot
        {
            Room = room,
            ApprovedQuestions = approvedQuestions,
            Players = players,
            Answers = answers,
            TotalQuestionCount = questions.Count,
            AnsweredCountForCurrentQuestion = answeredCount,
            Users = users.ToDictionary(u => u.Id),
            IsFinalAdvance = isFinalAdvance,
            Rewards = rewards,
        };
    }
}
