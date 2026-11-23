using Microsoft.EntityFrameworkCore;
using Npgsql;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;
using QuizBattle.Infrastructure.Persistence;

namespace QuizBattle.Tests.Integration;

/// <summary>Ràng buộc schema của chế độ ôn tập (CHECK, unique một phần, SET NULL/CASCADE) trên Postgres thật bằng migration thật.</summary>
[Collection(DatabaseCollection.Name)]
public sealed class PracticeSchemaTests(DatabaseFixture fixture)
{
    private const string CheckViolation = "23514";
    private const string UniqueViolation = "23505";
    private static readonly DateOnly Today = new(2026, 11, 21);

    // ---- CK_PracticeAnswers_XpReason ----

    [RequiresDatabaseFact]
    public async Task XpReason_NullWithWrongAnswer_ViolatesCheck()
    {
        var seed = await SeedAsync();
        await using var db = fixture.CreateContext();
        db.PracticeAnswers.Add(Answer(seed, isCorrect: false, xp: 0, reason: null));

        await AssertViolationAsync(db, CheckViolation, "CK_PracticeAnswers_XpReason");
    }

    [RequiresDatabaseFact]
    public async Task XpReason_WrongWithXp_ViolatesCheck()
    {
        var seed = await SeedAsync();
        await using var db = fixture.CreateContext();
        db.PracticeAnswers.Add(Answer(seed, isCorrect: false, xp: 5, reason: PracticeXpReason.Wrong));

        await AssertViolationAsync(db, CheckViolation, "CK_PracticeAnswers_XpReason");
    }

    [RequiresDatabaseFact]
    public async Task XpReason_WrongWithCorrectAnswer_ViolatesCheck()
    {
        var seed = await SeedAsync();
        await using var db = fixture.CreateContext();
        db.PracticeAnswers.Add(Answer(seed, isCorrect: true, xp: 0, reason: PracticeXpReason.Wrong));

        await AssertViolationAsync(db, CheckViolation, "CK_PracticeAnswers_XpReason");
    }

    [RequiresDatabaseFact]
    public async Task XpReason_AlreadyTodayWithXp_ViolatesCheck()
    {
        var seed = await SeedAsync();
        await using var db = fixture.CreateContext();
        db.PracticeAnswers.Add(Answer(seed, isCorrect: true, xp: 5, reason: PracticeXpReason.AlreadyToday));

        await AssertViolationAsync(db, CheckViolation, "CK_PracticeAnswers_XpReason");
    }

    [RequiresDatabaseFact]
    public async Task XpReason_PartialWithZeroXp_ViolatesCheck()
    {
        var seed = await SeedAsync();
        await using var db = fixture.CreateContext();
        db.PracticeAnswers.Add(Answer(seed, isCorrect: true, xp: 0, reason: PracticeXpReason.DailyCapPartial));

        await AssertViolationAsync(db, CheckViolation, "CK_PracticeAnswers_XpReason");
    }

    [RequiresDatabaseFact]
    public async Task XpReason_UnknownValue_ViolatesCheck()
    {
        var seed = await SeedAsync();
        await using var db = fixture.CreateContext();
        db.PracticeAnswers.Add(Answer(seed, isCorrect: true, xp: 5, reason: null));
        await db.SaveChangesAsync();

        // Ghi thẳng một chuỗi ngoài tập cho phép (EF không cho tạo giá trị enum này).
        await using var other = fixture.CreateContext();
        var ex = await Assert.ThrowsAsync<PostgresException>(() => other.Database.ExecuteSqlAsync(
            $"""UPDATE "PracticeAnswers" SET "XpReason" = 'bogus', "XpAwarded" = 0 WHERE "UserId" = {seed.User.Id}"""));
        Assert.Equal(CheckViolation, ex.SqlState);
        Assert.Equal("CK_PracticeAnswers_XpReason", ex.ConstraintName);
    }

    [RequiresDatabaseFact]
    public async Task XpReason_NullWithCorrectAnswer_Allowed_IncludingZeroXp()
    {
        var seed = await SeedAsync();
        await using var db = fixture.CreateContext();
        db.PracticeAnswers.Add(Answer(seed, isCorrect: true, xp: 5, reason: null));
        db.PracticeAnswers.Add(Answer(seed, isCorrect: true, xp: 0, reason: null, question: seed.Questions[1])); // XpPerCorrect = 0

        await db.SaveChangesAsync();
    }

