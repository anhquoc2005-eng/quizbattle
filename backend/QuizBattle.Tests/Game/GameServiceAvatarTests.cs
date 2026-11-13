using QuizBattle.Application.Avatars;
using QuizBattle.Application.Game;
using QuizBattle.Domain.Avatars;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;
using QuizBattle.Tests.TestHelpers;

namespace QuizBattle.Tests.Game;

/// <summary>Mọi DTO phòng đều mang nhân vật đủ ô: đã lưu, mặc định theo tài khoản, hoặc mặc định theo tên (khách).</summary>
public sealed class GameServiceAvatarTests
{
    private static readonly Guid HostUserId = Guid.NewGuid();
    private static readonly Guid AccountWithAvatar = Guid.Parse("c0000000-0000-0000-0000-00000000000a");
    private static readonly Guid AccountWithoutRow = Guid.Parse("c0000000-0000-0000-0000-00000000000b");
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static AvatarDto Dto(AvatarSelection s) => new(
        s.Get(AvatarSlot.Eyes)!, s.Get(AvatarSlot.Eyebrows)!, s.Get(AvatarSlot.Mouth)!, s.Get(AvatarSlot.Nose)!,
        s.Get(AvatarSlot.Hair)!, s.Get(AvatarSlot.Clothes)!, s.Get(AvatarSlot.Glasses), s.Get(AvatarSlot.Beard),
        s.Get(AvatarSlot.Gesture), s.Get(AvatarSlot.ClothesGraphic), s.Get(AvatarSlot.Background));

    private static readonly AvatarDto SavedDto = new(
        "variant02", "variant03", "variant04", "variant05", "variant62", "variant07", null, null, "ok", null, "b9c6d6");

    private static UserAvatar SavedAvatar(Guid userId) => new()
    {
        UserId = userId, EyesItemKey = "eyes.variant02", EyebrowsItemKey = "eyebrows.variant03", MouthItemKey = "mouth.variant04",
        NoseItemKey = "nose.variant05", HairItemKey = "hair.variant62", ClothesItemKey = "clothes.variant07",
        GestureItemKey = "gesture.ok", BackgroundItemKey = "background.b9c6d6", UpdatedAt = Now,
    };

    private static User NewUser(Guid id) => new()
    {
        Id = id, Email = $"{id:N}@test.local", PasswordHash = "h", DisplayName = "TK", CreatedAt = Now,
    };

    private static Player NewPlayer(Guid roomId, string name, Guid? userId = null, int score = 0, string hash = "hash") => new()
    {
        Id = Guid.NewGuid(), RoomId = roomId, UserId = userId, DisplayName = name, SessionTokenHash = hash, Score = score, JoinedAt = Now,
    };

    private static Room NewRoom(RoomStatus status) => new()
    {
        Id = Guid.NewGuid(), Code = "ABCDEF", HostUserId = HostUserId, Topic = "Chủ đề", Status = status, QuestionTimeLimitSeconds = 20, CreatedAt = Now,
    };

    private static Question NewQuestion(Guid roomId, DateTimeOffset? startedAt = null, DateTimeOffset? endedAt = null) => new()
    {
        Id = Guid.NewGuid(), RoomId = roomId, OrderIndex = 0, Text = "Câu 0", Options = ["A", "B", "C", "D"], CorrectIndex = 0,
        ReviewStatus = QuestionReviewStatus.Approved, StartedAt = startedAt, EndedAt = endedAt,
    };

    private static GameService CreateService(FakeGameRepository repository) =>
        new(repository, new FakeSessionTokenGenerator(), new FakeTimeProvider(Now));

    // Phòng đang chơi: một tài khoản có nhân vật đã lưu, một tài khoản chưa từng lưu, một khách; câu 0 đang chạy.
    private static (FakeGameRepository Repository, Room Room, Player Saved, Player NoRow, Player Guest) InProgressRoom()
    {
        var room = NewRoom(RoomStatus.InProgress);
        var repository = new FakeGameRepository { Room = room };
        repository.Questions.Add(NewQuestion(room.Id, startedAt: Now));
        repository.Users.AddRange([NewUser(AccountWithAvatar), NewUser(AccountWithoutRow)]);
        repository.UserAvatars[AccountWithAvatar] = SavedAvatar(AccountWithAvatar);
        var saved = NewPlayer(room.Id, "Có nhân vật", AccountWithAvatar, score: 300);
        var noRow = NewPlayer(room.Id, "Chưa lưu", AccountWithoutRow, score: 200);
        var guest = NewPlayer(room.Id, "Khách An", userId: null, score: 100);
        repository.Players.AddRange([saved, noRow, guest]);
        return (repository, room, saved, noRow, guest);
    }

