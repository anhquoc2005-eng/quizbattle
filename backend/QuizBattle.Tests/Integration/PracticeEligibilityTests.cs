using Microsoft.EntityFrameworkCore;
using QuizBattle.Application.Common;
using QuizBattle.Application.Practice;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Tests.Integration;

/// <summary>Điều kiện "phòng ôn được" (FR-002, Ch5) trên Postgres thật.</summary>
[Collection(DatabaseCollection.Name)]
public sealed class PracticeEligibilityTests(DatabaseFixture fixture)
{
    private readonly PracticeTestData _data = new(fixture);

    private async Task<PracticeRoomsResponse> ListAsync(User user)
    {
        await using var db = fixture.CreateContext();
        return await PracticeTestData.Service(db, PracticeTestData.Noon).GetRoomsAsync(user.Id, CancellationToken.None);
    }

    [RequiresDatabaseFact]
    public async Task ListRooms_PlayerAndHostRooms_Listed()
    {
        var me = await _data.UserAsync("me");
        var other = await _data.UserAsync();
        var asPlayer = await _data.FinishedRoomAsync(await _data.UserAsync("host"), [me, other]);
        var asHost = await _data.FinishedRoomAsync(me, [await _data.UserAsync(), await _data.UserAsync()]);

        var rooms = (await ListAsync(me)).Rooms;

        Assert.Equal(2, rooms.Count);
        Assert.Equal("player", rooms.Single(r => r.RoomCode == asPlayer.Room.Code).Role);
        Assert.Equal("host", rooms.Single(r => r.RoomCode == asHost.Room.Code).Role);
        Assert.All(rooms, r => Assert.Equal(3, r.QuestionCount));
    }

    [RequiresDatabaseFact]
    public async Task ListRooms_UnrelatedRoom_NotListed()
    {
        var me = await _data.UserAsync("me");
        await _data.FinishedRoomAsync(await _data.UserAsync("host"), [await _data.UserAsync(), await _data.UserAsync()]);

        Assert.Empty((await ListAsync(me)).Rooms);
    }

    [RequiresDatabaseFact]
    public async Task ListRooms_RoomNotFinished_NotListed()
    {
        var me = await _data.UserAsync("me");
        var host = await _data.UserAsync("host");
        await _data.FinishedRoomAsync(host, [me, await _data.UserAsync()], status: RoomStatus.InProgress);
        await _data.FinishedRoomAsync(host, [me, await _data.UserAsync()], status: RoomStatus.Lobby);

        Assert.Empty((await ListAsync(me)).Rooms);
    }

    [RequiresDatabaseFact]
    public async Task ListRooms_FinishedRoomWithoutAwardedReward_NotListed()
    {
        // Phòng chỉ có một mình / không ai được thưởng: không phải trận hợp lệ (RewardCalculator cần >= 2 tài khoản).
        var me = await _data.UserAsync("me");
        await _data.FinishedRoomAsync(me, [], awarded: false);
        await _data.FinishedRoomAsync(await _data.UserAsync("host"), [me], awarded: false);

        Assert.Empty((await ListAsync(me)).Rooms);
    }

    [RequiresDatabaseFact]
    public async Task ListRooms_GuestSeatOnly_NotListedForSomeoneElse()
    {
        // Ghế khách có UserId null nên không thể khớp bất kỳ tài khoản nào.
        var me = await _data.UserAsync("me");
        var room = await _data.FinishedRoomAsync(await _data.UserAsync("host"), [await _data.UserAsync(), await _data.UserAsync()]);
        await using (var db = fixture.CreateContext())
        {
            db.Players.Add(new Player
            {
                Id = Guid.NewGuid(), RoomId = room.Room.Id, UserId = null, DisplayName = "Khách",
                SessionTokenHash = Guid.NewGuid().ToString("N"), JoinedAt = PracticeTestData.Noon,
            });
            await db.SaveChangesAsync();
        }

        Assert.Empty((await ListAsync(me)).Rooms);
    }

    [RequiresDatabaseFact]
    public async Task ListRooms_OnlyApprovedEndedQuestionsCounted()
    {
        var me = await _data.UserAsync("me");
        var room = await _data.FinishedRoomAsync(await _data.UserAsync("host"), [me, await _data.UserAsync()], questions: 3);
        await using (var db = fixture.CreateContext())
        {
            db.Questions.Add(NewQuestion(room.Room.Id, 10, QuestionReviewStatus.Pending, ended: true)); // AI chưa duyệt
            db.Questions.Add(NewQuestion(room.Room.Id, 11, QuestionReviewStatus.Approved, ended: false)); // chưa chơi
            await db.SaveChangesAsync();
        }

        var item = Assert.Single((await ListAsync(me)).Rooms);
        Assert.Equal(3, item.QuestionCount);

        await using var check = fixture.CreateContext();
        var playable = await new QuizBattle.Infrastructure.Persistence.PracticeRepository(check).FindPlayableRoomAsync(me.Id, room.Room.Code, CancellationToken.None);
        Assert.Equal(room.QuestionIds.OrderBy(x => x), playable!.QuestionIds.OrderBy(x => x));
    }