    [RequiresDatabaseFact]
    public async Task XpReason_AllValidCombinations_Allowed()
    {
        var seed = await SeedAsync(questionCount: 4);
        await using var db = fixture.CreateContext();
        db.PracticeAnswers.Add(Answer(seed, false, 0, PracticeXpReason.Wrong, seed.Questions[0]));
        db.PracticeAnswers.Add(Answer(seed, true, 0, PracticeXpReason.AlreadyToday, seed.Questions[1]));
        db.PracticeAnswers.Add(Answer(seed, true, 0, PracticeXpReason.DailyCap, seed.Questions[2]));
        db.PracticeAnswers.Add(Answer(seed, true, 3, PracticeXpReason.DailyCapPartial, seed.Questions[3]));

        await db.SaveChangesAsync();

        var stored = await db.PracticeAnswers.AsNoTracking().Where(a => a.UserId == seed.User.Id).Select(a => a.XpReason).ToListAsync();
        Assert.Equal(4, stored.Count(r => r is not null)); // đọc lại qua converter về enum
    }

    [RequiresDatabaseFact]
    public async Task XpReason_StoredAsSnakeCaseText()
    {
        var seed = await SeedAsync();
        await using var db = fixture.CreateContext();
        db.PracticeAnswers.Add(Answer(seed, true, 3, PracticeXpReason.DailyCapPartial));
        await db.SaveChangesAsync();

        var raw = await db.Database.SqlQuery<string>(
            $"""SELECT "XpReason" AS "Value" FROM "PracticeAnswers" WHERE "UserId" = {seed.User.Id}""").SingleAsync();

        Assert.Equal("daily_cap_partial", raw);
    }

    [RequiresDatabaseFact]
    public async Task SelectedIndex_OutOfRange_ViolatesCheck()
    {
        var seed = await SeedAsync();
        await using var db = fixture.CreateContext();
        var answer = Answer(seed, true, 5, null);
        answer.SelectedIndex = 4;
        db.PracticeAnswers.Add(answer);

        await AssertViolationAsync(db, CheckViolation, "CK_PracticeAnswers_SelectedIndex_Range");
    }

    [RequiresDatabaseFact]
    public async Task PracticeXpDaily_NegativeXp_ViolatesCheck()
    {
        var seed = await SeedAsync();
        await using var db = fixture.CreateContext();
        db.PracticeXpDailies.Add(new PracticeXpDaily { UserId = seed.User.Id, Day = Today, Xp = -1 });

        await AssertViolationAsync(db, CheckViolation, "CK_PracticeXpDaily_Xp_NonNegative");
    }

    // ---- Unique ----

    [RequiresDatabaseFact]
    public async Task PracticeAnswers_DuplicateSessionQuestion_ViolatesUnique()
    {
        var seed = await SeedAsync();
        await using var first = fixture.CreateContext();
        first.PracticeAnswers.Add(Answer(seed, true, 5, null));
        await first.SaveChangesAsync();

        await using var second = fixture.CreateContext();
        second.PracticeAnswers.Add(Answer(seed, false, 0, PracticeXpReason.Wrong, day: Today.AddDays(1))); // khác ngày, vẫn cùng (lượt, câu)

        await AssertViolationAsync(second, UniqueViolation, "IX_PracticeAnswers_SessionId_QuestionId");
    }

    [RequiresDatabaseFact]
    public async Task PracticeAnswers_SameUserQuestionDayTwiceWithXp_ViolatesPartialUnique()
    {
        var seed = await SeedAsync();
        var otherSession = await SeedSessionAsync(seed, open: false);
        await using var first = fixture.CreateContext();
        first.PracticeAnswers.Add(Answer(seed, true, 5, null));
        await first.SaveChangesAsync();

        await using var second = fixture.CreateContext();
        second.PracticeAnswers.Add(Answer(seed, true, 5, null, session: otherSession)); // lượt khác, cùng (user, câu, ngày), cùng có XP

        await AssertViolationAsync(second, UniqueViolation, "IX_PracticeAnswers_UserId_QuestionId_Day_Awarded");
    }

    [RequiresDatabaseFact]
    public async Task PracticeAnswers_SameUserQuestionDay_SecondWithZeroXp_Allowed()
    {
        var seed = await SeedAsync();
        var otherSession = await SeedSessionAsync(seed, open: false);
        await using var db = fixture.CreateContext();
        db.PracticeAnswers.Add(Answer(seed, true, 5, null));
        db.PracticeAnswers.Add(Answer(seed, true, 0, PracticeXpReason.AlreadyToday, session: otherSession));

        await db.SaveChangesAsync();
    }

