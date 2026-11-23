using Microsoft.EntityFrameworkCore;
using QuizBattle.Application.Common;
using QuizBattle.Application.Practice;
using QuizBattle.Domain.Entities;
using QuizBattle.Tests.TestHelpers;

namespace QuizBattle.Tests.Integration;

/// <summary>Nộp đáp án và luật XP cơ bản (FR-005 đến FR-008) trên Postgres thật.</summary>
[Collection(DatabaseCollection.Name)]
public sealed class PracticeSubmitTests(DatabaseFixture fixture)
{
    private static readonly DateOnly Day = new(2026, 11, 21);
    private readonly PracticeTestData _data = new(fixture);

    private async Task<(User Me, SeededRoom Room, Guid SessionId)> SetupAsync(int questions = 3)
    {
        var me = await _data.UserAsync("me");
        var room = await _data.FinishedRoomAsync(await _data.UserAsync("host"), [me, await _data.UserAsync()], questions);
        return (me, room, await _data.StartAsync(me, room));
    }

    [RequiresDatabaseFact]
    public async Task Submit_CorrectAnswer_AwardsXpAndWritesRowAndUpdatesTotalXp()
    {
        var (me, _, sessionId) = await SetupAsync();

        var (questionId, result) = await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct);

        Assert.True(result.IsCorrect);
        Assert.Equal(PracticeTestData.Correct, result.CorrectIndex);
        Assert.Equal(5, result.XpAwarded);
        Assert.Null(result.XpReason);
        Assert.Equal(5, result.XpGainedToday);
        Assert.Equal(95, result.RemainingToday);
        Assert.False(result.Completed);
        Assert.Equal(5, await _data.TotalXpAsync(me.Id));
        Assert.Equal(5, await _data.DailyXpAsync(me.Id, Day));

