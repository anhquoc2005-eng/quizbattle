using Microsoft.EntityFrameworkCore;
using Npgsql;
using QuizBattle.Application.Practice;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;
using QuizBattle.Infrastructure.Persistence;
using QuizBattle.Tests.TestHelpers;

namespace QuizBattle.Tests.Integration;

public sealed record SeededRoom(Room Room, Question[] Questions, User Host, User[] Players)
{
    public Guid[] QuestionIds => Questions.Select(q => q.Id).ToArray();
}

/// <summary>Dữ liệu mẫu và cách dựng PracticeService cho test ôn tập trên Postgres thật. CorrectIndex của mọi câu là 0.</summary>
public sealed class PracticeTestData(DatabaseFixture fixture)
{
    public static readonly DateTimeOffset Noon = new(2026, 11, 21, 5, 0, 0, TimeSpan.Zero); // 12:00 ngày 21 giờ VN

    public const int Correct = 0;
    public const int Wrong = 1;

    public async Task<User> UserAsync(string name = "user")
    {
        var id = Guid.NewGuid();
        var user = new User { Id = id, Email = $"{name}-{id:N}@test.local", PasswordHash = "h", DisplayName = name, CreatedAt = Noon };
        await using var db = fixture.CreateContext();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    /// <summary>
    /// Phòng đã kết thúc: mỗi người chơi có một Player; nếu awarded thì có MatchReward Awarded cho từng người chơi (trận hợp lệ).
    /// </summary>
    public async Task<SeededRoom> FinishedRoomAsync(
        User host,
        User[]? players = null,
        int questions = 3,
        bool awarded = true,
        RoomStatus status = RoomStatus.Finished,
        bool questionsEnded = true,
        QuestionReviewStatus review = QuestionReviewStatus.Approved,
        string? code = null)
    {
        players ??= [];
        var room = new Room
        {
            Id = Guid.NewGuid(),
            Code = code ?? Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(),
            HostUserId = host.Id,
            Topic = "Lịch sử Việt Nam",
            Status = status,
            CreatedAt = Noon,
            StartedAt = Noon,
            EndedAt = status == RoomStatus.Finished ? Noon : null,
        };
        var qs = Enumerable.Range(0, questions).Select(i => new Question
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            OrderIndex = i,
            Text = $"Câu {i}",
            Options = ["A", "B", "C", "D"],
            CorrectIndex = Correct,
            ReviewStatus = review,
            StartedAt = questionsEnded ? Noon : null,
            EndedAt = questionsEnded ? Noon.AddSeconds(20) : null,
        }).ToArray();
        var seats = players.Select(p => new Player
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            UserId = p.Id,
            DisplayName = $"P-{Guid.NewGuid():N}"[..20],
            SessionTokenHash = Guid.NewGuid().ToString("N"),
            JoinedAt = Noon,
        }).ToArray();

        await using var db = fixture.CreateContext();
        db.Rooms.Add(room);
        db.Questions.AddRange(qs);
        db.Players.AddRange(seats);
        if (awarded)
        {
            db.MatchRewards.AddRange(seats.Select(s => new MatchReward
            {
                Id = Guid.NewGuid(),
                PlayerId = s.Id,
                UserId = s.UserId!.Value,
                RoomId = room.Id,
                RoomCode = room.Code,
                RoomTopic = room.Topic,
                FinishedAt = Noon,
                Status = RewardStatus.Awarded,
                Score = 0,
                RewardRank = 1,
                Xp = 0,
            }));
        }

        await db.SaveChangesAsync();
        return new SeededRoom(room, qs, host, players);
    }

    public static PracticeService Service(AppDbContext db, DateTimeOffset now, PracticeOptions? options = null, Action<PracticeRepository>? configure = null)
    {
        var repository = new PracticeRepository(db);
        configure?.Invoke(repository);
        return new PracticeService(repository, options ?? new PracticeOptions(5, 100), new FakeTimeProvider(now));
    }

    public async Task<Guid> StartAsync(User user, SeededRoom room, DateTimeOffset? now = null, PracticeOptions? options = null)
    {
        await using var db = fixture.CreateContext();
        var started = await Service(db, now ?? Noon, options).StartSessionAsync(user.Id, new StartPracticeSessionRequest(room.Room.Code), CancellationToken.None);
        return started.Session.SessionId;
    }

    /// <summary>Nộp câu hiện tại của lượt (câu đầu theo thứ tự đã xáo) với đáp án chọn trước.</summary>
    public async Task<(Guid QuestionId, PracticeAnswerResultDto Result)> AnswerCurrentAsync(
        User user, Guid sessionId, int selectedIndex, DateTimeOffset? now = null, PracticeOptions? options = null)
    {
        await using var db = fixture.CreateContext();
        var service = Service(db, now ?? Noon, options);
        var state = await service.GetSessionAsync(user.Id, sessionId, CancellationToken.None);
        var questionId = state.Current?.QuestionId ?? throw new InvalidOperationException("Lượt đã hết câu.");
        var result = await service.SubmitAnswerAsync(user.Id, sessionId, new SubmitPracticeAnswerRequest(questionId, selectedIndex), CancellationToken.None);
        return (questionId, result);
    }

    public async Task<int> DailyXpAsync(Guid userId, DateOnly day)
    {
        await using var db = fixture.CreateContext();
        return await db.PracticeXpDailies.AsNoTracking().Where(d => d.UserId == userId && d.Day == day).Select(d => d.Xp).SingleOrDefaultAsync();
    }

    public async Task<int> TotalXpAsync(Guid userId)
    {
        await using var db = fixture.CreateContext();
        return await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.TotalXp).SingleAsync();
    }
}

