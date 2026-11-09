using QuizBattle.Application.Common;
using QuizBattle.Application.Game;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Tests.TestHelpers;

/// <summary>
/// Repository giả cho test GameService. Mô phỏng khóa phòng (FOR UPDATE) bằng SemaphoreSlim và áp
/// NewMatchRewards/NewCoinTransactions sau khi work chạy xong, như GameRepository thật làm với SaveChanges.
/// </summary>
public sealed class FakeGameRepository : IGameRepository
{
    private readonly SemaphoreSlim _roomLock = new(1, 1);

    public required Room Room { get; init; }

    public List<Question> Questions { get; } = [];

    public List<Player> Players { get; } = [];

    public List<Answer> Answers { get; } = [];

    public List<User> Users { get; } = [];

    public List<MatchReward> MatchRewards { get; } = [];

    public List<CoinTransaction> CoinTransactions { get; } = [];

    /// <summary>Gọi sau work, trước khi áp thưởng; ném lỗi để mô phỏng bước lưu thất bại.</summary>
    public Action? FailBeforeCommit { get; set; }

    /// <summary>Ép IsFinalAdvance của snapshot (null = tính như repository thật) để thử đường Users không được khóa.</summary>
    public bool? ForceIsFinalAdvance { get; set; }

    public Task<GameSnapshot?> FindSnapshotByCodeAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(Room.Code == code ? BuildSnapshot() : null);

    public Task<Player?> FindPlayerByTokenHashAsync(string sessionTokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(Players.SingleOrDefault(p => p.SessionTokenHash == sessionTokenHash));

    public Task<TResult> WithLockedRoomAsync<TResult>(string code, Func<GameSnapshot, TResult> work, CancellationToken cancellationToken)
    {
        if (Room.Code != code)
        {
            throw new NotFoundException("Không tìm thấy phòng.");
        }

        return RunLockedAsync(work, cancellationToken);
    }

    public Task<TResult> WithLockedRoomByIdAsync<TResult>(Guid roomId, Func<GameSnapshot, TResult> work, CancellationToken cancellationToken)
    {
        if (Room.Id != roomId)
        {
            throw new NotFoundException("Không tìm thấy phòng.");
        }

        return RunLockedAsync(work, cancellationToken);
    }

    public Task<IReadOnlyList<DueRoom>> FindDueRoomsAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
        throw new NotSupportedException(); // truy vấn SQL thật, không mô phỏng ở lớp giả

    public Task<Player> AddPlayerAsync(string code, Func<GameSnapshot, Player> buildPlayer, CancellationToken cancellationToken)
    {
        if (Room.Code != code)
        {
            throw new NotFoundException("Không tìm thấy phòng.");
        }

        var player = buildPlayer(BuildSnapshot());
        Players.Add(player);
        return Task.FromResult(player);
    }

    public Task<TResult> SubmitAnswerAsync<TResult>(
        Guid playerId, Func<SubmitAnswerSnapshot, (Answer Answer, TResult Result)> work, CancellationToken cancellationToken)
    {
        var player = Players.SingleOrDefault(p => p.Id == playerId) ?? throw new NotFoundException("Không tìm thấy người chơi.");
        var question = Questions.SingleOrDefault(q => q.RoomId == Room.Id && q.StartedAt != null && q.EndedAt == null);
        var answeredCount = question is null ? 0 : Answers.Count(a => a.QuestionId == question.Id);

        var snapshot = new SubmitAnswerSnapshot
        {
            Player = player,
            Room = Room,
            Question = question,
            AnsweredCountForCurrentQuestion = answeredCount,
            TotalPlayers = Players.Count,
        };

        var (answer, result) = work(snapshot);

        if (Answers.Any(a => a.PlayerId == answer.PlayerId && a.QuestionId == answer.QuestionId))
        {
            throw new ConflictException("Bạn đã trả lời câu này."); // mô phỏng unique index 23505
        }

        Answers.Add(answer);
        return Task.FromResult(result);
    }

    private async Task<TResult> RunLockedAsync<TResult>(Func<GameSnapshot, TResult> work, CancellationToken cancellationToken)
    {
        await _roomLock.WaitAsync(cancellationToken);
        var rollback = CaptureState();
        try
        {
            var snapshot = BuildSnapshot();
            var result = work(snapshot);

            FailBeforeCommit?.Invoke(); // mô phỏng SaveChanges lỗi sau khi work đã sửa entity
            MatchRewards.AddRange(snapshot.NewMatchRewards);
            CoinTransactions.AddRange(snapshot.NewCoinTransactions);
            return result;
        }
        catch
        {
            rollback(); // như transaction thật: work hoặc SaveChanges lỗi thì không giữ thay đổi nào
            throw;
        }
        finally
        {
            _roomLock.Release();
        }
    }

    // Chụp các trường work có thể sửa (entity thật được EF theo dõi nên rollback = bỏ thay đổi chưa lưu).
    private Action CaptureState()
    {
        var (status, startedAt, endedAt) = (Room.Status, Room.StartedAt, Room.EndedAt);
        var questions = Questions.Select(q => (q, q.StartedAt, q.EndedAt)).ToList();
        var scores = Players.Select(p => (p, p.Score)).ToList();
        var users = Users.Select(u => (u, u.TotalXp, u.CoinBalance)).ToList();

        return () =>
        {
            (Room.Status, Room.StartedAt, Room.EndedAt) = (status, startedAt, endedAt);
            foreach (var (q, s, e) in questions)
            {
                (q.StartedAt, q.EndedAt) = (s, e);
            }

            foreach (var (p, score) in scores)
            {
                p.Score = score;
            }

            foreach (var (u, xp, coins) in users)
            {
                (u.TotalXp, u.CoinBalance) = (xp, coins);
            }
        };
    }

    private GameSnapshot BuildSnapshot()
    {
        var approved = Questions.Where(q => q.ReviewStatus == QuestionReviewStatus.Approved).OrderBy(q => q.OrderIndex).ToList();
        var current = approved.Where(q => q.StartedAt is not null).OrderByDescending(q => q.StartedAt).FirstOrDefault();
        var players = Players.Where(p => p.RoomId == Room.Id).ToList();
        var userIds = players.Where(p => p.UserId is not null).Select(p => p.UserId!.Value).ToHashSet();

        return new GameSnapshot
        {
            Room = Room,
            ApprovedQuestions = approved,
            Players = players,
            Answers = Answers,
            TotalQuestionCount = Questions.Count,
            AnsweredCountForCurrentQuestion = current is null ? 0 : Answers.Count(a => a.QuestionId == current.Id),
            Users = Users.Where(u => userIds.Contains(u.Id)).ToDictionary(u => u.Id),
            IsFinalAdvance = ForceIsFinalAdvance
                ?? (Room.Status == RoomStatus.InProgress && approved.LastOrDefault() is { StartedAt: not null, EndedAt: not null }),
            Rewards = Room.Status == RoomStatus.Finished ? MatchRewards.Where(r => r.RoomId == Room.Id).ToList() : [],
        };
    }
}
