using Microsoft.EntityFrameworkCore;
using QuizBattle.Application.Practice;
using QuizBattle.Domain.Entities;
using QuizBattle.Tests.TestHelpers;
using Xunit.Abstractions;

namespace QuizBattle.Tests.Integration;

/// <summary>Trần XP ngày (FR-006, Ch2, Ch3): đồng thời, phần còn lại, trần 0, XP mỗi câu 0, sang ngày mới.</summary>
[Collection(DatabaseCollection.Name)]
public sealed class PracticeCapTests(DatabaseFixture fixture, ITestOutputHelper output)
{
    internal static readonly DateOnly Day = new(2026, 11, 21);
    private readonly PracticeTestData _data = new(fixture);

    // Một user có `rooms` phòng ôn được, mỗi phòng một câu và một lượt đang mở: nộp đồng thời `rooms` câu khác nhau của cùng một user.
    internal static async Task<(User Me, Guid[] SessionIds, Guid[] QuestionIds)> SeedParallelAsync(PracticeTestData data, int rooms)
    {
        var me = await data.UserAsync("me");
        var other = await data.UserAsync();
        var sessions = new List<Guid>();
        var questions = new List<Guid>();
        for (var i = 0; i < rooms; i++)
        {
            var room = await data.FinishedRoomAsync(await data.UserAsync("host"), [me, other], questions: 1);
            sessions.Add(await data.StartAsync(me, room));
            questions.Add(room.Questions[0].Id);
        }

        return (me, sessions.ToArray(), questions.ToArray());
    }

    [RequiresDatabaseFact]
    public async Task Cap_ParallelSubmitsOverCap_NeverExceedsCap()
    {
        const int tasks = 30; // 30 x 5 XP = 150 > trần 100
        var (me, sessions, questions) = await SeedParallelAsync(_data, tasks);

        var (outcomes, stats) = await ConcurrentRun.RunCatchingAsync(fixture, tasks, (i, db) =>
            PracticeTestData.Service(db, PracticeTestData.Noon).SubmitAnswerAsync(
                me.Id, sessions[i], new SubmitPracticeAnswerRequest(questions[i], PracticeTestData.Correct), CancellationToken.None));

        output.WriteLine($"Cap_ParallelSubmitsOverCap: {stats}");
        ConcurrentRun.AssertReallyConcurrent(stats);
        Assert.All(outcomes, o => Assert.True(o.Succeeded, o.Error?.ToString()));
        var results = outcomes.Select(o => o.Value!).ToList();
        Assert.Equal(100, results.Sum(r => r.XpAwarded));
        Assert.Equal(20, results.Count(r => r.XpAwarded == 5 && r.XpReason is null));
        Assert.Equal(10, results.Count(r => r.XpAwarded == 0 && r.XpReason == "daily_cap"));
        Assert.Equal(100, await _data.DailyXpAsync(me.Id, Day));
        Assert.Equal(100, await _data.TotalXpAsync(me.Id));

        await using var db = fixture.CreateContext();
        await XpInvariant.AssertAsync(db, [me.Id]);
    }

