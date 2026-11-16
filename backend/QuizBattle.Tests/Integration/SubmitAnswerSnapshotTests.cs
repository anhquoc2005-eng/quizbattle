using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;
using QuizBattle.Infrastructure.Persistence;

namespace QuizBattle.Tests.Integration;

/// <summary>
/// Truy vấn THẬT của GameRepository.SubmitAnswerAsync lấy danh sách người đã trả lời: chỉ câu ĐANG CHẠY, không lẫn answer của câu
/// trước. (Các test GameService chạy qua FakeGameRepository nên không kiểm được SQL này.)
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class SubmitAnswerSnapshotTests(DatabaseFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [RequiresDatabaseFact]
    public async Task SubmitAnswerSnapshot_CurrentQuestionHasAandB_PreviousHasC_ListsOnlyAandB()
    {
        var seeded = await SeedRoomAsync(answerersOfCurrent: ["A", "B"], answerersOfPrevious: ["C"]);

        var ids = await CaptureAnsweredPlayerIdsAsync(seeded.SubmitterId);

        Assert.Equal(2, ids.Count);
        Assert.Equivalent(new[] { seeded.PlayerIds["A"], seeded.PlayerIds["B"] }, ids);
        Assert.DoesNotContain(seeded.PlayerIds["C"], ids); // C chỉ trả lời câu trước
    }

    [RequiresDatabaseFact]
    public async Task SubmitAnswerSnapshot_CurrentQuestionNobodyAnswered_IsEmpty()
    {
        var seeded = await SeedRoomAsync(answerersOfCurrent: [], answerersOfPrevious: ["C"]);

        var ids = await CaptureAnsweredPlayerIdsAsync(seeded.SubmitterId);

        Assert.Empty(ids); // câu trước có answer của C vẫn không được tính
    }

    // Chạy SubmitAnswerAsync thật, chụp snapshot rồi ném lỗi trong work để transaction rollback (không ghi answer nào).
    private async Task<IReadOnlyList<Guid>> CaptureAnsweredPlayerIdsAsync(Guid submitterId)
    {
        IReadOnlyList<Guid>? captured = null;
        await using var db = fixture.CreateContext();

        await Assert.ThrowsAsync<StopAfterSnapshotException>(() => new GameRepository(db).SubmitAnswerAsync<int>(
            submitterId,
            snapshot =>
            {
                captured = snapshot.AnsweredPlayerIds;
                throw new StopAfterSnapshotException();
            },
            CancellationToken.None));

        return captured ?? throw new InvalidOperationException("Snapshot chưa được tạo.");
    }

    private sealed class StopAfterSnapshotException : Exception;

    private sealed record SeededRoom(IReadOnlyDictionary<string, Guid> PlayerIds, Guid SubmitterId);

    // Phòng InProgress với 2 câu: câu 0 đã kết thúc, câu 1 đang chạy. Người chơi A, B, C trả lời theo tham số; D chưa trả lời gì
    // và là người "nộp" (để lấy snapshot).
    private async Task<SeededRoom> SeedRoomAsync(string[] answerersOfCurrent, string[] answerersOfPrevious)
    {
        var hostId = Guid.NewGuid();
        var room = new Room
        {
            Id = Guid.NewGuid(), Code = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(), HostUserId = hostId, Topic = "Chủ đề",
            Status = RoomStatus.InProgress, CreatedAt = Now, StartedAt = Now,
        };
        var previous = NewQuestion(room.Id, 0, startedAt: Now, endedAt: Now.AddSeconds(10));
        var current = NewQuestion(room.Id, 1, startedAt: Now.AddSeconds(16), endedAt: null);
        var players = new[] { "A", "B", "C", "D" }.ToDictionary(
            name => name,
            name => new Player
            {
                Id = Guid.NewGuid(), RoomId = room.Id, UserId = null, DisplayName = $"Người {name}",
                SessionTokenHash = Guid.NewGuid().ToString("N"), JoinedAt = Now,
            });

        await using var db = fixture.CreateContext();
        db.Users.Add(new User { Id = hostId, Email = $"host-{hostId:N}@test.local", PasswordHash = "h", DisplayName = "host", CreatedAt = Now });
        db.Rooms.Add(room);
        db.Questions.AddRange(previous, current);
        db.Players.AddRange(players.Values);
        db.Answers.AddRange(answerersOfPrevious.Select(name => NewAnswer(players[name], previous)));
        db.Answers.AddRange(answerersOfCurrent.Select(name => NewAnswer(players[name], current)));
        await db.SaveChangesAsync();

        return new SeededRoom(players.ToDictionary(p => p.Key, p => p.Value.Id), players["D"].Id);
    }

    private static Question NewQuestion(Guid roomId, int orderIndex, DateTimeOffset startedAt, DateTimeOffset? endedAt) => new()
    {
        Id = Guid.NewGuid(), RoomId = roomId, OrderIndex = orderIndex, Text = $"Câu {orderIndex}", Options = ["A", "B", "C", "D"],
        CorrectIndex = 0, ReviewStatus = QuestionReviewStatus.Approved, StartedAt = startedAt, EndedAt = endedAt,
    };

    private static Answer NewAnswer(Player player, Question question) => new()
    {
        Id = Guid.NewGuid(), PlayerId = player.Id, QuestionId = question.Id, SelectedIndex = 0,
        AnsweredAt = question.StartedAt!.Value.AddSeconds(2), IsCorrect = true, PointsAwarded = 900,
    };
}