    private static readonly AvatarDto ExpectedNoRow = Dto(DefaultAvatarGenerator.ForAccount(AccountWithoutRow));
    private static readonly AvatarDto ExpectedGuest = Dto(DefaultAvatarGenerator.ForGuest("Khách An"));

    [Fact]
    public async Task JoinAsPlayer_Broadcast_HasAvatar()
    {
        var room = NewRoom(RoomStatus.Lobby);
        var repository = new FakeGameRepository { Room = room };
        repository.Players.Add(NewPlayer(room.Id, "Khách An", hash: SessionTokenHasher.Hash("tok")));
        var service = CreateService(repository);

        var (state, broadcast, _) = await service.JoinAsPlayerAsync(room.Code, "tok", CancellationToken.None);

        Assert.Equal(ExpectedGuest, broadcast.Avatar);
        Assert.Equal(ExpectedGuest, Assert.Single(state.Players).Avatar);
    }

    [Fact]
    public async Task JoinAsPlayer_AccountWithSavedAvatar_BroadcastShowsSaved()
    {
        var room = NewRoom(RoomStatus.Lobby);
        var repository = new FakeGameRepository { Room = room };
        repository.Users.Add(NewUser(AccountWithAvatar));
        repository.UserAvatars[AccountWithAvatar] = SavedAvatar(AccountWithAvatar);
        repository.Players.Add(NewPlayer(room.Id, "Có nhân vật", AccountWithAvatar, hash: SessionTokenHasher.Hash("tok")));
        var service = CreateService(repository);

        var (_, broadcast, _) = await service.JoinAsPlayerAsync(room.Code, "tok", CancellationToken.None);

        Assert.Equal(SavedDto, broadcast.Avatar);
    }

    [Fact]
    public async Task RoomState_Players_AllHaveFullAvatar()
    {
        var (repository, room, saved, noRow, guest) = InProgressRoom();

        var state = await CreateService(repository).JoinAsHostAsync(HostUserId, room.Code, CancellationToken.None);

        var byId = state.Players.ToDictionary(p => p.PlayerId);
        Assert.Equal(SavedDto, byId[saved.Id].Avatar);
        Assert.Equal(ExpectedNoRow, byId[noRow.Id].Avatar);
        Assert.Equal(ExpectedGuest, byId[guest.Id].Avatar);
        Assert.All(state.Players, p => AssertFull(p.Avatar));
    }

    [Fact]
    public async Task RoomState_Leaderboard_AllHaveFullAvatar()
    {
        var (repository, room, saved, noRow, guest) = InProgressRoom();

        var state = await CreateService(repository).JoinAsHostAsync(HostUserId, room.Code, CancellationToken.None);

        var byId = state.Leaderboard.ToDictionary(e => e.PlayerId);
        Assert.Equal(SavedDto, byId[saved.Id].Avatar);
        Assert.Equal(ExpectedNoRow, byId[noRow.Id].Avatar);
        Assert.Equal(ExpectedGuest, byId[guest.Id].Avatar);
        Assert.All(state.Leaderboard, e => AssertFull(e.Avatar));
    }

    [Fact]
    public async Task LeaderboardUpdated_HasAvatars()
    {
        var (repository, room, saved, noRow, guest) = InProgressRoom();

        var (_, leaderboard) = await CreateService(repository).EndQuestionAsync(HostUserId, room.Code, CancellationToken.None);

        var byId = leaderboard.Leaderboard.ToDictionary(e => e.PlayerId);
        Assert.Equal(SavedDto, byId[saved.Id].Avatar);
        Assert.Equal(ExpectedNoRow, byId[noRow.Id].Avatar);
        Assert.Equal(ExpectedGuest, byId[guest.Id].Avatar);
    }

    [Fact]
    public async Task GameEnded_FinalLeaderboard_HasAvatars()
    {
        var (repository, room, saved, noRow, guest) = InProgressRoom();
        repository.Questions[0].EndedAt = Now.AddSeconds(5);
        repository.Answers.AddRange(
        [
            new Answer { Id = Guid.NewGuid(), PlayerId = saved.Id, QuestionId = repository.Questions[0].Id, SelectedIndex = 0, AnsweredAt = Now.AddSeconds(2), IsCorrect = true, PointsAwarded = 300 },
            new Answer { Id = Guid.NewGuid(), PlayerId = noRow.Id, QuestionId = repository.Questions[0].Id, SelectedIndex = 0, AnsweredAt = Now.AddSeconds(3), IsCorrect = true, PointsAwarded = 200 },
        ]);

        var outcome = await CreateService(repository).NextQuestionAsync(HostUserId, room.Code, CancellationToken.None);

        var byId = outcome.GameEnded!.FinalLeaderboard.ToDictionary(e => e.PlayerId);
        Assert.Equal(SavedDto, byId[saved.Id].Avatar);
        Assert.Equal(ExpectedNoRow, byId[noRow.Id].Avatar);
        Assert.Equal(ExpectedGuest, byId[guest.Id].Avatar);
    }