    [RequiresDatabaseFact]
    public async Task Cap_RemainingLessThanQuestionXp_GrantsRemainderThenZero()
    {
        // Cấu hình không chia hết: 7 XP mỗi câu, trần 10 -> 7, rồi 3, rồi 0.
        var options = new PracticeOptions(7, 10);
        var me = await _data.UserAsync("me");
        var room = await _data.FinishedRoomAsync(await _data.UserAsync("host"), [me, await _data.UserAsync()], questions: 3);
        var sessionId = await _data.StartAsync(me, room, options: options);

        var (_, first) = await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct, options: options);
        var (_, second) = await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct, options: options);
        var (_, third) = await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct, options: options);

        Assert.Equal((7, (string?)null), (first.XpAwarded, first.XpReason));
        Assert.Equal((3, "daily_cap_partial"), (second.XpAwarded, second.XpReason));
        Assert.Equal((0, "daily_cap"), (third.XpAwarded, third.XpReason));
        Assert.Equal(10, await _data.DailyXpAsync(me.Id, Day));
        Assert.Equal(10, await _data.TotalXpAsync(me.Id));
        Assert.Equal(0, third.RemainingToday);

        await using var db = fixture.CreateContext();
        await XpInvariant.AssertAsync(db, [me.Id]);
    }

    [RequiresDatabaseFact]
    public async Task Cap_Zero_NoXpAndNoDailyRow()
    {
        var options = new PracticeOptions(5, 0);
        var me = await _data.UserAsync("me");
        var room = await _data.FinishedRoomAsync(await _data.UserAsync("host"), [me, await _data.UserAsync()], questions: 1);
        var sessionId = await _data.StartAsync(me, room, options: options);

        var (_, result) = await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct, options: options);

        Assert.Equal((0, "daily_cap"), (result.XpAwarded, result.XpReason));
        Assert.Equal(0, result.RemainingToday);
        await using var db = fixture.CreateContext();
        Assert.False(await db.PracticeXpDailies.AnyAsync(d => d.UserId == me.Id));
        await XpInvariant.AssertAsync(db, [me.Id]);
    }

    [RequiresDatabaseFact]
    public async Task Cap_XpPerCorrectZero_CorrectAnswerNullReasonZeroXp()
    {
        var options = new PracticeOptions(0, 100);
        var me = await _data.UserAsync("me");
        var room = await _data.FinishedRoomAsync(await _data.UserAsync("host"), [me, await _data.UserAsync()], questions: 1);
        var sessionId = await _data.StartAsync(me, room, options: options);

        var (_, result) = await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct, options: options);

        Assert.True(result.IsCorrect);
        Assert.Equal((0, (string?)null), (result.XpAwarded, result.XpReason));
        Assert.Equal(0, await _data.TotalXpAsync(me.Id));
        await using var db = fixture.CreateContext();
        Assert.False(await db.PracticeXpDailies.AnyAsync(d => d.UserId == me.Id));
    }

    [RequiresDatabaseFact]
    public async Task Cap_NewDay_UsesSeparateCounterRow()
    {
        var options = new PracticeOptions(5, 5);
        var me = await _data.UserAsync("me");
        var room = await _data.FinishedRoomAsync(await _data.UserAsync("host"), [me, await _data.UserAsync()], questions: 3);
        var sessionId = await _data.StartAsync(me, room, options: options);

        var (_, day1First) = await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct, options: options);
        var (_, day1Second) = await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct, options: options);
        var (_, day2) = await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct, PracticeTestData.Noon.AddDays(1), options);

        Assert.Equal(5, day1First.XpAwarded);
        Assert.Equal((0, "daily_cap"), (day1Second.XpAwarded, day1Second.XpReason));
        Assert.Equal(5, day2.XpAwarded);
        Assert.Equal(5, await _data.DailyXpAsync(me.Id, Day));
        Assert.Equal(5, await _data.DailyXpAsync(me.Id, Day.AddDays(1)));
        Assert.Equal(10, await _data.TotalXpAsync(me.Id));
    }

    [RequiresDatabaseFact]
    public async Task Cap_LoweredBelowAlreadyUsed_DoesNotDecreaseCounterOrGrant()
    {
        // Hạ trần giữa ngày: đã dùng 100, trần mới 50. Bộ đếm không được giảm và không được cộng thêm.
        var me = await _data.UserAsync("me");
        var room = await _data.FinishedRoomAsync(await _data.UserAsync("host"), [me, await _data.UserAsync()], questions: 1);
        var options = new PracticeOptions(5, 50);
        var sessionId = await _data.StartAsync(me, room, options: options);
        await using (var db = fixture.CreateContext())
        {
            db.PracticeXpDailies.Add(new PracticeXpDaily { UserId = me.Id, Day = Day, Xp = 100 });
            await db.SaveChangesAsync();
        }

        var (_, result) = await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct, options: options);

        Assert.Equal((0, "daily_cap"), (result.XpAwarded, result.XpReason));
        Assert.Equal(100, await _data.DailyXpAsync(me.Id, Day));
    }
}
