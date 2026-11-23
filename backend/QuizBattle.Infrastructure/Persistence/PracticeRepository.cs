using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using QuizBattle.Application.Common;
using QuizBattle.Application.Practice;
using QuizBattle.Domain;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Infrastructure.Persistence;

/// <summary>Cách câu SQL giữ chỗ XP bị biến dạng trong test độ nhạy (mutation). Production luôn là Normal.</summary>
internal enum PracticeReserveVariant
{
    Normal,

    /// <summary>M1: bỏ WHERE Xp &lt; cap.</summary>
    NoWhere,

    /// <summary>M2: bỏ LEAST (cộng nguyên XP của câu dù vượt trần).</summary>
    NoLeast,

    /// <summary>M3: từ chối cả câu khi phần còn lại nhỏ hơn XP của câu, thay vì cộng phần còn lại.</summary>
    RejectWhole,

    /// <summary>M8: đọc rồi ghi không nguyên tử (tính granted ở ứng dụng từ số đã đọc, ghi cộng dồn không điều kiện).</summary>
    NonAtomic,
}

public sealed class PracticeRepository(AppDbContext db) : IPracticeRepository
{
    // CHỈ DÀNH CHO TEST (internal, qua InternalsVisibleTo), mặc định không làm gì: dùng để chứng minh test đủ nhạy (mẫu
    // AvatarRepository). Mã production không bao giờ đặt các thuộc tính này.
    internal bool UseUserRowLock { get; set; } = true;

    internal PracticeReserveVariant ReserveVariant { get; set; } = PracticeReserveVariant.Normal;

    internal bool SkipAlreadyTodayCheck { get; set; }

    internal bool SkipReplay { get; set; }

    internal bool UseOnConflict { get; set; } = true;

    /// <summary>Chạy sau khi đã đọc xong trạng thái và TRƯỚC khi giữ chỗ XP: test ép hai giao dịch cùng đọc rồi mới ghi.</summary>
    internal Func<Task>? AfterReads { get; set; }

    /// <summary>Chạy sau khi giữ chỗ XP và TRƯỚC SaveChanges: test ép lỗi để chứng minh rollback.</summary>
    internal Func<Task>? BeforeSave { get; set; }

    // Kiểm tra và tăng trong MỘT câu lệnh (mẫu AiQuotaRepository). Dòng chưa có: chèn LEAST(xp, cap). Dòng đã có: cộng phần còn
    // lại LEAST(xp, cap - Xp), CHỈ khi Xp < cap. Không có dòng trả về nghĩa là đã chạm trần.
    private const string ReserveSql = """
        INSERT INTO "PracticeXpDaily" ("UserId", "Day", "Xp")
        VALUES (@user, @day, LEAST(@xp, @cap))
        ON CONFLICT ("UserId", "Day")
        DO UPDATE SET "Xp" = "PracticeXpDaily"."Xp" + LEAST(@xp, @cap - "PracticeXpDaily"."Xp")
        WHERE "PracticeXpDaily"."Xp" < @cap
        RETURNING "Xp"
        """;

    private const string ReserveNoWhereSql = """
        INSERT INTO "PracticeXpDaily" ("UserId", "Day", "Xp")
        VALUES (@user, @day, LEAST(@xp, @cap))
        ON CONFLICT ("UserId", "Day")
        DO UPDATE SET "Xp" = "PracticeXpDaily"."Xp" + LEAST(@xp, @cap - "PracticeXpDaily"."Xp")
        RETURNING "Xp"
        """;

    private const string ReserveNoLeastSql = """
        INSERT INTO "PracticeXpDaily" ("UserId", "Day", "Xp")
        VALUES (@user, @day, @xp)
        ON CONFLICT ("UserId", "Day")
        DO UPDATE SET "Xp" = "PracticeXpDaily"."Xp" + @xp
        WHERE "PracticeXpDaily"."Xp" < @cap
        RETURNING "Xp"
        """;

    private const string ReserveRejectWholeSql = """
        INSERT INTO "PracticeXpDaily" ("UserId", "Day", "Xp")
        VALUES (@user, @day, @xp)
        ON CONFLICT ("UserId", "Day")
        DO UPDATE SET "Xp" = "PracticeXpDaily"."Xp" + @xp
        WHERE "PracticeXpDaily"."Xp" + @xp <= @cap
        RETURNING "Xp"
        """;