        await using var db = fixture.CreateContext();
        var row = await db.PracticeAnswers.AsNoTracking().SingleAsync(a => a.UserId == me.Id);
        Assert.Equal(questionId, row.QuestionId);
        Assert.Equal(sessionId, row.SessionId);
        Assert.Equal(5, row.XpAwarded);
        Assert.Equal(Day, row.Day);
        Assert.Null(row.XpReason);
        await XpInvariant.AssertAsync(db, [me.Id]);
    }

    [RequiresDatabaseFact]
    public async Task Submit_WrongAnswer_ZeroXpWithWrongReasonAndStillRevealsCorrectIndex()
    {
        var (me, _, sessionId) = await SetupAsync();

        var (_, result) = await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Wrong);

        Assert.False(result.IsCorrect);
        Assert.Equal(PracticeTestData.Correct, result.CorrectIndex);
        Assert.Equal(0, result.XpAwarded);
        Assert.Equal("wrong", result.XpReason);
        Assert.Equal(0, await _data.TotalXpAsync(me.Id));
        Assert.Equal(0, await _data.DailyXpAsync(me.Id, Day));
    }

    [RequiresDatabaseFact]
    public async Task Submit_SameQuestionNextSessionSameDay_ZeroXpAlreadyToday()
    {
        var (me, room, first) = await SetupAsync();
        for (var i = 0; i < 3; i++)
        {
            await _data.AnswerCurrentAsync(me, first, PracticeTestData.Correct); // lượt 1 xong: cả 3 câu đã nhận XP hôm nay
        }

        var second = await _data.StartAsync(me, room);
        var (_, result) = await _data.AnswerCurrentAsync(me, second, PracticeTestData.Correct);

        Assert.True(result.IsCorrect);
        Assert.Equal(0, result.XpAwarded);
        Assert.Equal("already_today", result.XpReason);
        Assert.Equal(15, await _data.TotalXpAsync(me.Id));
        Assert.Equal(15, await _data.DailyXpAsync(me.Id, Day));
    }

    [RequiresDatabaseFact]
    public async Task Submit_SameQuestionNextDay_AwardsAgainOnSeparateCounterRow()
    {
        var (me, room, first) = await SetupAsync();
        for (var i = 0; i < 3; i++)
        {
            await _data.AnswerCurrentAsync(me, first, PracticeTestData.Correct);
        }

        var nextDay = PracticeTestData.Noon.AddDays(1);
        var second = await _data.StartAsync(me, room, nextDay);
        var (_, result) = await _data.AnswerCurrentAsync(me, second, PracticeTestData.Correct, nextDay);

        Assert.Equal(5, result.XpAwarded);
        Assert.Null(result.XpReason);
        Assert.Equal(15, await _data.DailyXpAsync(me.Id, Day));
        Assert.Equal(5, await _data.DailyXpAsync(me.Id, Day.AddDays(1)));
        Assert.Equal(20, await _data.TotalXpAsync(me.Id));
    }

    [RequiresDatabaseFact]
    public async Task Submit_Correct_DoesNotCreateCoinTransactionOrChangeCoinBalance()
    {
        var (me, _, sessionId) = await SetupAsync();

        await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct);

        await using var db = fixture.CreateContext();
        Assert.False(await db.CoinTransactions.AnyAsync(c => c.UserId == me.Id));
        Assert.Equal(0, (await db.Users.AsNoTracking().SingleAsync(u => u.Id == me.Id)).CoinBalance);
    }

    [RequiresDatabaseFact]
    public async Task Submit_OutOfOrderUnansweredQuestion_Throws409NotCurrent()
    {
        var (me, _, sessionId) = await SetupAsync();
        await using var db = fixture.CreateContext();
        var service = PracticeTestData.Service(db, PracticeTestData.Noon);
        var state = await service.GetSessionAsync(me.Id, sessionId, CancellationToken.None);
        var order = (await db.PracticeSessions.AsNoTracking().SingleAsync(s => s.Id == sessionId)).QuestionIds;
        var notCurrent = order.First(id => id != state.Current!.QuestionId);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.SubmitAnswerAsync(me.Id, sessionId, new SubmitPracticeAnswerRequest(notCurrent, PracticeTestData.Correct), CancellationToken.None));

        Assert.Equal("not_current_question", ex.Code);
        Assert.False(await db.PracticeAnswers.AnyAsync(a => a.UserId == me.Id));
        Assert.Equal(0, await _data.DailyXpAsync(me.Id, Day));
    }

    [RequiresDatabaseFact]
    public async Task Submit_QuestionNotInSession_Throws404()
    {
        var (me, _, sessionId) = await SetupAsync();
        var otherRoom = await _data.FinishedRoomAsync(await _data.UserAsync("host"), [await _data.UserAsync(), await _data.UserAsync()]);
        await using var db = fixture.CreateContext();
        var service = PracticeTestData.Service(db, PracticeTestData.Noon);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.SubmitAnswerAsync(me.Id, sessionId, new SubmitPracticeAnswerRequest(Guid.NewGuid(), 0), CancellationToken.None));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.SubmitAnswerAsync(me.Id, sessionId, new SubmitPracticeAnswerRequest(otherRoom.Questions[0].Id, 0), CancellationToken.None));
    }

    [RequiresDatabaseFact]
    public async Task Submit_OtherUsersSession_Throws404AndWritesNothing()
    {
        var (me, room, sessionId) = await SetupAsync();
        var stranger = room.Players.Single(p => p.Id != me.Id);
        await using var db = fixture.CreateContext();
        var service = PracticeTestData.Service(db, PracticeTestData.Noon);
        var state = await service.GetSessionAsync(me.Id, sessionId, CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.SubmitAnswerAsync(stranger.Id, sessionId, new SubmitPracticeAnswerRequest(state.Current!.QuestionId, 0), CancellationToken.None));
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetSessionAsync(stranger.Id, sessionId, CancellationToken.None));
        Assert.False(await db.PracticeAnswers.AnyAsync(a => a.SessionId == sessionId));
    }

    [RequiresDatabaseFact]
    public async Task Submit_InvalidSelectedIndexOrEmptyQuestion_ThrowsValidation()
    {
        var (me, _, sessionId) = await SetupAsync();
        await using var db = fixture.CreateContext();
        var service = PracticeTestData.Service(db, PracticeTestData.Noon);
        var state = await service.GetSessionAsync(me.Id, sessionId, CancellationToken.None);

        foreach (var bad in new[] { -1, 4, 99 })
        {
            await Assert.ThrowsAsync<RequestValidationException>(() =>
                service.SubmitAnswerAsync(me.Id, sessionId, new SubmitPracticeAnswerRequest(state.Current!.QuestionId, bad), CancellationToken.None));
        }

        await Assert.ThrowsAsync<RequestValidationException>(() =>
            service.SubmitAnswerAsync(me.Id, sessionId, new SubmitPracticeAnswerRequest(Guid.Empty, 0), CancellationToken.None));
    }

    [RequiresDatabaseFact]
    public async Task Submit_LastQuestion_CompletesSessionAndSummaryIsCorrect()
    {
        var (me, _, sessionId) = await SetupAsync();

        await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct);
        await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Wrong);
        var (_, last) = await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct);

        Assert.True(last.Completed);
        await using var db = fixture.CreateContext();
        var state = await PracticeTestData.Service(db, PracticeTestData.Noon).GetSessionAsync(me.Id, sessionId, CancellationToken.None);
        Assert.True(state.Completed);
        Assert.Null(state.Current);
        Assert.Equal(3, state.AnsweredCount);
        Assert.Equal(new PracticeSummaryDto(2, 10, 10, 90, new DateTimeOffset(2026, 11, 22, 0, 0, 0, TimeSpan.FromHours(7))), state.Summary);
    }
}
