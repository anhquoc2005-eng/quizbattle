using Microsoft.EntityFrameworkCore;
using QuizBattle.Application.Common;
using QuizBattle.Application.Practice;
using QuizBattle.Domain.Entities;
using QuizBattle.Tests.TestHelpers;
using Xunit.Abstractions;

namespace QuizBattle.Tests.Integration;

/// <summary>Nộp đáp án idempotent (FR-005, SC-003): phát lại kết quả đã lưu, không chấm lại, không cộng XP.</summary>
[Collection(DatabaseCollection.Name)]
public sealed class PracticeIdempotencyTests(DatabaseFixture fixture, ITestOutputHelper output)
{
    private static readonly DateOnly Day = new(2026, 11, 21);
    private readonly PracticeTestData _data = new(fixture);

    private async Task<(User Me, Guid SessionId)> SetupAsync(int questions = 3)
    {
        var me = await _data.UserAsync("me");
        var room = await _data.FinishedRoomAsync(await _data.UserAsync("host"), [me, await _data.UserAsync()], questions);
        return (me, await _data.StartAsync(me, room));
    }

    private async Task<PracticeAnswerResultDto> SubmitAsync(User user, Guid sessionId, Guid questionId, int selectedIndex)
    {
        await using var db = fixture.CreateContext();
        return await PracticeTestData.Service(db, PracticeTestData.Noon)
            .SubmitAnswerAsync(user.Id, sessionId, new SubmitPracticeAnswerRequest(questionId, selectedIndex), CancellationToken.None);
    }

    private async Task<int> AnswerRowsAsync(Guid userId)
    {
        await using var db = fixture.CreateContext();
        return await db.PracticeAnswers.CountAsync(a => a.UserId == userId);
    }

    [RequiresDatabaseFact]
    public async Task Submit_SameQuestionAgain_ReturnsStoredResultAndDoesNotAddXp()
    {
        var (me, sessionId) = await SetupAsync();
        var (questionId, first) = await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct);

        var again = await SubmitAsync(me, sessionId, questionId, PracticeTestData.Correct);

