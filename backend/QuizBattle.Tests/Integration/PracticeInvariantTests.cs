using Microsoft.EntityFrameworkCore;
using Npgsql;
using QuizBattle.Application.Game;
using QuizBattle.Application.Practice;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;
using QuizBattle.Infrastructure.Persistence;
using QuizBattle.Tests.TestHelpers;
using Xunit.Abstractions;

namespace QuizBattle.Tests.Integration;

/// <summary>Bất biến XP hai nguồn, rollback, nửa đêm VN, và ôn tập đồng thời với kết thúc trận của cùng một user (FR-007, FR-015).</summary>
[Collection(DatabaseCollection.Name)]
public sealed class PracticeInvariantTests(DatabaseFixture fixture, ITestOutputHelper output)
{
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FinishAt = Start.AddSeconds(10 + Room.RESULT_DISPLAY_SECONDS + 1);
    private static readonly DateOnly Day = new(2026, 11, 21);
    private readonly PracticeTestData _data = new(fixture);

    [RequiresDatabaseFact]
    public async Task Invariant_MixedMatchAndPracticeSequence_Holds()
    {
        // Trận thật: kết thúc qua GameService (RewardSettlement), rồi ôn chính phòng đó.
        var room = await SeedInProgressRoomAsync(scoreA: 800, scoreB: 600);
        await FinishAsync(room);
        var me = room.A;
        await using (var before = fixture.CreateContext())
        {
            Assert.Equal(80, (await before.Users.AsNoTracking().SingleAsync(u => u.Id == me.Id)).TotalXp);
        }

        var practiceRoom = new SeededRoom(await LoadRoomAsync(room.RoomId), [], room.Host, [room.A, room.B]);
        var sessionId = await _data.StartAsync(me, practiceRoom);
        var (_, result) = await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct);