    private const string ReserveNonAtomicSql = """
        INSERT INTO "PracticeXpDaily" ("UserId", "Day", "Xp")
        VALUES (@user, @day, @xp)
        ON CONFLICT ("UserId", "Day")
        DO UPDATE SET "Xp" = "PracticeXpDaily"."Xp" + @xp
        RETURNING "Xp"
        """;

    // research R11: mức READ COMMITTED đặt tường minh; ON CONFLICT phải ghi ĐÚNG điều kiện của unique index một phần.
    private const string CreateSessionSql = """
        INSERT INTO "PracticeSessions" ("Id", "UserId", "RoomId", "RoomCode", "RoomTopic", "QuestionIds", "StartedAt")
        VALUES (@id, @user, @room, @code, @topic, @questionIds, @startedAt)
        ON CONFLICT ("UserId", "RoomId") WHERE "CompletedAt" IS NULL DO NOTHING
        RETURNING "Id"
        """;

    // M7: INSERT thường, không ON CONFLICT: bên thua cuộc đua nhận lỗi unique index.
    private const string CreateSessionPlainSql = """
        INSERT INTO "PracticeSessions" ("Id", "UserId", "RoomId", "RoomCode", "RoomTopic", "QuestionIds", "StartedAt")
        VALUES (@id, @user, @room, @code, @topic, @questionIds, @startedAt)
        RETURNING "Id"
        """;

