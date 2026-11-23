using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuizBattle.Application.Common;
using QuizBattle.Application.Practice;
using QuizBattle.Domain.Entities;
using Xunit.Abstractions;

namespace QuizBattle.Tests.Integration;

/// <summary>Lượt ôn: một lượt mở cho mỗi (user, phòng), lastResult, thứ tự lưu cố định, câu bị xóa giữa lượt (FR-011 đến FR-013).</summary>
[Collection(DatabaseCollection.Name)]
public sealed class PracticeSessionTests(DatabaseFixture fixture, ITestOutputHelper output)
{
    private readonly PracticeTestData _data = new(fixture);

    private async Task<(User Me, SeededRoom Room)> SetupAsync(int questions = 3)
    {
        var me = await _data.UserAsync("me");
        var room = await _data.FinishedRoomAsync(await _data.UserAsync("host"), [me, await _data.UserAsync()], questions);
        return (me, room);
    }

    private async Task<PracticeSessionStartDto> StartAsync(User user, SeededRoom room, DateTimeOffset? now = null)
    {
        await using var db = fixture.CreateContext();
        return await PracticeTestData.Service(db, now ?? PracticeTestData.Noon)
            .StartSessionAsync(user.Id, new StartPracticeSessionRequest(room.Room.Code), CancellationToken.None);
    }

    private async Task<PracticeSessionDto> GetAsync(User user, Guid sessionId)
    {
        await using var db = fixture.CreateContext();
        return await PracticeTestData.Service(db, PracticeTestData.Noon).GetSessionAsync(user.Id, sessionId, CancellationToken.None);
    }

    [RequiresDatabaseFact]
    public async Task CreateSession_Twice_ReturnsSameSessionIdSecondNotCreated()
    {
        var (me, room) = await SetupAsync();

        var first = await StartAsync(me, room);
        var second = await StartAsync(me, room);

        Assert.True(first.Created);
        Assert.False(second.Created);
        Assert.Equal(first.Session.SessionId, second.Session.SessionId);
        Assert.Equal(3, second.Session.Total);
    }

    [RequiresDatabaseFact]
    public async Task CreateSession_TwoParallelRequests_ReturnSameSessionId()
    {
        // 8 request đồng thời (>= 2 như yêu cầu): đúng một request tạo (201), phần còn lại nhận lại cùng lượt (200).
        const int tasks = 8;
        var (me, room) = await SetupAsync();

        var (outcomes, stats) = await ConcurrentRun.RunCatchingAsync(fixture, tasks, (_, db) =>
            PracticeTestData.Service(db, PracticeTestData.Noon).StartSessionAsync(me.Id, new StartPracticeSessionRequest(room.Room.Code), CancellationToken.None));

        output.WriteLine($"CreateSession_TwoParallelRequests: {stats}");
        ConcurrentRun.AssertReallyConcurrent(stats);
        Assert.All(outcomes, o => Assert.True(o.Succeeded, o.Error?.ToString()));
        var started = outcomes.Select(o => o.Value!).ToList();
        Assert.Single(started.Select(s => s.Session.SessionId).Distinct());
        Assert.Equal(1, started.Count(s => s.Created));

        await using var db = fixture.CreateContext();
        Assert.Equal(1, await db.PracticeSessions.CountAsync(s => s.UserId == me.Id && s.CompletedAt == null));
    }

    [RequiresDatabaseFact]
    public async Task CreateSession_AfterCompleted_CreatesNewSession()
    {
        var (me, room) = await SetupAsync();
        var first = await StartAsync(me, room);
        for (var i = 0; i < 3; i++)
        {
            await _data.AnswerCurrentAsync(me, first.Session.SessionId, PracticeTestData.Correct);
        }

        var second = await StartAsync(me, room);

        Assert.True(second.Created);
        Assert.NotEqual(first.Session.SessionId, second.Session.SessionId);
    }

    [RequiresDatabaseFact]
    public async Task CreateSession_DifferentRooms_BothOpenAllowed()
    {
        var me = await _data.UserAsync("me");
        var roomA = await _data.FinishedRoomAsync(await _data.UserAsync("host"), [me, await _data.UserAsync()]);
        var roomB = await _data.FinishedRoomAsync(await _data.UserAsync("host"), [me, await _data.UserAsync()]);

        var a = await StartAsync(me, roomA);
        var b = await StartAsync(me, roomB);

        Assert.True(a.Created);
        Assert.True(b.Created);
        Assert.NotEqual(a.Session.SessionId, b.Session.SessionId);
    }

    [RequiresDatabaseFact]
    public async Task CreateSession_TwoUsersSameRoom_EachGetsOwnSession()
    {
        var (me, room) = await SetupAsync();
        var partner = room.Players.Single(p => p.Id != me.Id);

        var mine = await StartAsync(me, room);
        var theirs = await StartAsync(partner, room);

        Assert.True(theirs.Created);
        Assert.NotEqual(mine.Session.SessionId, theirs.Session.SessionId);
    }

