using Microsoft.EntityFrameworkCore;
using QuizBattle.Application.Common;
using QuizBattle.Application.Game;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;
using QuizBattle.Infrastructure.Persistence;
using QuizBattle.Tests.TestHelpers;

namespace QuizBattle.Tests.Integration;

/// <summary>Kết thúc trận trên Postgres thật: khóa FOR UPDATE, một lần cộng, không mất cập nhật, không deadlock.</summary>
[Collection(DatabaseCollection.Name)]
public sealed class RewardSettlementTests(DatabaseFixture fixture)
{
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    // Cả chủ phòng lẫn timer đều đủ điều kiện chuyển sang Finished tại mốc này.
    private static readonly DateTimeOffset FinishAt = Start.AddSeconds(10 + Room.RESULT_DISPLAY_SECONDS + 1);

    [RequiresDatabaseFact]
    public async Task NextQuestion_TwoConcurrentCallsAtFinalQuestion_RewardedExactlyOnce()
    {
        var room = await SeedRoomAsync(scoreA: 800, scoreB: 600);

        var results = await Task.WhenAll(
            TryNextQuestionAsync(room.Code, room.HostUserId),
            TryNextQuestionAsync(room.Code, room.HostUserId));

        Assert.Single(results, r => r is null); // đúng một lời gọi thắng
        Assert.Single(results, r => r is ConflictException); // lời gọi còn lại bị từ chối vì trận đã kết thúc

        await using var db = fixture.CreateContext();
        Assert.Equal(RoomStatus.Finished, (await db.Rooms.AsNoTracking().SingleAsync(r => r.Id == room.Id)).Status);
        Assert.Equal(2, await db.MatchRewards.CountAsync(r => r.RoomId == room.Id));
        Assert.Equal(2, await db.CoinTransactions.CountAsync(c => c.RoomId == room.Id));
        await AssertInvariantsAsync(room.UserIds);
        Assert.Equal(80, (await db.Users.AsNoTracking().SingleAsync(u => u.Id == room.UserIds[0])).TotalXp);
    }

    [RequiresDatabaseFact]
    public async Task TwoRoomsFinishingConcurrentlyForSameUser_NoLostUpdate()
    {
        var shared = await SeedUserAsync("shared");
        var expectedXp = 0;

        // Lặp nhiều vòng để tăng khả năng hai transaction thật sự chồng lên nhau.
        for (var round = 0; round < 8; round++)
        {
            var room1 = await SeedRoomAsync(scoreA: 800, scoreB: 100, accountA: shared);
            var room2 = await SeedRoomAsync(scoreA: 500, scoreB: 100, accountA: shared);

            var both = Task.WhenAll(
                TryNextQuestionAsync(room1.Code, room1.HostUserId),
                TryNextQuestionAsync(room2.Code, room2.HostUserId));
            var results = await both.WaitAsync(TimeSpan.FromSeconds(30)); // quá hạn = nghi deadlock

            Assert.All(results, r => Assert.Null(r));
            expectedXp += 80 + 50;
        }

        await using var db = fixture.CreateContext();
        Assert.Equal(expectedXp, (await db.Users.AsNoTracking().SingleAsync(u => u.Id == shared.Id)).TotalXp);
        await AssertInvariantsAsync([shared.Id]);
    }

    // Trả về exception nghiệp vụ (nếu có) để test khẳng định ai thắng, ai bị từ chối.
    private async Task<Exception?> TryNextQuestionAsync(string code, Guid hostUserId)
    {
        await using var db = fixture.CreateContext();
        var service = new GameService(new GameRepository(db), new UnusedTokenGenerator(), new FakeTimeProvider(FinishAt));
        try
        {
            await service.NextQuestionAsync(hostUserId, code, CancellationToken.None);
            return null;
        }
        catch (ConflictException ex)
        {
            return ex;
        }
    }

    // TotalXp == SUM(MatchRewards.Xp) và CoinBalance == SUM(CoinTransactions.Amount) cho từng tài khoản.
    private async Task AssertInvariantsAsync(IEnumerable<Guid> userIds)
    {
        await using var db = fixture.CreateContext();
        foreach (var userId in userIds)
        {
            var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == userId);
            var xp = await db.MatchRewards.Where(r => r.UserId == userId).SumAsync(r => r.Xp);
            var coins = await db.CoinTransactions.Where(c => c.UserId == userId).SumAsync(c => c.Amount);
            Assert.Equal(xp, user.TotalXp);
            Assert.Equal(coins, user.CoinBalance);
        }
    }

    private async Task<User> SeedUserAsync(string name)
    {
        var id = Guid.NewGuid();
        var user = new User { Id = id, Email = $"{name}-{id:N}@test.local", PasswordHash = "h", DisplayName = name, CreatedAt = Start };
        await using var db = fixture.CreateContext();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private sealed record SeededRoom(Guid Id, string Code, Guid HostUserId, Guid[] UserIds);

    // Phòng InProgress còn đúng một câu đã kết thúc; hai tài khoản đã trả lời. Mỗi lần gọi dùng dữ liệu mới.
    private async Task<SeededRoom> SeedRoomAsync(int scoreA, int scoreB, User? accountA = null)
    {
        var host = await SeedUserAsync("host");
        var a = accountA ?? await SeedUserAsync("a");
        var b = await SeedUserAsync("b");
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
        return new SeededRoom(room.Id, room.Code, host.Id, [a.Id, b.Id]);
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