    [RequiresDatabaseFact]
    public async Task PracticeAnswers_SameUserQuestionNextDayWithXp_Allowed()
    {
        var seed = await SeedAsync();
        var otherSession = await SeedSessionAsync(seed, open: false);
        await using var db = fixture.CreateContext();
        db.PracticeAnswers.Add(Answer(seed, true, 5, null));
        db.PracticeAnswers.Add(Answer(seed, true, 5, null, session: otherSession, day: Today.AddDays(1)));

        await db.SaveChangesAsync();
    }

    [RequiresDatabaseFact]
    public async Task PracticeSessions_TwoOpenForSameUserRoom_ViolatesPartialUnique()
    {
        var seed = await SeedAsync();
        await using var db = fixture.CreateContext();
        db.PracticeSessions.Add(NewSession(seed, completed: false));

        await AssertViolationAsync(db, UniqueViolation, "IX_PracticeSessions_OpenPerUserRoom");
    }

    [RequiresDatabaseFact]
    public async Task PracticeSessions_OneOpenOneCompleted_Allowed()
    {
        var seed = await SeedAsync();
        await using var db = fixture.CreateContext();
        db.PracticeSessions.Add(NewSession(seed, completed: true));

        await db.SaveChangesAsync();
    }

    [RequiresDatabaseFact]
    public async Task PracticeSessions_OpenForDifferentRooms_Allowed()
    {
        var seed = await SeedAsync();
        var otherRoom = await SeedAsync(user: seed.User);
        await using var db = fixture.CreateContext();

        // seed.Session và otherRoom.Session đều mở, cùng user, khác phòng: đã lưu thành công ở SeedAsync.
        Assert.Equal(2, await db.PracticeSessions.CountAsync(s => s.UserId == seed.User.Id && s.CompletedAt == null));
        Assert.NotEqual(seed.Room.Id, otherRoom.Room.Id);
    }

    [RequiresDatabaseFact]
    public async Task PracticeSessions_NullRoomId_AllowedManyTimes()
    {
        var seed = await SeedAsync();
        await using var db = fixture.CreateContext();
        db.PracticeSessions.Add(NewSession(seed, completed: false, nullRoom: true));
        db.PracticeSessions.Add(NewSession(seed, completed: false, nullRoom: true));

        await db.SaveChangesAsync();
    }

    // ---- SET NULL / CASCADE ----

    [RequiresDatabaseFact]
    public async Task DeleteRoom_KeepsPracticeAnswersWithNullRoomAndQuestionAndSession()
    {
        var seed = await SeedAsync();
        await using (var db = fixture.CreateContext())
        {
            db.PracticeAnswers.Add(Answer(seed, true, 5, null));
            await db.SaveChangesAsync();
        }

        await using (var db = fixture.CreateContext())
        {
            await db.Rooms.Where(r => r.Id == seed.Room.Id).ExecuteDeleteAsync(); // câu hỏi cascade theo phòng
            await db.PracticeSessions.Where(s => s.Id == seed.Session.Id).ExecuteDeleteAsync(); // gỡ cả lượt: SessionId phải về NULL
        }

        await using var check = fixture.CreateContext();
        var answer = await check.PracticeAnswers.AsNoTracking().SingleAsync(a => a.UserId == seed.User.Id);
        Assert.Equal(5, answer.XpAwarded);
        Assert.Null(answer.QuestionId);
        Assert.Null(answer.SessionId);
    }

    [RequiresDatabaseFact]
    public async Task DeleteRoom_KeepsPracticeSessionWithNullRoomId()
    {
        var seed = await SeedAsync();

        await using (var db = fixture.CreateContext())
        {
            await db.Rooms.Where(r => r.Id == seed.Room.Id).ExecuteDeleteAsync();
        }

        await using var check = fixture.CreateContext();
        var session = await check.PracticeSessions.AsNoTracking().SingleAsync(s => s.Id == seed.Session.Id);
        Assert.Null(session.RoomId);
        Assert.Equal(seed.Room.Code, session.RoomCode);
    }

    [RequiresDatabaseFact]
    public async Task DeleteUser_CascadesPracticeRows()
    {
        var seed = await SeedAsync();
        await using (var db = fixture.CreateContext())
        {
            db.PracticeAnswers.Add(Answer(seed, true, 5, null));
            db.PracticeXpDailies.Add(new PracticeXpDaily { UserId = seed.User.Id, Day = Today, Xp = 5 });
            await db.SaveChangesAsync();
        }

        await using (var db = fixture.CreateContext())
        {
            // Người chơi không phải chủ phòng để chỉ kiểm cascade của ôn tập (Rooms.HostUserId có quy tắc riêng).
            await db.Rooms.Where(r => r.Id == seed.Room.Id).ExecuteDeleteAsync();
            await db.Users.Where(u => u.Id == seed.User.Id).ExecuteDeleteAsync();
        }

        await using var check = fixture.CreateContext();
        Assert.False(await check.PracticeAnswers.AnyAsync(a => a.UserId == seed.User.Id));
        Assert.False(await check.PracticeSessions.AnyAsync(s => s.UserId == seed.User.Id));
        Assert.False(await check.PracticeXpDailies.AnyAsync(d => d.UserId == seed.User.Id));
    }