    [RequiresDatabaseFact]
    public async Task ListRooms_RoomWithNoPlayedQuestions_NotListed()
    {
        var me = await _data.UserAsync("me");
        await _data.FinishedRoomAsync(await _data.UserAsync("host"), [me, await _data.UserAsync()], questionsEnded: false);
        await _data.FinishedRoomAsync(await _data.UserAsync("host"), [me, await _data.UserAsync()], review: QuestionReviewStatus.Pending);

        Assert.Empty((await ListAsync(me)).Rooms);
    }

    [RequiresDatabaseFact]
    public async Task ListRooms_IncludesOpenSessionId_AndOmitsCompletedOne()
    {
        var me = await _data.UserAsync("me");
        var room = await _data.FinishedRoomAsync(await _data.UserAsync("host"), [me, await _data.UserAsync()]);
        Assert.Null(Assert.Single((await ListAsync(me)).Rooms).OpenSessionId);

        var sessionId = await _data.StartAsync(me, room);
        Assert.Equal(sessionId, Assert.Single((await ListAsync(me)).Rooms).OpenSessionId);

        for (var i = 0; i < 3; i++)
        {
            await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Correct);
        }

        Assert.Null(Assert.Single((await ListAsync(me)).Rooms).OpenSessionId);
    }

    [RequiresDatabaseFact]
    public async Task ListRooms_ReportsDailyQuotaFromConfiguration()
    {
        var me = await _data.UserAsync("me");

        var response = await ListAsync(me);

        Assert.Equal(5, response.XpPerCorrect);
        Assert.Equal(100, response.DailyXpCap);
        Assert.Equal(0, response.XpGainedToday);
        Assert.Equal(100, response.RemainingToday);
        Assert.Equal(new DateTimeOffset(2026, 11, 22, 0, 0, 0, TimeSpan.FromHours(7)), response.ResetAt);
    }

    [RequiresDatabaseFact]
    public async Task CreateSession_IneligibleRoom_ReturnsNotFoundSameAsMissingRoom()
    {
        var me = await _data.UserAsync("me");
        var foreign = await _data.FinishedRoomAsync(await _data.UserAsync("host"), [await _data.UserAsync(), await _data.UserAsync()]);

        async Task<NotFoundException> StartAsync(string code)
        {
            await using var db = fixture.CreateContext();
            return await Assert.ThrowsAsync<NotFoundException>(() =>
                PracticeTestData.Service(db, PracticeTestData.Noon).StartSessionAsync(me.Id, new StartPracticeSessionRequest(code), CancellationToken.None));
        }

        var ineligible = await StartAsync(foreign.Room.Code);
        var missing = await StartAsync("ZZZZZZ");

        Assert.Equal(missing.Message, ineligible.Message);
        Assert.Equal(missing.Code, ineligible.Code);
        Assert.Equal(0, await CountSessionsAsync(me.Id));
    }

    [RequiresDatabaseFact]
    public async Task CreateSession_LowercaseCode_IsNormalized()
    {
        var me = await _data.UserAsync("me");
        var room = await _data.FinishedRoomAsync(await _data.UserAsync("host"), [me, await _data.UserAsync()]);

        await using var db = fixture.CreateContext();
        var started = await PracticeTestData.Service(db, PracticeTestData.Noon)
            .StartSessionAsync(me.Id, new StartPracticeSessionRequest(room.Room.Code.ToLowerInvariant()), CancellationToken.None);

        Assert.True(started.Created);
        Assert.Equal(3, started.Session.Total);
    }

    private async Task<int> CountSessionsAsync(Guid userId)
    {
        await using var db = fixture.CreateContext();
        return await db.PracticeSessions.CountAsync(s => s.UserId == userId);
    }

    private static Question NewQuestion(Guid roomId, int order, QuestionReviewStatus review, bool ended) => new()
    {
        Id = Guid.NewGuid(), RoomId = roomId, OrderIndex = order, Text = $"Câu {order}", Options = ["A", "B", "C", "D"],
        CorrectIndex = 0, ReviewStatus = review,
        StartedAt = ended ? PracticeTestData.Noon : null, EndedAt = ended ? PracticeTestData.Noon.AddSeconds(20) : null,
    };
}