    public async Task<IReadOnlyList<PracticeRoomRow>> ListRoomsAsync(Guid userId, CancellationToken cancellationToken) =>
        await PlayableRooms(userId)
            .OrderByDescending(r => r.EndedAt ?? r.CreatedAt)
            .Select(r => new PracticeRoomRow(
                r.Code,
                r.Topic,
                r.EndedAt ?? r.CreatedAt,
                r.Questions.Count(q => q.ReviewStatus == QuestionReviewStatus.Approved && q.EndedAt != null),
                r.HostUserId == userId ? "host" : "player",
                db.PracticeSessions
                    .Where(s => s.UserId == userId && s.RoomId == r.Id && s.CompletedAt == null)
                    .Select(s => (Guid?)s.Id)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

    public async Task<int> GetXpTodayAsync(Guid userId, DateOnly day, CancellationToken cancellationToken) =>
        await db.PracticeXpDailies.AsNoTracking()
            .Where(d => d.UserId == userId && d.Day == day)
            .Select(d => d.Xp)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<PlayableRoom?> FindPlayableRoomAsync(Guid userId, string roomCode, CancellationToken cancellationToken)
    {
        var room = await PlayableRooms(userId)
            .Where(r => r.Code == roomCode)
            .Select(r => new { r.Id, r.Code, r.Topic })
            .SingleOrDefaultAsync(cancellationToken);
        if (room is null)
        {
            return null;
        }

        var questionIds = await db.Questions.AsNoTracking()
            .Where(q => q.RoomId == room.Id && q.ReviewStatus == QuestionReviewStatus.Approved && q.EndedAt != null)
            .OrderBy(q => q.OrderIndex)
            .Select(q => q.Id)
            .ToListAsync(cancellationToken);

        return new PlayableRoom(room.Id, room.Code, room.Topic, questionIds);
    }

    public async Task<PracticeSessionStart> CreateOrGetSessionAsync(
        Guid userId, PlayableRoom room, IReadOnlyList<Guid> shuffledQuestionIds, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken);

        var newId = Guid.NewGuid();
        await using (var insert = db.Database.GetDbConnection().CreateCommand())
        {
            insert.Transaction = transaction.GetDbTransaction();
            insert.CommandText = UseOnConflict ? CreateSessionSql : CreateSessionPlainSql;
            insert.Parameters.Add(new NpgsqlParameter("id", newId));
            insert.Parameters.Add(new NpgsqlParameter("user", userId));
            insert.Parameters.Add(new NpgsqlParameter("room", room.RoomId));
            insert.Parameters.Add(new NpgsqlParameter("code", room.Code));
            insert.Parameters.Add(new NpgsqlParameter("topic", room.Topic));
            insert.Parameters.Add(new NpgsqlParameter("questionIds", shuffledQuestionIds.ToArray()));
            insert.Parameters.Add(new NpgsqlParameter<DateTime>("startedAt", now.UtcDateTime) { NpgsqlDbType = NpgsqlDbType.TimestampTz });

            if (await insert.ExecuteScalarAsync(cancellationToken) is Guid created)
            {
                await transaction.CommitAsync(cancellationToken);
                return new PracticeSessionStart(created, shuffledQuestionIds.Count, Created: true);
            }
        }

        // Đã có lượt đang mở. Đọc lại bằng MỘT CÂU LỆNH RIÊNG: ở READ COMMITTED mỗi câu có snapshot mới nên thấy dòng của
        // giao dịch đồng thời vừa commit (research R11). Không dùng DO UPDATE ... RETURNING: sẽ ghi/khóa dòng chỉ để đọc.
        var existing = await db.PracticeSessions.AsNoTracking()
            .Where(s => s.UserId == userId && s.RoomId == room.RoomId && s.CompletedAt == null)
            .Select(s => new { s.Id, Total = s.QuestionIds.Length })
            .SingleAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new PracticeSessionStart(existing.Id, existing.Total, Created: false);
    }

    public async Task<PracticeSessionState?> GetSessionAsync(Guid userId, Guid sessionId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var session = await db.PracticeSessions.SingleOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        var questions = await LoadQuestionsAsync(session.QuestionIds, cancellationToken);
        var answers = await db.PracticeAnswers.AsNoTracking()
            .Where(a => a.SessionId == sessionId)
            .ToListAsync(cancellationToken);

        var ordered = session.QuestionIds.Where(questions.ContainsKey).ToList();
        var answeredIds = answers.Where(a => a.QuestionId is not null).Select(a => a.QuestionId!.Value).ToHashSet();
        var remaining = ordered.Where(id => !answeredIds.Contains(id)).ToList();

        // Câu bị xóa giữa lượt (xóa phòng): không còn câu nào để làm thì lượt kết thúc, tổng kết theo số câu đã nộp (Ch8).
        if (session.CompletedAt is null && remaining.Count == 0)
        {
            session.CompletedAt = now;
            await db.SaveChangesAsync(cancellationToken);
        }

        var answered = answers.Where(a => a.QuestionId is { } q && questions.ContainsKey(q)).ToList();
        var last = answered.OrderByDescending(a => a.AnsweredAt).ThenByDescending(a => a.Id).FirstOrDefault();
        var current = session.CompletedAt is null && remaining.Count > 0
            ? new PracticeQuestionDto(remaining[0], answered.Count, questions[remaining[0]].Text, questions[remaining[0]].Options)
            : null;

        return new PracticeSessionState(
            session.Id,
            session.RoomTopic,
            ordered.Count,
            answered.Count,
            session.CompletedAt is not null,
            current,
            last is null ? null : ToResult(last, questions[last.QuestionId!.Value].CorrectIndex),
            answers.Count(a => a.IsCorrect),
            answers.Sum(a => a.XpAwarded));
    }

    public async Task<PracticeSubmitOutcome> SubmitAnswerAsync(PracticeSubmitCommand command, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // 1. Khóa dòng Users của chính user cho tới khi commit (CLAUDE.md): mọi lần ghi Users của user này xếp hàng ở đây. Chỉ MỘT
        // dòng Users và không khóa Room nên không nằm trong chu trình chờ với kết thúc trận hay cửa hàng.
        var user = await LoadUserAsync(command.UserId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy tài khoản.", "not_found");

        var session = await db.PracticeSessions.SingleOrDefaultAsync(s => s.Id == command.SessionId && s.UserId == command.UserId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy lượt ôn tập.", "not_found");
        if (!session.QuestionIds.Contains(command.QuestionId))
        {
            throw new NotFoundException("Câu hỏi không thuộc lượt ôn tập này.", "not_found");
        }

        var questions = await LoadQuestionsAsync(session.QuestionIds, cancellationToken);
        if (!questions.TryGetValue(command.QuestionId, out var question))
        {
            throw new NotFoundException("Câu hỏi không còn tồn tại.", "not_found");
        }

        // 2. Đã chấm thì phát lại kết quả đã lưu (idempotent): không chấm lại, không ghi gì, bỏ qua selectedIndex mới. Khóa Users đã
        // tuần tự hóa nên hai request đồng thời cùng câu không thể cùng thấy "chưa có dòng"; vì vậy không có nhánh bắt unique-violation
        // (research R12) - hai unique index chỉ là lưới của DB, vi phạm là lỗi lập trình và trả 500.
        var answers = await db.PracticeAnswers.AsNoTracking().Where(a => a.SessionId == command.SessionId).ToListAsync(cancellationToken);
        if (!SkipReplay && answers.FirstOrDefault(a => a.QuestionId == command.QuestionId) is { } stored)
        {
            return new PracticeSubmitOutcome(stored.IsCorrect, question.CorrectIndex, stored.XpAwarded, stored.XpReason, session.CompletedAt is not null, Replayed: true);
        }

        // 3. Chỉ được nộp câu hiện tại: câu đầu tiên còn tồn tại và chưa chấm theo thứ tự đã lưu.
        var answeredIds = answers.Where(a => a.QuestionId is not null).Select(a => a.QuestionId!.Value).ToHashSet();
        var current = session.QuestionIds.FirstOrDefault(id => questions.ContainsKey(id) && !answeredIds.Contains(id));
        if (current != command.QuestionId)
        {
            throw new ConflictException("Chỉ được trả lời câu hiện tại của lượt ôn.", "not_current_question");
        }

        // 4. Chấm ở server.
        var isCorrect = command.SelectedIndex == question.CorrectIndex;
        var usedBefore = await db.PracticeXpDailies.AsNoTracking()
            .Where(d => d.UserId == command.UserId && d.Day == command.Day)
            .Select(d => d.Xp)
            .SingleOrDefaultAsync(cancellationToken);
        var alreadyRewardedToday = !SkipAlreadyTodayCheck && await db.PracticeAnswers.AsNoTracking().AnyAsync(
            a => a.UserId == command.UserId && a.QuestionId == command.QuestionId && a.Day == command.Day && a.XpAwarded > 0,
            cancellationToken);
        var (desired, reason) = PracticeRewardCalculator.Calculate(isCorrect, alreadyRewardedToday, command.XpPerCorrect);

        if (AfterReads is { } afterReads)
        {
            await afterReads();
        }

        // 5. Nguồn sự thật của XP là kết quả câu SQL: granted = Xp trả về - Xp đã đọc dưới khóa (0 nếu chưa có dòng; không có dòng
        // trả về = 0). XpAwarded và XpReason suy từ granted; ApplyCap chỉ để đối chiếu, không thay thế SQL.
        var granted = 0;
        if (desired > 0 && command.DailyXpCap > 0)
        {
            var newTotal = await ReserveAsync(transaction, command, desired, usedBefore, cancellationToken);
            granted = newTotal is null ? 0 : newTotal.Value - usedBefore;
        }

        if (desired > 0)
        {
            if (granted != desired)
            {
                reason = granted > 0 ? PracticeXpReason.DailyCapPartial : PracticeXpReason.DailyCap;
            }

            AssertMatchesDomainRule(desired, usedBefore, command.DailyXpCap, granted, reason);
        }

        db.PracticeAnswers.Add(new PracticeAnswer
        {
            Id = Guid.NewGuid(),
            SessionId = command.SessionId,
            UserId = command.UserId,
            QuestionId = command.QuestionId,
            SelectedIndex = command.SelectedIndex,
            IsCorrect = isCorrect,
            XpAwarded = granted,
            XpReason = reason,
            Day = command.Day,
            AnsweredAt = command.Now,
        });
        user.TotalXp += granted;

        var completed = session.QuestionIds.All(id => !questions.ContainsKey(id) || answeredIds.Contains(id) || id == command.QuestionId);
        if (completed)
        {
            session.CompletedAt = command.Now;
        }

        if (BeforeSave is { } beforeSave)
        {
            await beforeSave();
        }

        await db.SaveChangesAsync(cancellationToken); // dòng XP, TotalXp và đóng lượt cùng một transaction với bộ đếm
        await transaction.CommitAsync(cancellationToken);
        return new PracticeSubmitOutcome(isCorrect, question.CorrectIndex, granted, reason, completed, Replayed: false);
    }

    // Phòng ôn được (xem IPracticeRepository.ListRoomsAsync). Dùng chung cho danh sách và tra theo mã.
    private IQueryable<Room> PlayableRooms(Guid userId) =>
        db.Rooms.AsNoTracking().Where(r =>
            r.Status == RoomStatus.Finished
            && (r.HostUserId == userId || r.Players.Any(p => p.UserId == userId))
            && db.MatchRewards.Any(m => m.RoomId == r.Id && m.Status == RewardStatus.Awarded)
            && r.Questions.Any(q => q.ReviewStatus == QuestionReviewStatus.Approved && q.EndedAt != null));

    private async Task<Dictionary<Guid, QuestionRow>> LoadQuestionsAsync(Guid[] ids, CancellationToken cancellationToken) =>
        await db.Questions.AsNoTracking()
            .Where(q => ids.Contains(q.Id))
            .Select(q => new QuestionRow(q.Id, q.Text, q.Options, q.CorrectIndex))
            .ToDictionaryAsync(q => q.Id, cancellationToken);

    private Task<User?> LoadUserAsync(Guid userId, CancellationToken cancellationToken) =>
        UseUserRowLock
            ? db.Users.FromSqlInterpolated($"SELECT * FROM \"Users\" WHERE \"Id\" = {userId} FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
            : db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

    // DbCommand thô thay vì SqlQuery: EF bọc câu thô trong SELECT ... FROM (...), không hợp lệ với INSERT ... RETURNING.
    private async Task<int?> ReserveAsync(
        IDbContextTransaction transaction, PracticeSubmitCommand command, int desired, int usedBefore, CancellationToken cancellationToken)
    {
        var xp = desired;
        if (ReserveVariant == PracticeReserveVariant.NonAtomic)
        {
            xp = Math.Min(desired, Math.Max(command.DailyXpCap - usedBefore, 0));
            if (xp <= 0)
            {
                return null;
            }
        }

        await using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.Transaction = transaction.GetDbTransaction();
        cmd.CommandText = ReserveVariant switch
        {
            PracticeReserveVariant.Normal => ReserveSql,
            PracticeReserveVariant.NoWhere => ReserveNoWhereSql,
            PracticeReserveVariant.NoLeast => ReserveNoLeastSql,
            PracticeReserveVariant.RejectWhole => ReserveRejectWholeSql,
            PracticeReserveVariant.NonAtomic => ReserveNonAtomicSql,
            _ => throw new InvalidOperationException("Biến thể giữ chỗ XP không hợp lệ."),
        };
        cmd.Parameters.Add(new NpgsqlParameter("user", command.UserId));
        cmd.Parameters.Add(new NpgsqlParameter("day", command.Day));
        cmd.Parameters.Add(new NpgsqlParameter("xp", xp));
        cmd.Parameters.Add(new NpgsqlParameter("cap", command.DailyXpCap));

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is int total ? total : null;
    }

    // ApplyCap là hàm thuần mô tả luật; SQL mới là nguồn sự thật. Lệch nghĩa là một trong hai sai: dừng và rollback thay vì ghi XP sai.
    // Chỉ kiểm khi chạy đúng thiết kế (biến thể Normal, có khóa): mutation cố ý làm SQL lệch luật để test độ nhạy.
    private void AssertMatchesDomainRule(int desired, int usedBefore, int cap, int granted, PracticeXpReason? reason)
    {
        if (ReserveVariant != PracticeReserveVariant.Normal || !UseUserRowLock)
        {
            return;
        }

        var (expectedGranted, expectedReason) = PracticeRewardCalculator.ApplyCap(desired, usedBefore, cap);
        if (expectedGranted != granted || (expectedReason is not null && expectedReason != reason))
        {
            throw new InvalidOperationException(
                $"Câu SQL giữ chỗ XP lệch luật ApplyCap (granted {granted} so với {expectedGranted}, desired {desired}, đã dùng {usedBefore}, trần {cap}).");
        }
    }

    private static PracticeResultDto ToResult(PracticeAnswer answer, int correctIndex) =>
        new(answer.QuestionId!.Value, answer.IsCorrect, correctIndex, answer.XpAwarded, answer.XpReason?.ToCode());

    private sealed record QuestionRow(Guid Id, string Text, string[] Options, int CorrectIndex);
}
