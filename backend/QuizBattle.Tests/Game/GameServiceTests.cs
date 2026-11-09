using QuizBattle.Application.Common;
using QuizBattle.Application.Game;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;
using QuizBattle.Tests.TestHelpers;

namespace QuizBattle.Tests.Game;

public sealed class GameServiceTests
{
    private static readonly Guid HostUserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task OpenRoom_NoApprovedQuestions_Conflict()
    {
        var room = NewRoom(RoomStatus.Draft);
        var repository = new FakeGameRepository { Room = room };
        repository.Questions.Add(NewQuestion(room.Id, 0, QuestionReviewStatus.Pending));
        var service = CreateService(repository);

        await Assert.ThrowsAsync<ConflictException>(() => service.OpenRoomAsync(HostUserId, room.Code, CancellationToken.None));
    }

    [Fact]
    public async Task JoinRoom_NotLobby_Conflict()
    {
        var room = NewRoom(RoomStatus.Draft);
        var repository = new FakeGameRepository { Room = room };
        var service = CreateService(repository);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreatePlayerAsync(room.Code, new CreatePlayerRequest { DisplayName = "Alice" }, CancellationToken.None));
    }

    [Fact]
    public async Task JoinRoom_RoomFull_Conflict()
    {
        var room = NewRoom(RoomStatus.Lobby);
        var repository = new FakeGameRepository { Room = room };
        for (var i = 0; i < Room.MAX_PLAYERS; i++)
        {
            repository.Players.Add(NewPlayer(room.Id, $"Player{i}", $"hash{i}"));
        }

        var service = CreateService(repository);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreatePlayerAsync(room.Code, new CreatePlayerRequest { DisplayName = "Người mới" }, CancellationToken.None));
    }

    [Fact]
    public async Task JoinRoom_DuplicateName_Conflict()
    {
        var room = NewRoom(RoomStatus.Lobby);
        var repository = new FakeGameRepository { Room = room };
        repository.Players.Add(NewPlayer(room.Id, "Alice"));
        var service = CreateService(repository);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreatePlayerAsync(room.Code, new CreatePlayerRequest { DisplayName = "Alice" }, CancellationToken.None));
    }

    [Fact]
    public async Task JoinRoom_DuplicateNameDifferentCase_Conflict()
    {
        var room = NewRoom(RoomStatus.Lobby);
        var repository = new FakeGameRepository { Room = room };
        repository.Players.Add(NewPlayer(room.Id, "An"));
        var service = CreateService(repository);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreatePlayerAsync(room.Code, new CreatePlayerRequest { DisplayName = "an" }, CancellationToken.None));
    }

    [Fact]
    public async Task JoinRoom_SameNameInAnotherRoom_Succeeds()
    {
        var otherRoom = NewRoom(RoomStatus.Lobby);
        var room = NewRoom(RoomStatus.Lobby);
        var repository = new FakeGameRepository { Room = room };
        repository.Players.Add(NewPlayer(otherRoom.Id, "An"));
        var service = CreateService(repository);

        var response = await service.CreatePlayerAsync(room.Code, new CreatePlayerRequest { DisplayName = "an" }, CancellationToken.None);

        Assert.Equal(room.Code, response.RoomCode);
    }

    [Fact]
    public async Task JoinAsPlayer_Reconnect_KeepsScore()
    {
        var room = NewRoom(RoomStatus.InProgress);
        var player = NewPlayer(room.Id, "Alice", SessionTokenHasher.Hash("real-token"), score: 750);
        var repository = new FakeGameRepository { Room = room };
        repository.Players.Add(player);
        var service = CreateService(repository);

        var (state, _) = await service.JoinAsPlayerAsync(room.Code, "real-token", CancellationToken.None);

        Assert.Equal(750, state.Players.Single(p => p.PlayerId == player.Id).Score);
        Assert.Single(repository.Players); // vào lại không tạo thêm Player mới
        Assert.Equal(Now, state.ServerTime);
    }

    [Fact]
    public async Task StartGame_NotHost_Rejected()
    {
        var room = NewRoom(RoomStatus.Lobby);
        var repository = new FakeGameRepository { Room = room };
        repository.Questions.Add(NewQuestion(room.Id, 0));
        var service = CreateService(repository);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.StartGameAsync(Guid.NewGuid(), room.Code, CancellationToken.None));
    }

    [Fact]
    public async Task NextQuestion_BeforeEnd_Rejected()
    {
        var room = NewRoom(RoomStatus.InProgress);
        var repository = new FakeGameRepository { Room = room };
        repository.Questions.Add(NewQuestion(room.Id, 0, startedAt: Now));
        var service = CreateService(repository);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.NextQuestionAsync(HostUserId, room.Code, CancellationToken.None));
    }

    [Fact]
    public async Task NextQuestion_LastQuestion_FinishesGame()
    {
        var room = NewRoom(RoomStatus.InProgress);
        var repository = new FakeGameRepository { Room = room };
        repository.Questions.Add(NewQuestion(room.Id, 0, startedAt: Now, endedAt: Now.AddSeconds(20)));
        var service = CreateService(repository);

        var outcome = await service.NextQuestionAsync(HostUserId, room.Code, CancellationToken.None);

        Assert.Null(outcome.NextQuestion);
        Assert.NotNull(outcome.GameEnded);
        Assert.Equal(RoomStatus.Finished, room.Status);
    }

    [Fact]
    public async Task NextQuestion_AfterFinished_Conflict()
    {
        var room = NewRoom(RoomStatus.Finished);
        var repository = new FakeGameRepository { Room = room };
        repository.Questions.Add(NewQuestion(room.Id, 0, startedAt: Now, endedAt: Now.AddSeconds(20)));
        var service = CreateService(repository);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.NextQuestionAsync(HostUserId, room.Code, CancellationToken.None));
    }

    [Fact]
    public async Task SubmitAnswer_AfterDeadline_Rejected()
    {
        var room = NewRoom(RoomStatus.InProgress, timeLimitSeconds: 20);
        var question = NewQuestion(room.Id, 0, startedAt: Now);
        var player = NewPlayer(room.Id, "Alice");
        var repository = new FakeGameRepository { Room = room };
        repository.Questions.Add(question);
        repository.Players.Add(player);
        var service = CreateService(repository, Now.AddSeconds(21));

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.SubmitAnswerAsync(player.Id, question.Id, 0, CancellationToken.None));

        Assert.Equal(0, player.Score);
    }

    [Fact]
    public async Task SubmitAnswer_Twice_Rejected()
    {
        var room = NewRoom(RoomStatus.InProgress, timeLimitSeconds: 20);
        var question = NewQuestion(room.Id, 0, startedAt: Now);
        var player = NewPlayer(room.Id, "Alice");
        var repository = new FakeGameRepository { Room = room };
        repository.Questions.Add(question);
        repository.Players.Add(player);
        var service = CreateService(repository, Now.AddSeconds(2));

        await service.SubmitAnswerAsync(player.Id, question.Id, 0, CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.SubmitAnswerAsync(player.Id, question.Id, 0, CancellationToken.None));
    }

    [Fact]
    public async Task SubmitAnswer_WrongQuestion_Rejected()
    {
        var room = NewRoom(RoomStatus.InProgress, timeLimitSeconds: 20);
        var question = NewQuestion(room.Id, 0, startedAt: Now); // câu đang chạy thật
        var player = NewPlayer(room.Id, "Alice");
        var repository = new FakeGameRepository { Room = room };
        repository.Questions.Add(question);
        repository.Players.Add(player);
        var service = CreateService(repository, Now.AddSeconds(2));

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.SubmitAnswerAsync(player.Id, Guid.NewGuid(), 0, CancellationToken.None));
    }

    [Fact]
    public async Task SubmitAnswer_AfterQuestionEnded_RejectedAndNoScoreChange()
    {
        var room = NewRoom(RoomStatus.InProgress, timeLimitSeconds: 20);
        var question = NewQuestion(room.Id, 0, startedAt: Now, endedAt: Now.AddSeconds(5)); // host đã EndQuestion
        var player = NewPlayer(room.Id, "Alice");
        var repository = new FakeGameRepository { Room = room };
        repository.Questions.Add(question);
        repository.Players.Add(player);
        var service = CreateService(repository, Now.AddSeconds(2));

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.SubmitAnswerAsync(player.Id, question.Id, 0, CancellationToken.None));

        Assert.Equal(0, player.Score);
    }

    [Fact]
    public async Task HostEndQuestion_BeforeDeadline_Allowed()
    {
        var room = NewRoom(RoomStatus.InProgress, timeLimitSeconds: 20);
        var question = NewQuestion(room.Id, 0, startedAt: Now);
        var repository = new FakeGameRepository { Room = room };
        repository.Questions.Add(question);
        repository.Players.Add(NewPlayer(room.Id, "Alice")); // chưa ai trả lời, chưa hết giờ
        var service = CreateService(repository, Now.AddSeconds(2));

        var (ended, _) = await service.EndQuestionAsync(HostUserId, room.Code, CancellationToken.None);

        Assert.Equal(Now.AddSeconds(2), question.EndedAt);
        Assert.Equal(Now.AddSeconds(2 + Room.RESULT_DISPLAY_SECONDS), ended.NextQuestionAt);
    }

    [Fact]
    public async Task TryAutoEndQuestion_PastDeadline_EndsQuestion()
    {
        var room = NewRoom(RoomStatus.InProgress, timeLimitSeconds: 20);
        var question = NewQuestion(room.Id, 0, startedAt: Now);
        var repository = new FakeGameRepository { Room = room };
        repository.Questions.Add(question);
        repository.Players.Add(NewPlayer(room.Id, "Alice"));
        var service = CreateService(repository, Now.AddSeconds(20));

        var result = await service.TryAutoEndQuestionAsync(room.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(Now.AddSeconds(20), question.EndedAt);
    }

    [Fact]
    public async Task TryAutoEndQuestion_AlreadyEnded_ReturnsNullNoChange()
    {
        var room = NewRoom(RoomStatus.InProgress, timeLimitSeconds: 20);
        var endedAt = Now.AddSeconds(5); // chủ phòng đã kết thúc trước
        var question = NewQuestion(room.Id, 0, startedAt: Now, endedAt: endedAt);
        var repository = new FakeGameRepository { Room = room };
        repository.Questions.Add(question);
        var service = CreateService(repository, Now.AddSeconds(30));

        var result = await service.TryAutoEndQuestionAsync(room.Id, CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(endedAt, question.EndedAt);
    }

    [Fact]
    public async Task TryAutoAdvance_BeforeDisplayTime_ReturnsNull()
    {
        var room = NewRoom(RoomStatus.InProgress);
        var repository = new FakeGameRepository { Room = room };
        repository.Questions.Add(NewQuestion(room.Id, 0, startedAt: Now, endedAt: Now.AddSeconds(10)));
        repository.Questions.Add(NewQuestion(room.Id, 1));
        var service = CreateService(repository, Now.AddSeconds(10 + Room.RESULT_DISPLAY_SECONDS - 1));

        Assert.Null(await service.TryAutoAdvanceAsync(room.Id, CancellationToken.None));
        Assert.Null(repository.Questions[1].StartedAt);
    }

    [Fact]
    public async Task TryAutoAdvance_LastQuestion_FinishesGame()
    {
        var room = NewRoom(RoomStatus.InProgress);
        var repository = new FakeGameRepository { Room = room };
        repository.Questions.Add(NewQuestion(room.Id, 0, startedAt: Now, endedAt: Now.AddSeconds(10)));
        var service = CreateService(repository, Now.AddSeconds(10 + Room.RESULT_DISPLAY_SECONDS));

        var outcome = await service.TryAutoAdvanceAsync(room.Id, CancellationToken.None);

        Assert.NotNull(outcome?.GameEnded);
        Assert.Equal(RoomStatus.Finished, room.Status);
    }

    [Fact]
    public async Task SubmitAnswer_LastPlayer_EndsQuestionImmediately()
    {
        var room = NewRoom(RoomStatus.InProgress, timeLimitSeconds: 20);
        var question = NewQuestion(room.Id, 0, startedAt: Now);
        var alice = NewPlayer(room.Id, "Alice");
        var bob = NewPlayer(room.Id, "Bob");
        var repository = new FakeGameRepository { Room = room };
        repository.Questions.Add(question);
        repository.Players.AddRange([alice, bob]);
        var service = CreateService(repository, Now.AddSeconds(2));

        await service.SubmitAnswerAsync(alice.Id, question.Id, 0, CancellationToken.None);
        Assert.Null(await service.TryAutoEndQuestionAsync(room.Id, CancellationToken.None)); // còn Bob chưa trả lời

        var (_, progress, roomId) = await service.SubmitAnswerAsync(bob.Id, question.Id, 1, CancellationToken.None);
        var result = await service.TryAutoEndQuestionAsync(roomId, CancellationToken.None);

        Assert.Equal(progress.TotalPlayers, progress.AnsweredCount);
        Assert.NotNull(result);
        Assert.Equal(Now.AddSeconds(2), question.EndedAt);
    }

    [Fact]
    public void QuestionStartedDto_DoesNotExposeCorrectIndex()
    {
        Assert.Null(typeof(QuestionStartedDto).GetProperty("CorrectIndex"));
    }

    private static Room NewRoom(RoomStatus status, int timeLimitSeconds = 20) => new()
    {
        Id = Guid.NewGuid(),
        Code = "ABCDEF",
        HostUserId = HostUserId,
        Topic = "Java OOP",
        Status = status,
        QuestionTimeLimitSeconds = timeLimitSeconds,
        CreatedAt = Now,
    };

    private static Question NewQuestion(
        Guid roomId, int orderIndex, QuestionReviewStatus review = QuestionReviewStatus.Approved,
        DateTimeOffset? startedAt = null, DateTimeOffset? endedAt = null) => new()
    {
        Id = Guid.NewGuid(),
        RoomId = roomId,
        OrderIndex = orderIndex,
        Text = $"Câu {orderIndex}",
        Options = ["A", "B", "C", "D"],
        CorrectIndex = 0,
        ReviewStatus = review,
        StartedAt = startedAt,
        EndedAt = endedAt,
    };

    private static Player NewPlayer(Guid roomId, string name, string tokenHash = "hash", int score = 0) => new()
    {
        Id = Guid.NewGuid(),
        RoomId = roomId,
        DisplayName = name,
        SessionTokenHash = tokenHash,
        Score = score,
        JoinedAt = Now,
    };

    private static GameService CreateService(FakeGameRepository repository, DateTimeOffset? now = null) =>
        new(repository, new FakeSessionTokenGenerator(), new FakeTimeProvider(now ?? Now));

    private sealed class FakeSessionTokenGenerator(string token = "fake-token") : ISessionTokenGenerator
    {
        public string Generate() => token;
    }
}