        Assert.Equal(5, result.XpAwarded);
        Assert.Equal(85, await _data.TotalXpAsync(me.Id));
        await using var db = fixture.CreateContext();
        await XpInvariant.AssertAsync(db, [me.Id, room.B.Id]);
    }

    [RequiresDatabaseFact]
    public async Task Invariant_RoomDeletedAfterPractice_RowsKeptAndInvariantHolds()
    {
        var me = await _data.UserAsync("me");
        var room = await _data.FinishedRoomAsync(await _data.UserAsync("host"), [me, await _data.UserAsync()], questions: 2);
        var sessionId = await _data.StartAsync(me, room);
        await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct);
        await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct);

        await using (var db = fixture.CreateContext())
        {
            await db.Rooms.Where(r => r.Id == room.Room.Id).ExecuteDeleteAsync();
        }

        await using var check = fixture.CreateContext();
        Assert.Equal(2, await check.PracticeAnswers.CountAsync(a => a.UserId == me.Id));
        Assert.Equal(10, await _data.TotalXpAsync(me.Id));
        await XpInvariant.AssertAsync(check, [me.Id]);
    }

    [RequiresDatabaseFact]
    public async Task Submit_FailureAfterCounterIncrement_RollsEverythingBack()
    {
        var me = await _data.UserAsync("me");
        var room = await _data.FinishedRoomAsync(await _data.UserAsync("host"), [me, await _data.UserAsync()], questions: 1);
        var sessionId = await _data.StartAsync(me, room);
        await using (var db = fixture.CreateContext())
        {
            var service = PracticeTestData.Service(db, PracticeTestData.Noon, configure: r => r.BeforeSave = () => throw new InvalidOperationException("ép lỗi sau khi tăng bộ đếm"));
            var state = await service.GetSessionAsync(me.Id, sessionId, CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitAnswerAsync(
                me.Id, sessionId, new SubmitPracticeAnswerRequest(state.Current!.QuestionId, PracticeTestData.Correct), CancellationToken.None));
        }

        await using var check = fixture.CreateContext();
        Assert.False(await check.PracticeXpDailies.AnyAsync(d => d.UserId == me.Id)); // bộ đếm đã tăng trong transaction nhưng bị hoàn
        Assert.False(await check.PracticeAnswers.AnyAsync(a => a.UserId == me.Id));
        Assert.Equal(0, await _data.TotalXpAsync(me.Id));
        Assert.Null((await check.PracticeSessions.AsNoTracking().SingleAsync(s => s.Id == sessionId)).CompletedAt);

        // Gửi lại bình thường sau lỗi: vẫn chấm và thưởng một lần.
        var (_, retry) = await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct);
        Assert.Equal(5, retry.XpAwarded);
        await XpInvariant.AssertAsync(check, [me.Id]);
    }

    [RequiresDatabaseFact]
    public async Task Submit_AcrossVietnamMidnight_UsesSeparateDailyCounters()
    {
        var options = new PracticeOptions(5, 5); // mỗi ngày chỉ nhận được 1 câu
        var me = await _data.UserAsync("me");
        var room = await _data.FinishedRoomAsync(await _data.UserAsync("host"), [me, await _data.UserAsync()], questions: 3);
        var sessionId = await _data.StartAsync(me, room, options: options);
        var beforeMidnight = new DateTimeOffset(2026, 11, 21, 16, 59, 59, TimeSpan.Zero); // 23:59:59 ngày 21 giờ VN
        var afterMidnight = beforeMidnight.AddSeconds(1); // 00:00:00 ngày 22 giờ VN

        var (_, a) = await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct, beforeMidnight, options);
        var (_, b) = await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct, beforeMidnight, options);
        var (_, c) = await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct, afterMidnight, options);

        Assert.Equal(5, a.XpAwarded);
        Assert.Equal((0, "daily_cap"), (b.XpAwarded, b.XpReason)); // cùng ngày 21: đã chạm trần
        Assert.Equal(5, c.XpAwarded); // sang ngày 22: bộ đếm mới
        Assert.Equal(5, await _data.DailyXpAsync(me.Id, Day));
        Assert.Equal(5, await _data.DailyXpAsync(me.Id, Day.AddDays(1)));
        await using var db = fixture.CreateContext();
        await XpInvariant.AssertAsync(db, [me.Id]);
    }

    [RequiresDatabaseFact]
    public async Task Submit_ConcurrentWithMatchSettlementOfSameUser_NoLostUpdateNoDeadlock()
    {
        var shared = await _data.UserAsync("shared");
        const int rounds = 6;
        var combined = new List<ConcurrencyStats>();

        for (var round = 0; round < rounds; round++)
        {
            var match = await SeedInProgressRoomAsync(scoreA: 800, scoreB: 100, accountA: shared);
            var practice = await _data.FinishedRoomAsync(await _data.UserAsync("host"), [shared, await _data.UserAsync()], questions: 1);
            var sessionId = await _data.StartAsync(shared, practice);
            var questionId = practice.Questions[0].Id;

            // Hai tác vụ cùng thả: kết thúc trận (khóa Room rồi Users theo Id) và ôn tập (khóa một dòng Users) của cùng user.
            var (outcomes, stats) = await ConcurrentRun.RunCatchingAsync<object?>(fixture, 2, async (i, db) =>
            {
                if (i == 0)
                {
                    var service = new GameService(new GameRepository(db), new UnusedTokenGenerator(), new FakeTimeProvider(FinishAt));
                    await service.NextQuestionAsync(match.Host.Id, match.Code, CancellationToken.None);
                    return null;
                }

                return await PracticeTestData.Service(db, PracticeTestData.Noon).SubmitAnswerAsync(
                    shared.Id, sessionId, new SubmitPracticeAnswerRequest(questionId, PracticeTestData.Correct), CancellationToken.None);
            });

            combined.Add(stats);
            ConcurrentRun.AssertReallyConcurrent(stats);
            foreach (var outcome in outcomes)
            {
                Assert.True(outcome.Succeeded, outcome.Error?.ToString());
                Assert.False(outcome.Error is PostgresException { SqlState: "40P01" or "40001" });
            }
        }

        output.WriteLine($"Submit_ConcurrentWithMatchSettlement: {rounds} vòng, mỗi vòng {combined[0]}; chồng tối đa nhỏ nhất={combined.Min(s => s.MaxOverlap)}");
        await using var db2 = fixture.CreateContext();
        await XpInvariant.AssertAsync(db2, [shared.Id]);
        Assert.Equal(rounds * 80 + rounds * 5, await _data.TotalXpAsync(shared.Id)); // mỗi vòng: 80 XP trận + 5 XP ôn
    }

    // ---- Dữ liệu mẫu trận thật (rút gọn từ RewardSettlementTests) ----

    private sealed record InProgressRoom(Guid RoomId, string Code, User Host, User A, User B);

    private async Task<InProgressRoom> SeedInProgressRoomAsync(int scoreA, int scoreB, User? accountA = null)
    {
        var host = await _data.UserAsync("host");
        var a = accountA ?? await _data.UserAsync("a");
        var b = await _data.UserAsync("b");
        var room = new Room
        {
            Id = Guid.NewGuid(), Code = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(), HostUserId = host.Id,
            Topic = "Chủ đề", Status = RoomStatus.InProgress, CreatedAt = Start, StartedAt = Start,
        };
        var question = new Question
        {
            Id = Guid.NewGuid(), RoomId = room.Id, OrderIndex = 0, Text = "Câu 1", Options = ["A", "B", "C", "D"],
            CorrectIndex = 0, ReviewStatus = QuestionReviewStatus.Approved, StartedAt = Start, EndedAt = Start.AddSeconds(10),
        };
        var playerA = NewPlayer(room.Id, a.Id, "A", scoreA);
        var playerB = NewPlayer(room.Id, b.Id, "B", scoreB);

        await using var db = fixture.CreateContext();
        db.Rooms.Add(room);
        db.Questions.Add(question);
        db.Players.AddRange(playerA, playerB);
        db.Answers.AddRange(NewAnswer(playerA, question, scoreA), NewAnswer(playerB, question, scoreB));
        await db.SaveChangesAsync();
        return new InProgressRoom(room.Id, room.Code, host, a, b);
    }

    private async Task FinishAsync(InProgressRoom room)
    {
        await using var db = fixture.CreateContext();
        var service = new GameService(new GameRepository(db), new UnusedTokenGenerator(), new FakeTimeProvider(FinishAt));
        await service.NextQuestionAsync(room.Host.Id, room.Code, CancellationToken.None);
    }

    private async Task<Room> LoadRoomAsync(Guid roomId)
    {
        await using var db = fixture.CreateContext();
        return await db.Rooms.AsNoTracking().SingleAsync(r => r.Id == roomId);
    }

    private static Player NewPlayer(Guid roomId, Guid userId, string name, int score) => new()
    {
        Id = Guid.NewGuid(), RoomId = roomId, UserId = userId, DisplayName = name, SessionTokenHash = Guid.NewGuid().ToString("N"),
        Score = score, JoinedAt = Start,
    };

    private static Answer NewAnswer(Player player, Question question, int points) => new()
    {
        Id = Guid.NewGuid(), PlayerId = player.Id, QuestionId = question.Id, SelectedIndex = 0,
        AnsweredAt = Start.AddSeconds(2), IsCorrect = points > 0, PointsAwarded = points,
    };

    private sealed class UnusedTokenGenerator : ISessionTokenGenerator
    {
        public string Generate() => throw new NotSupportedException();
    }
}