    [Fact]
    public async Task TwoGuestsWithSameName_InDifferentRooms_HaveSameAvatar()
    {
        async Task<AvatarDto> GuestAvatarAsync()
        {
            var (repository, room, _, _, guest) = InProgressRoom();
            var state = await CreateService(repository).JoinAsHostAsync(HostUserId, room.Code, CancellationToken.None);
            return state.Players.Single(p => p.PlayerId == guest.Id).Avatar;
        }

        Assert.Equal(await GuestAvatarAsync(), await GuestAvatarAsync());
    }

    [Fact]
    public async Task Guest_AvatarIsFreeItemsOnlyWithGreyBackground()
    {
        var (repository, room, _, _, guest) = InProgressRoom();

        var state = await CreateService(repository).JoinAsHostAsync(HostUserId, room.Code, CancellationToken.None);

        var avatar = state.Players.Single(p => p.PlayerId == guest.Id).Avatar;
        Assert.Equal("d4d4d4", avatar.Background);
        Assert.Null(avatar.Gesture);
        Assert.DoesNotContain(avatar.Hair, AvatarCatalog.HeadscarfVariantKeys);
    }

    [Fact]
    public async Task AvatarChangedBetweenSnapshots_NextSnapshotShowsNew()
    {
        var (repository, room, saved, _, _) = InProgressRoom();
        var service = CreateService(repository);

        var before = await service.JoinAsHostAsync(HostUserId, room.Code, CancellationToken.None);
        repository.UserAvatars[AccountWithAvatar].HairItemKey = "hair.variant01"; // người chơi đổi đồ ở nơi khác
        var after = await service.JoinAsHostAsync(HostUserId, room.Code, CancellationToken.None);

        Assert.Equal("variant62", before.Players.Single(p => p.PlayerId == saved.Id).Avatar.Hair);
        Assert.Equal("variant01", after.Players.Single(p => p.PlayerId == saved.Id).Avatar.Hair); // nhân vật hiện tại, không phải bản chụp
    }

    [Fact]
    public async Task ChangingAvatar_DoesNotChangeScoresRanksOrRewards()
    {
        static async Task<(IReadOnlyList<(Guid, string, int, int)> Board, IReadOnlyList<PlayerRewardDto> Rewards)> RunAsync(string hair)
        {
            var (repository, room, saved, noRow, _) = InProgressRoom();
            repository.UserAvatars[AccountWithAvatar].HairItemKey = hair;
            repository.Questions[0].EndedAt = Now.AddSeconds(5);
            repository.Answers.AddRange(
            [
                new Answer { Id = Guid.NewGuid(), PlayerId = saved.Id, QuestionId = repository.Questions[0].Id, SelectedIndex = 0, AnsweredAt = Now.AddSeconds(2), IsCorrect = true, PointsAwarded = 300 },
                new Answer { Id = Guid.NewGuid(), PlayerId = noRow.Id, QuestionId = repository.Questions[0].Id, SelectedIndex = 0, AnsweredAt = Now.AddSeconds(3), IsCorrect = true, PointsAwarded = 200 },
            ]);
            var ended = (await CreateService(repository).NextQuestionAsync(HostUserId, room.Code, CancellationToken.None)).GameEnded!;
            return (ended.FinalLeaderboard.Select(e => (e.PlayerId, e.DisplayName, e.Score, e.Rank)).OrderBy(e => e.Item2).ToList(), ended.Rewards);
        }

        var (boardA, rewardsA) = await RunAsync("hair.variant62");
        var (boardB, rewardsB) = await RunAsync("hair.variant01");

        // PlayerId là Guid mới mỗi lần dựng phòng nên so theo tên, điểm, hạng; thưởng so theo giá trị không phụ thuộc PlayerId.
        Assert.Equal(boardA.Select(e => (e.Item2, e.Item3, e.Item4)), boardB.Select(e => (e.Item2, e.Item3, e.Item4)));
        Assert.Equal(
            rewardsA.Select(r => (r.Awarded, r.RewardRank, r.Score, r.Xp, r.Coins, r.RankBonus)).OrderBy(r => r.Score),
            rewardsB.Select(r => (r.Awarded, r.RewardRank, r.Score, r.Xp, r.Coins, r.RankBonus)).OrderBy(r => r.Score));
    }

    private static void AssertFull(AvatarDto avatar)
    {
        Assert.NotNull(avatar);
        Assert.All(
            new[] { avatar.Eyes, avatar.Eyebrows, avatar.Mouth, avatar.Nose, avatar.Hair, avatar.Clothes },
            v => Assert.False(string.IsNullOrEmpty(v)));
    }

    private sealed class FakeSessionTokenGenerator : ISessionTokenGenerator
    {
        public string Generate() => "fake-token";
    }
}