    [RequiresDatabaseFact]
    public async Task QuestionIds_RoundTripAsUuidArrayKeepingOrder()
    {
        var seed = await SeedAsync(questionCount: 4);

        await using var db = fixture.CreateContext();
        var session = await db.PracticeSessions.AsNoTracking().SingleAsync(s => s.Id == seed.Session.Id);

        Assert.Equal(seed.Questions.Select(q => q.Id), session.QuestionIds);
    }

    // ---- Dữ liệu mẫu ----

    private sealed record Seed(User User, Room Room, Question[] Questions, PracticeSession Session);

    private static async Task AssertViolationAsync(AppDbContext db, string sqlState, string constraintName)
    {
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var pg = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal(sqlState, pg.SqlState);
        Assert.Equal(constraintName, pg.ConstraintName);
    }

    // Tạo (hoặc dùng lại) user, một phòng mới, các câu hỏi và MỘT lượt đang mở của user ở phòng đó.
    private async Task<Seed> SeedAsync(int questionCount = 2, User? user = null)
    {
        var owner = user ?? NewUser();
        var room = new Room
        {
            Id = Guid.NewGuid(),
            Code = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(),
            HostUserId = owner.Id,
            Topic = "Chủ đề",
            Status = RoomStatus.Finished,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var questions = Enumerable.Range(0, questionCount).Select(i => new Question
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            OrderIndex = i,
            Text = $"Câu {i}",
            Options = ["A", "B", "C", "D"],
            CorrectIndex = 0,
            ReviewStatus = QuestionReviewStatus.Approved,
        }).ToArray();
        var session = new PracticeSession
        {
            Id = Guid.NewGuid(),
            UserId = owner.Id,
            RoomId = room.Id,
            RoomCode = room.Code,
            RoomTopic = room.Topic,
            QuestionIds = questions.Select(q => q.Id).ToArray(),
            StartedAt = DateTimeOffset.UtcNow,
        };

        await using var db = fixture.CreateContext();
        if (user is null)
        {
            db.Users.Add(owner);
        }

        db.Rooms.Add(room);
        db.Questions.AddRange(questions);
        await db.SaveChangesAsync();
        db.PracticeSessions.Add(session);
        await db.SaveChangesAsync();
        return new Seed(owner, room, questions, session);
    }

    // Lượt thứ hai của cùng (user, phòng): phải đã hoàn tất để không đụng unique "một lượt mở".
    private async Task<PracticeSession> SeedSessionAsync(Seed seed, bool open)
    {
        var session = NewSession(seed, completed: !open);
        await using var db = fixture.CreateContext();
        db.PracticeSessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }

    private static PracticeSession NewSession(Seed seed, bool completed, bool nullRoom = false) => new()
    {
        Id = Guid.NewGuid(),
        UserId = seed.User.Id,
        RoomId = nullRoom ? null : seed.Room.Id,
        RoomCode = seed.Room.Code,
        RoomTopic = seed.Room.Topic,
        QuestionIds = seed.Questions.Select(q => q.Id).ToArray(),
        StartedAt = DateTimeOffset.UtcNow,
        CompletedAt = completed ? DateTimeOffset.UtcNow : null,
    };

    private static PracticeAnswer Answer(Seed seed, bool isCorrect, int xp, PracticeXpReason? reason, Question? question = null, PracticeSession? session = null, DateOnly? day = null) => new()
    {
        Id = Guid.NewGuid(),
        SessionId = (session ?? seed.Session).Id,
        UserId = seed.User.Id,
        QuestionId = (question ?? seed.Questions[0]).Id,
        SelectedIndex = isCorrect ? 0 : 1,
        IsCorrect = isCorrect,
        XpAwarded = xp,
        XpReason = reason,
        Day = day ?? Today,
        AnsweredAt = DateTimeOffset.UtcNow,
    };

    private static User NewUser()
    {
        var id = Guid.NewGuid();
        return new User
        {
            Id = id,
            Email = $"{id:N}@test.local",
            PasswordHash = "hash",
            DisplayName = "Tester",
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}