        Assert.Equal(first.IsCorrect, again.IsCorrect);
        Assert.Equal(first.CorrectIndex, again.CorrectIndex);
        Assert.Equal(first.XpAwarded, again.XpAwarded);
        Assert.Equal(first.XpReason, again.XpReason);
        Assert.Equal(5, await _data.TotalXpAsync(me.Id));
        Assert.Equal(5, await _data.DailyXpAsync(me.Id, Day));
        Assert.Equal(1, await AnswerRowsAsync(me.Id));
    }

    [RequiresDatabaseFact]
    public async Task Submit_AgainWithDifferentSelectedIndex_StillReturnsFirstResult()
    {
        var (me, sessionId) = await SetupAsync();
        var (questionId, first) = await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Wrong);
        Assert.False(first.IsCorrect);

        var again = await SubmitAsync(me, sessionId, questionId, PracticeTestData.Correct); // gửi đáp án ĐÚNG ở lần sau

        Assert.False(again.IsCorrect);
        Assert.Equal("wrong", again.XpReason);
        Assert.Equal(0, again.XpAwarded);
        Assert.Equal(0, await _data.TotalXpAsync(me.Id));
        await using var db = fixture.CreateContext();
        var row = await db.PracticeAnswers.AsNoTracking().SingleAsync(a => a.UserId == me.Id);
        Assert.Equal(PracticeTestData.Wrong, row.SelectedIndex); // dòng đã lưu không bị ghi đè
    }

    [RequiresDatabaseFact]
    public async Task Submit_AgainAfterSessionCompleted_ReturnsStoredResult()
    {
        var (me, sessionId) = await SetupAsync();
        var (firstQuestion, first) = await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct);
        await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Wrong);
        await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct);

        var again = await SubmitAsync(me, sessionId, firstQuestion, PracticeTestData.Wrong);

        Assert.True(again.IsCorrect);
        Assert.Equal(first.XpAwarded, again.XpAwarded);
        Assert.True(again.Completed); // phản ánh trạng thái hiện tại của lượt
        Assert.Equal(3, await AnswerRowsAsync(me.Id));
        Assert.Equal(10, await _data.TotalXpAsync(me.Id));
    }

    [RequiresDatabaseFact]
    public async Task Submit_TwoParallelRequestsSameQuestion_BothSucceedWithSameResultAndXpOnce()
    {
        const int tasks = 8;
        var (me, sessionId) = await SetupAsync();
        await using var peek = fixture.CreateContext();
        var questionId = (await PracticeTestData.Service(peek, PracticeTestData.Noon).GetSessionAsync(me.Id, sessionId, CancellationToken.None)).Current!.QuestionId;

        var (outcomes, stats) = await ConcurrentRun.RunCatchingAsync(fixture, tasks, (_, db) =>
            PracticeTestData.Service(db, PracticeTestData.Noon).SubmitAnswerAsync(
                me.Id, sessionId, new SubmitPracticeAnswerRequest(questionId, PracticeTestData.Correct), CancellationToken.None));

        output.WriteLine($"Submit_TwoParallelRequestsSameQuestion: {stats}");
        ConcurrentRun.AssertReallyConcurrent(stats);
        Assert.All(outcomes, o => Assert.True(o.Succeeded, o.Error?.ToString())); // cả hai (tất cả) đều 200
        var results = outcomes.Select(o => o.Value!).ToList();
        Assert.All(results, r =>
        {
            Assert.True(r.IsCorrect);
            Assert.Equal(PracticeTestData.Correct, r.CorrectIndex);
            Assert.Equal(5, r.XpAwarded);
            Assert.Null(r.XpReason);
        });
        Assert.Equal(1, await AnswerRowsAsync(me.Id));
        Assert.Equal(5, await _data.TotalXpAsync(me.Id));
        Assert.Equal(5, await _data.DailyXpAsync(me.Id, Day));

        await using var db = fixture.CreateContext();
        await XpInvariant.AssertAsync(db, [me.Id]);
    }

    [RequiresDatabaseFact]
    public async Task Submit_Replay_ReportsCurrentXpGainedTodayNotFrozen()
    {
        var (me, sessionId) = await SetupAsync();
        var (firstQuestion, first) = await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct);
        await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct);

        var replay = await SubmitAsync(me, sessionId, firstQuestion, PracticeTestData.Correct);

        Assert.Equal(5, first.XpGainedToday);
        Assert.Equal(5, replay.XpAwarded); // kết quả câu: đã lưu
        Assert.Equal(10, replay.XpGainedToday); // tổng ngày: hiện tại
        Assert.Equal(90, replay.RemainingToday);
    }

    [RequiresDatabaseFact]
    public async Task Submit_UnansweredNonCurrentQuestion_StillConflict()
    {
        var (me, sessionId) = await SetupAsync();
        await using var db = fixture.CreateContext();
        var order = (await db.PracticeSessions.AsNoTracking().SingleAsync(s => s.Id == sessionId)).QuestionIds;

        var ex = await Assert.ThrowsAsync<ConflictException>(() => SubmitAsync(me, sessionId, order[2], PracticeTestData.Correct));

        Assert.Equal("not_current_question", ex.Code);
    }

    [RequiresDatabaseFact]
    public async Task Submit_AnsweredEarlierQuestionIsNotCurrent_ButReplaysInsteadOfConflict()
    {
        var (me, sessionId) = await SetupAsync();
        var (firstQuestion, _) = await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct);
        await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct); // giờ câu đầu không còn là câu hiện tại

        var replay = await SubmitAsync(me, sessionId, firstQuestion, PracticeTestData.Wrong);

        Assert.True(replay.IsCorrect);
    }
}