/// <summary>Số liệu đo được của một lần chạy đồng thời: số tác vụ, số kết nối DB (backend PID) khác nhau, số tác vụ chồng thời gian tối đa.</summary>
public sealed record ConcurrencyStats(int Tasks, int DistinctBackendPids, int MaxOverlap)
{
    public override string ToString() => $"tác vụ={Tasks}, backend PID khác nhau={DistinctBackendPids}, chồng thời gian tối đa={MaxOverlap}";
}

public static class ConcurrentRun
{
    public static readonly TimeSpan HardTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Chạy <paramref name="count"/> tác vụ, mỗi tác vụ một DbContext với MỘT kết nối riêng đã mở sẵn. Mọi tác vụ chờ ở cổng chung
    /// cho tới khi đủ <paramref name="count"/> kết nối đã mở rồi mới được thả cùng lúc; timeout cứng (quá hạn = nghi deadlock, test đỏ).
    /// Trả kết quả (hoặc exception) từng tác vụ và số liệu đo được: test PHẢI khẳng định số kết nối khác nhau bằng số tác vụ để không
    /// "xanh" nhờ chạy tuần tự.
    /// </summary>
    public static async Task<(IReadOnlyList<TResult> Results, ConcurrencyStats Stats)> RunAsync<TResult>(
        DatabaseFixture fixture, int count, Func<int, AppDbContext, Task<TResult>> work)
    {
        var allOpen = new CountdownEvent(count);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pids = new int[count];
        var starts = new long[count];
        var ends = new long[count];

        async Task<TResult> OneAsync(int i)
        {
            await using var db = fixture.CreateContext();
            await db.Database.OpenConnectionAsync();
            pids[i] = ((NpgsqlConnection)db.Database.GetDbConnection()).ProcessID;
            if (allOpen.Signal())
            {
                gate.TrySetResult(); // tác vụ cuối cùng mở xong kết nối thì thả cổng
            }

            await gate.Task;
            starts[i] = System.Diagnostics.Stopwatch.GetTimestamp();
            try
            {
                return await work(i, db);
            }
            finally
            {
                ends[i] = System.Diagnostics.Stopwatch.GetTimestamp();
            }
        }

        var tasks = Enumerable.Range(0, count).Select(i => Task.Run(() => OneAsync(i))).ToArray();
        TResult[] results;
        try
        {
            results = await Task.WhenAll(tasks).WaitAsync(HardTimeout);
        }
        catch (TimeoutException)
        {
            throw new Xunit.Sdk.XunitException($"Quá {HardTimeout.TotalSeconds}s: nghi deadlock hoặc chờ khóa vĩnh viễn ({count} tác vụ).");
        }

        var overlap = Enumerable.Range(0, count).Max(i => Enumerable.Range(0, count).Count(j => starts[j] <= ends[i] && ends[j] >= starts[i]));
        return (results, new ConcurrencyStats(count, pids.Distinct().Count(), overlap));
    }

    /// <summary>Như RunAsync nhưng bắt exception của từng tác vụ thành kết quả (để khẳng định ai thành công, ai lỗi).</summary>
    public static async Task<(IReadOnlyList<Outcome<TResult>> Results, ConcurrencyStats Stats)> RunCatchingAsync<TResult>(
        DatabaseFixture fixture, int count, Func<int, AppDbContext, Task<TResult>> work) =>
        await RunAsync(fixture, count, async (i, db) =>
        {
            try
            {
                return new Outcome<TResult>(await work(i, db), null);
            }
            catch (Exception ex)
            {
                return new Outcome<TResult>(default, ex);
            }
        });

    public static void AssertReallyConcurrent(ConcurrencyStats stats)
    {
        Assert.Equal(stats.Tasks, stats.DistinctBackendPids);
        Assert.True(stats.MaxOverlap >= Math.Min(stats.Tasks, 2), $"Các tác vụ không chồng thời gian: {stats}");
    }
}

public sealed record Outcome<T>(T? Value, Exception? Error)
{
    public bool Succeeded => Error is null;
}
