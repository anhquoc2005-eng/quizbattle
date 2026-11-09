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

    // ---- Thưởng cuối trận (US1) ----

    [Fact]
    public async Task AdvanceToFinished_AwardsEachEligiblePlayerOnce()
    {
        var repository = FinalQuestionRoom(out var room);
        var (a, userA) = AddAccount(repository, room, "A", score: 800);
        var (b, userB) = AddAccount(repository, room, "B", score: 600);
        var service = CreateService(repository, FinishAt);

        await service.NextQuestionAsync(HostUserId, room.Code, CancellationToken.None);

        Assert.Equal(RoomStatus.Finished, room.Status);
        Assert.Equal(2, repository.MatchRewards.Count);
        Assert.All(repository.MatchRewards, r => Assert.Equal(RewardStatus.Awarded, r.Status));
        Assert.Equal(80, userA.TotalXp);
        Assert.Equal(60, userB.TotalXp);
        Assert.Contains(repository.MatchRewards, r => r.PlayerId == a.Id);
        Assert.Contains(repository.MatchRewards, r => r.PlayerId == b.Id);
    }

    [Fact]
    public async Task NextQuestion_CalledTwiceAtEnd_SecondConflictAndNoDoubleReward()
    {
        var repository = FinalQuestionRoom(out var room);
        var (_, userA) = AddAccount(repository, room, "A", score: 800);
        AddAccount(repository, room, "B", score: 600);
        var service = CreateService(repository, FinishAt);

        await service.NextQuestionAsync(HostUserId, room.Code, CancellationToken.None);
        await Assert.ThrowsAsync<ConflictException>(() =>
            service.NextQuestionAsync(HostUserId, room.Code, CancellationToken.None));

        Assert.Equal(2, repository.MatchRewards.Count);
        Assert.Equal(2, repository.CoinTransactions.Count);
        Assert.Equal(80, userA.TotalXp);
    }

    [Fact]
    public async Task TryAutoAdvance_AfterHostFinished_ReturnsNullAndNoDoubleReward()
    {
        var repository = FinalQuestionRoom(out var room);
        var (_, userA) = AddAccount(repository, room, "A", score: 800);
        AddAccount(repository, room, "B", score: 600);
        var service = CreateService(repository, FinishAt);

        await service.NextQuestionAsync(HostUserId, room.Code, CancellationToken.None);
        var auto = await service.TryAutoAdvanceAsync(room.Id, CancellationToken.None);

        Assert.Null(auto);
        Assert.Equal(2, repository.MatchRewards.Count);
        Assert.Equal(80, userA.TotalXp);
    }

    [Fact]
    public async Task AdvanceToFinished_RewardsAndStatusPersistedTogether()
    {
        var repository = FinalQuestionRoom(out var room);
        var (_, userA) = AddAccount(repository, room, "A", score: 800);
        AddAccount(repository, room, "B", score: 600);
        var service = CreateService(repository, FinishAt);
        repository.FailBeforeCommit = () => throw new InvalidOperationException("SaveChanges lỗi");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.NextQuestionAsync(HostUserId, room.Code, CancellationToken.None));

        Assert.Equal(RoomStatus.InProgress, room.Status);
        Assert.Equal(0, userA.TotalXp);
        Assert.Empty(repository.MatchRewards);
        Assert.Empty(repository.CoinTransactions);

        repository.FailBeforeCommit = null; // thử lại sau lỗi tạm thời: vẫn thưởng đầy đủ, đúng một lần
        await service.NextQuestionAsync(HostUserId, room.Code, CancellationToken.None);
        Assert.Equal(2, repository.MatchRewards.Count);
        Assert.Equal(80, userA.TotalXp);
    }

    [Fact]
    public async Task NextQuestionAndAutoAdvance_RunConcurrently_RewardedExactlyOnce()
    {
        var repository = FinalQuestionRoom(out var room);
        var (_, userA) = AddAccount(repository, room, "A", score: 800);
        AddAccount(repository, room, "B", score: 600);
        var service = CreateService(repository, FinishAt);

        var host = Capture(() => service.NextQuestionAsync(HostUserId, room.Code, CancellationToken.None));
        var timer = Capture(() => service.TryAutoAdvanceAsync(room.Id, CancellationToken.None));
        await Task.WhenAll(host, timer);

        Assert.Equal(2, repository.MatchRewards.Count);
        Assert.Equal(2, repository.CoinTransactions.Count);
        Assert.Equal(80, userA.TotalXp);
    }

    [Fact]
    public async Task GameEnded_RewardsMatchFormula()
    {
        var repository = FinalQuestionRoom(out var room);
        var (a, userA) = AddAccount(repository, room, "A", score: 999);
        var (b, userB) = AddAccount(repository, room, "B", score: 250);
        var service = CreateService(repository, FinishAt);

        var outcome = await service.NextQuestionAsync(HostUserId, room.Code, CancellationToken.None);

        var ra = RewardOf(outcome, a);
        var rb = RewardOf(outcome, b);
        Assert.True(ra.Awarded);
        Assert.Equal((99, 9, 30, 1), (ra.Xp, ra.Coins, ra.RankBonus, ra.RewardRank));
        Assert.Equal((25, 2, 20, 2), (rb.Xp, rb.Coins, rb.RankBonus, rb.RewardRank));
        Assert.Equal(99, userA.TotalXp);
        Assert.Equal(9 + 30, userA.CoinBalance);
        Assert.Equal(25, userB.TotalXp);
        Assert.Equal(2 + 20, userB.CoinBalance);
        Assert.Equal(99, ra.TotalXp);
    }

    [Fact]
    public async Task GameEnded_LevelUp_FlagsLeveledUp()
    {
        var repository = FinalQuestionRoom(out var room);
        var (a, _) = AddAccount(repository, room, "A", score: 800, totalXp: 190); // 190 + 80 = 270 -> cấp 2
        var (b, _) = AddAccount(repository, room, "B", score: 600, totalXp: 0);
        var service = CreateService(repository, FinishAt);

        var outcome = await service.NextQuestionAsync(HostUserId, room.Code, CancellationToken.None);

        var ra = RewardOf(outcome, a);
        Assert.True(ra.LeveledUp);
        Assert.Equal(1, ra.LevelBefore);
        Assert.Equal(2, ra.LevelAfter);
        Assert.Equal(270, ra.TotalXp);
        Assert.False(RewardOf(outcome, b).LeveledUp);
    }

    [Fact]
    public async Task GameEnded_CoinTransactionHasReasonAmountRoom()
    {
        var repository = FinalQuestionRoom(out var room);
        var (a, userA) = AddAccount(repository, room, "A", score: 800);
        AddAccount(repository, room, "B", score: 600);
        var service = CreateService(repository, FinishAt);

        await service.NextQuestionAsync(HostUserId, room.Code, CancellationToken.None);

        var reward = repository.MatchRewards.Single(r => r.PlayerId == a.Id);
        var transaction = repository.CoinTransactions.Single(c => c.UserId == userA.Id);
        Assert.Equal(CoinTransactionReason.MatchReward, transaction.Reason);
        Assert.Equal(8 + 30, transaction.Amount);
        Assert.Equal(room.Id, transaction.RoomId);
        Assert.Equal(reward.Id, transaction.MatchRewardId);
        Assert.Equal(FinishAt, transaction.CreatedAt);
    }

    [Fact]
    public async Task GameEnded_ZeroCoins_NoCoinTransactionCreated()
    {
        var repository = FinalQuestionRoom(out var room);
        AddAccount(repository, room, "A", score: 0);
        AddAccount(repository, room, "B", score: 0);
        var service = CreateService(repository, FinishAt);

        await service.NextQuestionAsync(HostUserId, room.Code, CancellationToken.None);

        Assert.Equal(2, repository.MatchRewards.Count); // vẫn có dòng thưởng (Awarded, 0/0/0)
        Assert.Empty(repository.CoinTransactions);
    }

    [Fact]
    public async Task GameEnded_ZeroScoreTopRank_NoRankBonusReasonZeroScore()
    {
        var repository = FinalQuestionRoom(out var room);
        var (a, _) = AddAccount(repository, room, "A", score: 0);
        AddAccount(repository, room, "B", score: 0);
        var service = CreateService(repository, FinishAt);

        var outcome = await service.NextQuestionAsync(HostUserId, room.Code, CancellationToken.None);

        var ra = RewardOf(outcome, a);
        Assert.True(ra.Awarded);
        Assert.Equal(0, ra.RankBonus);
        Assert.Equal("zero_score", ra.NoRankBonusReason);
    }

    [Fact]
    public async Task GameEnded_RewardRowsCopyRoomCodeTopicFinishedAt()
    {
        var repository = FinalQuestionRoom(out var room);
        AddAccount(repository, room, "A", score: 800);
        AddAccount(repository, room, "B", score: 600);
        var service = CreateService(repository, FinishAt);

        await service.NextQuestionAsync(HostUserId, room.Code, CancellationToken.None);

        Assert.All(repository.MatchRewards, r =>
        {
            Assert.Equal(room.Code, r.RoomCode);
            Assert.Equal(room.Topic, r.RoomTopic);
            Assert.Equal(room.EndedAt, r.FinishedAt);
            Assert.Equal(room.Id, r.RoomId);
        });
    }

    [Fact]
    public async Task GameEnded_PlayerLeftAfterAnswering_StillRewarded()
    {
        // LeaveRoom chỉ phát PlayerLeft, không xóa Player: người đã trả lời rồi rời đi vẫn nằm trong snapshot.
        var repository = FinalQuestionRoom(out var room);
        var (leaver, userLeaver) = AddAccount(repository, room, "Leaver", score: 120);
        AddAccount(repository, room, "Stayer", score: 600);
        var service = CreateService(repository, FinishAt);

        var outcome = await service.NextQuestionAsync(HostUserId, room.Code, CancellationToken.None);

        Assert.True(RewardOf(outcome, leaver).Awarded);
        Assert.Equal(12, userLeaver.TotalXp);
    }

    [Fact]
    public async Task Finished_OldRoomWithoutRewards_NotRewardedOnRoomState()
    {
        var room = NewRoom(RoomStatus.Finished);
        var repository = new FakeGameRepository { Room = room };
        repository.Questions.Add(NewQuestion(room.Id, 0, startedAt: Now, endedAt: Now.AddSeconds(20)));
        repository.Players.Add(NewPlayer(room.Id, "Cũ 1", tokenHash: "h1", score: 900));
        repository.Players.Add(NewPlayer(room.Id, "Cũ 2", tokenHash: "h2", score: 800));
        var service = CreateService(repository);

        var state = await service.JoinAsHostAsync(HostUserId, room.Code, CancellationToken.None);

        Assert.NotNull(state.Rewards);
        Assert.Equal(2, state.Rewards.Count);
        Assert.All(state.Rewards, r =>
        {
            Assert.False(r.Awarded);
            Assert.Equal("guest", r.NotAwardedReason);
        });
        Assert.Empty(repository.MatchRewards);
    }

    [Fact]
    public async Task RoomState_Finished_ReturnsStoredRewards()
    {
        var repository = FinalQuestionRoom(out var room);
        var (a, _) = AddAccount(repository, room, "A", score: 800);
        AddAccount(repository, room, "B", score: 600);
        var guest = NewPlayer(room.Id, "Khách", tokenHash: "guest", score: 900);
        repository.Players.Add(guest);
        var service = CreateService(repository, FinishAt);
        var outcome = await service.NextQuestionAsync(HostUserId, room.Code, CancellationToken.None);

        var state = await service.JoinAsHostAsync(HostUserId, room.Code, CancellationToken.None);

        Assert.NotNull(state.Rewards);
        Assert.Equal(3, state.Rewards.Count);
        var stored = state.Rewards.Single(r => r.PlayerId == a.Id);
        var live = RewardOf(outcome, a);
        Assert.Equal((live.Awarded, live.Xp, live.Coins, live.RankBonus, live.RewardRank), (stored.Awarded, stored.Xp, stored.Coins, stored.RankBonus, stored.RewardRank));
        Assert.Equal("guest", state.Rewards.Single(r => r.PlayerId == guest.Id).NotAwardedReason);
    }

    [Fact]
    public async Task RoomState_NotFinished_RewardsIsNull()
    {
        var room = NewRoom(RoomStatus.Lobby);
        var service = CreateService(new FakeGameRepository { Room = room });

        var state = await service.JoinAsHostAsync(HostUserId, room.Code, CancellationToken.None);

        Assert.Null(state.Rewards);
    }

    [Fact]
    public void QuestionStartedDto_DoesNotExposeCorrectIndex()
    {
        Assert.Null(typeof(QuestionStartedDto).GetProperty("CorrectIndex"));
    }

    // Câu cuối đã kết thúc lúc Now + 10s; FinishAt đủ muộn để cả chủ phòng lẫn timer chuyển sang Finished.
    private static readonly DateTimeOffset FinishAt = Now.AddSeconds(10 + Room.RESULT_DISPLAY_SECONDS + 1);

    private static FakeGameRepository FinalQuestionRoom(out Room room)
    {
        room = NewRoom(RoomStatus.InProgress);
        var repository = new FakeGameRepository { Room = room };
        repository.Questions.Add(NewQuestion(room.Id, 0, startedAt: Now, endedAt: Now.AddSeconds(10)));
        return repository;
    }

    // Người chơi có tài khoản, đã trả lời câu duy nhất của phòng (AnsweredCount = 1).
    private static (Player Player, User User) AddAccount(FakeGameRepository repository, Room room, string name, int score, int totalXp = 0)
    {
        var user = new User
        {
            Id = Guid.NewGuid(), Email = $"{name}@test.local", PasswordHash = "h", DisplayName = name, CreatedAt = Now, TotalXp = totalXp,
        };
        var player = NewPlayer(room.Id, name, tokenHash: $"hash-{name}", score: score);
        player.UserId = user.Id;
        repository.Users.Add(user);
        repository.Players.Add(player);
        repository.Answers.Add(new Answer
        {
            Id = Guid.NewGuid(), PlayerId = player.Id, QuestionId = repository.Questions[0].Id, SelectedIndex = 0,
            AnsweredAt = Now.AddSeconds(1), IsCorrect = score > 0, PointsAwarded = score,
        });
        return (player, user);
    }

    private static PlayerRewardDto RewardOf(NextQuestionOutcome outcome, Player player) =>
        Assert.Single(outcome.GameEnded!.Rewards, r => r.PlayerId == player.Id);

    private static async Task Capture(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (ConflictException)
        {
            // Người đến sau bị từ chối vì trận đã kết thúc: đúng hành vi mong đợi.
        }
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
