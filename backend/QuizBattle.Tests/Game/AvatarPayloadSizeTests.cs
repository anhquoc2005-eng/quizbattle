using System.Text;
using System.Text.Json;
using QuizBattle.Application.Avatars;
using QuizBattle.Application.Game;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;
using QuizBattle.Tests.TestHelpers;
using Xunit.Abstractions;

namespace QuizBattle.Tests.Game;

/// <summary>
/// Đo kích thước JSON thật của payload có nhân vật với phòng 50 người (SignalR dùng System.Text.Json, camelCase) và canh một
/// ngân sách rộng để phát hiện sớm nếu payload phình bất thường. Con số đo được ghi ở contracts/signalr-events.md.
/// </summary>
public sealed class AvatarPayloadSizeTests(ITestOutputHelper output)
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid HostUserId = Guid.NewGuid();
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static int Bytes<T>(T value) => Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(value, Json));

    // Phòng 50 người: nửa là tài khoản đã lưu nhân vật đầy đồ (đủ 11 ô: trường hợp xấu nhất), nửa là khách (mặc định).
    private static (FakeGameRepository Repository, Room Room) FullRoom(bool fullyEquipped)
    {
        var room = new Room
        {
            Id = Guid.NewGuid(), Code = "ABCDEF", HostUserId = HostUserId, Topic = "Chủ đề", Status = RoomStatus.InProgress,
            QuestionTimeLimitSeconds = 20, CreatedAt = Now,
        };
        var repository = new FakeGameRepository { Room = room };
        repository.Questions.Add(new Question
        {
            Id = Guid.NewGuid(), RoomId = room.Id, OrderIndex = 0, Text = "Câu 0", Options = ["A", "B", "C", "D"], CorrectIndex = 0,
            ReviewStatus = QuestionReviewStatus.Approved, StartedAt = Now,
        });
        for (var i = 0; i < Room.MAX_PLAYERS; i++)
        {
            Guid? userId = null;
            if (i % 2 == 0)
            {
                userId = Guid.NewGuid();
                repository.Users.Add(new User { Id = userId.Value, Email = $"{userId:N}@t.local", PasswordHash = "h", DisplayName = "TK", CreatedAt = Now });
                if (fullyEquipped)
                {
                    repository.UserAvatars[userId.Value] = new UserAvatar
                    {
                        UserId = userId.Value, EyesItemKey = "eyes.variant05", EyebrowsItemKey = "eyebrows.variant13", MouthItemKey = "mouth.variant30",
                        NoseItemKey = "nose.variant20", HairItemKey = "hair.variant62", ClothesItemKey = "clothes.variant10",
                        GlassesItemKey = "glasses.variant11", BeardItemKey = "beard.variant12", GestureItemKey = "gesture.wavePointLongArms",
                        ClothesGraphicItemKey = "clothesGraphic.galaxy", BackgroundItemKey = "background.cfc4e3", UpdatedAt = Now,
                    };
                }
            }

            repository.Players.Add(new Player
            {
                Id = Guid.NewGuid(), RoomId = room.Id, UserId = userId, DisplayName = $"Người chơi {i + 1:00}", SessionTokenHash = $"h{i}",
                Score = 1000 - i * 10, JoinedAt = Now,
            });
        }

        return (repository, room);
    }

    private static GameService Service(FakeGameRepository repository) =>
        new(repository, new Fake(), new FakeTimeProvider(Now));

    private sealed class Fake : ISessionTokenGenerator
    {
        public string Generate() => "t";
    }

    [Fact]
    public async Task RoomState_FiftyPlayers_AvatarPayloadIsWithinBudget()
    {
        foreach (var equipped in new[] { false, true })
        {
            var (repository, room) = FullRoom(equipped);
            var state = await Service(repository).JoinAsHostAsync(HostUserId, room.Code, CancellationToken.None);

            var total = Bytes(state);
            var avatarBytes = state.Players.Sum(p => Bytes(p.Avatar) + ",\"avatar\":".Length) + state.Leaderboard.Sum(e => Bytes(e.Avatar) + ",\"avatar\":".Length);
            output.WriteLine($"RoomState 50 người ({(equipped ? "tài khoản đầy đồ 11 ô + khách mặc định" : "toàn bộ nhân vật mặc định")}): tổng {total:N0} byte; phần nhân vật {avatarBytes:N0} byte ({state.Players.Count} + {state.Leaderboard.Count} avatar); trung bình {avatarBytes / (state.Players.Count + state.Leaderboard.Count)} byte/avatar");

            Assert.Equal(100, state.Players.Count + state.Leaderboard.Count);
            Assert.InRange(avatarBytes, 1, 40_000); // ước tính ≈ 21 KB; ngân sách rộng 40 KB
        }
    }

    [Fact]
    public async Task LeaderboardUpdatedAndGameEnded_FiftyPlayers_AvatarPayloadIsWithinBudget()
    {
        var (repository, room) = FullRoom(fullyEquipped: true);
        var service = Service(repository);

        var (_, updated) = await service.EndQuestionAsync(HostUserId, room.Code, CancellationToken.None);
        var avatarBytes = updated.Leaderboard.Sum(e => Bytes(e.Avatar) + ",\"avatar\":".Length);
        output.WriteLine($"LeaderboardUpdated 50 người: tổng {Bytes(updated):N0} byte; phần nhân vật {avatarBytes:N0} byte");

        var outcome = await service.NextQuestionAsync(HostUserId, room.Code, CancellationToken.None);
        var endedBytes = outcome.GameEnded!.FinalLeaderboard.Sum(e => Bytes(e.Avatar) + ",\"avatar\":".Length);
        output.WriteLine($"GameEnded 50 người: tổng {Bytes(outcome.GameEnded):N0} byte; phần nhân vật trong bảng điểm {endedBytes:N0} byte");

        Assert.InRange(avatarBytes, 1, 20_000); // ước tính ≈ 10,5 KB
        Assert.InRange(endedBytes, 1, 20_000);
    }

    [Fact]
    public void SingleAvatar_TypicalAndWorstCaseSize()
    {
        var typical = new AvatarDto("variant03", "variant05", "variant12", "variant07", "variant12", "variant08", null, null, null, null, "d4d4d4");
        var worst = new AvatarDto("variant05", "variant13", "variant30", "variant20", "variant62", "variant10", "variant11", "variant12", "wavePointLongArms", "electric", "cfc4e3");

        output.WriteLine($"AvatarDto: điển hình (4 ô trống) {Bytes(typical)} byte; đầy đồ 11 ô, kiểu dài nhất {Bytes(worst)} byte");

        Assert.InRange(Bytes(typical), 150, 260);
        Assert.InRange(Bytes(worst), 200, 330);
    }
}