    [RequiresDatabaseFact]
    public async Task GetSession_AfterSubmit_ReturnsLastResultWithCorrectIndex()
    {
        var (me, room) = await SetupAsync();
        var sessionId = (await StartAsync(me, room)).Session.SessionId;
        var (questionId, _) = await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Wrong);

        var state = await GetAsync(me, sessionId); // như F5 sau khi nộp

        Assert.Equal(new PracticeResultDto(questionId, false, PracticeTestData.Correct, 0, "wrong"), state.LastResult);
        Assert.Equal(1, state.AnsweredCount);
        Assert.NotEqual(questionId, state.Current!.QuestionId);
    }

    [RequiresDatabaseFact]
    public async Task GetSession_NoAnswerYet_LastResultNullAndFirstQuestionCurrent()
    {
        var (me, room) = await SetupAsync();
        var sessionId = (await StartAsync(me, room)).Session.SessionId;

        var state = await GetAsync(me, sessionId);

        Assert.Null(state.LastResult);
        Assert.Null(state.Summary);
        Assert.False(state.Completed);
        Assert.Equal(0, state.Current!.Index);
        Assert.Equal(3, state.Total);
    }

    [RequiresDatabaseFact]
    public async Task GetSession_CurrentQuestionHasNoCorrectIndexInRawJson()
    {
        var (me, room) = await SetupAsync();
        var sessionId = (await StartAsync(me, room)).Session.SessionId;

        var json = JsonSerializer.Serialize(await GetAsync(me, sessionId), new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.DoesNotContain("correct", json, StringComparison.OrdinalIgnoreCase);
    }

    [RequiresDatabaseFact]
    public async Task Session_OrderIsStoredNotReshuffledOnRead()
    {
        var (me, room) = await SetupAsync(questions: 6);
        var sessionId = (await StartAsync(me, room)).Session.SessionId;
        await using var db = fixture.CreateContext();
        var stored = (await db.PracticeSessions.AsNoTracking().SingleAsync(s => s.Id == sessionId)).QuestionIds;

        var seen = new List<Guid>();
        for (var i = 0; i < 6; i++)
        {
            var peek1 = await GetAsync(me, sessionId);
            var peek2 = await GetAsync(me, sessionId);
            Assert.Equal(peek1.Current!.QuestionId, peek2.Current!.QuestionId); // đọc lại không đổi thứ tự
            seen.Add(peek1.Current.QuestionId);
            await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct);
        }

        Assert.Equal(stored, seen);
        Assert.Equal(room.QuestionIds.OrderBy(x => x), stored.OrderBy(x => x));
    }

    [RequiresDatabaseFact]
    public async Task Session_QuestionsDeletedMidway_EndsWithSummaryByAnsweredCount()
    {
        var (me, room) = await SetupAsync();
        var sessionId = (await StartAsync(me, room)).Session.SessionId;
        var (answered, _) = await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct);
        await using (var db = fixture.CreateContext())
        {
            await db.Questions.Where(q => q.RoomId == room.Room.Id && q.Id != answered).ExecuteDeleteAsync(); // mô phỏng xóa câu/phòng giữa lượt
        }

        var state = await GetAsync(me, sessionId);

        Assert.True(state.Completed);
        Assert.Null(state.Current);
        Assert.Equal(1, state.AnsweredCount);
        Assert.Equal(1, state.Total);
        Assert.Equal(1, state.Summary!.CorrectCount);
        Assert.Equal(5, state.Summary.XpGainedInSession);
        Assert.Equal(95, state.Summary.RemainingToday);
        await using var check = fixture.CreateContext();
        Assert.NotNull((await check.PracticeSessions.AsNoTracking().SingleAsync(s => s.Id == sessionId)).CompletedAt);
    }

    [RequiresDatabaseFact]
    public async Task Session_QuestionDeletedBeforeSubmit_Throws404()
    {
        var (me, room) = await SetupAsync();
        var sessionId = (await StartAsync(me, room)).Session.SessionId;
        var current = (await GetAsync(me, sessionId)).Current!.QuestionId;
        await using (var db = fixture.CreateContext())
        {
            await db.Questions.Where(q => q.Id == current).ExecuteDeleteAsync();
        }

        await using var service = fixture.CreateContext();
        await Assert.ThrowsAsync<NotFoundException>(() => PracticeTestData.Service(service, PracticeTestData.Noon)
            .SubmitAnswerAsync(me.Id, sessionId, new SubmitPracticeAnswerRequest(current, 0), CancellationToken.None));
    }

    [RequiresDatabaseFact]
    public async Task Session_DeletedRoom_KeepsXpAndSessionReadable()
    {
        var (me, room) = await SetupAsync();
        var sessionId = (await StartAsync(me, room)).Session.SessionId;
        await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct);
        await using (var db = fixture.CreateContext())
        {
            await db.Rooms.Where(r => r.Id == room.Room.Id).ExecuteDeleteAsync();
        }

        var state = await GetAsync(me, sessionId);

        Assert.True(state.Completed);
        Assert.Equal(5, await _data.TotalXpAsync(me.Id));
        Assert.Equal(0, state.Total); // mọi câu đã bị xóa cùng phòng
    }
}
