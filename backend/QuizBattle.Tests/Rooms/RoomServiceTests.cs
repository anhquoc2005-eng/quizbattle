using QuizBattle.Application.Common;
using QuizBattle.Application.Rooms;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Tests.Rooms;

public sealed class RoomServiceTests
{
    [Fact]
    public async Task CreateRoom_TrimsTopicAndStartsAsDraft()
    {
        var repository = new FakeRoomRepository();
        var service = CreateService(repository, new FakeRoomCodeGenerator("ABCDEF"));

        var result = await service.CreateRoomAsync(
            Guid.NewGuid(),
            new CreateRoomRequest { Topic = "  Java OOP  " },
            CancellationToken.None);

        Assert.Equal("Java OOP", repository.AddedRooms.Single().Topic);
        Assert.Equal(RoomStatus.Draft, repository.AddedRooms.Single().Status);
        Assert.Equal("Java OOP", result.Topic);
        Assert.Equal("Draft", result.Status);
        Assert.Equal(20, result.QuestionTimeLimitSeconds);
    }

    [Fact]
    public async Task CreateRoom_CodeCollision_RetriesWithNewCode()
    {
        var repository = new FakeRoomRepository(collisionsBeforeSuccess: 1);
        var service = CreateService(repository, new FakeRoomCodeGenerator("AAAAAA", "BBBBBB"));

        var result = await service.CreateRoomAsync(
            Guid.NewGuid(),
            new CreateRoomRequest { Topic = "Java" },
            CancellationToken.None);

        Assert.Equal(2, repository.AddAttempts);
        Assert.Equal("BBBBBB", result.Code);
        Assert.Equal("BBBBBB", repository.AddedRooms.Single().Code);
    }

    [Fact]
    public async Task CreateRoom_FiveCollisions_Throws()
    {
        var repository = new FakeRoomRepository(collisionsBeforeSuccess: int.MaxValue);
        var service = CreateService(repository, new FakeRoomCodeGenerator("AAAAAA", "BBBBBB", "CCCCCC", "DDDDDD", "EEEEEE"));

        await Assert.ThrowsAsync<RoomCodeCollisionException>(() => service.CreateRoomAsync(
            Guid.NewGuid(),
            new CreateRoomRequest { Topic = "Java" },
            CancellationToken.None));

        Assert.Equal(5, repository.AddAttempts);
        Assert.Empty(repository.AddedRooms);
    }

    [Fact]
    public async Task GetRoom_OtherUsersRoom_ThrowsNotFound()
    {
        var owner = Guid.NewGuid();
        var repository = new FakeRoomRepository();
        repository.Seed(owner, new Room { Id = Guid.NewGuid(), Code = "ABCDEF", HostUserId = owner, Topic = "Java", Status = RoomStatus.Draft, QuestionTimeLimitSeconds = 20, CreatedAt = DateTimeOffset.UtcNow });
        var service = CreateService(repository, new FakeRoomCodeGenerator());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetRoomAsync(Guid.NewGuid(), "ABCDEF", CancellationToken.None));
    }

    [Fact]
    public async Task DeleteRoom_InLobby_ThrowsConflict()
    {
        var owner = Guid.NewGuid();
        var repository = new FakeRoomRepository();
        repository.Seed(owner, new Room { Id = Guid.NewGuid(), Code = "ABCDEF", HostUserId = owner, Topic = "Java", Status = RoomStatus.Lobby, QuestionTimeLimitSeconds = 20, CreatedAt = DateTimeOffset.UtcNow });
        var service = CreateService(repository, new FakeRoomCodeGenerator());

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.DeleteRoomAsync(owner, "ABCDEF", CancellationToken.None));
    }

    private static RoomService CreateService(FakeRoomRepository repository, FakeRoomCodeGenerator codeGenerator) =>
        new(repository, codeGenerator, TimeProvider.System);

    private sealed class FakeRoomCodeGenerator(params string[] codes) : IRoomCodeGenerator
    {
        private int _index;

        public string Generate() => codes[_index++];
    }

    private sealed class FakeRoomRepository(int collisionsBeforeSuccess = 0) : IRoomRepository
    {
        public List<Room> AddedRooms { get; } = [];

        public int AddAttempts { get; private set; }

        private Room? _seededRoom;
        private Guid _seededOwnerId;

        public void Seed(Guid ownerId, Room room)
        {
            _seededOwnerId = ownerId;
            _seededRoom = room;
        }

        public Task AddAsync(Room room, CancellationToken cancellationToken)
        {
            AddAttempts++;
            if (AddAttempts <= collisionsBeforeSuccess)
            {
                throw new RoomCodeCollisionException();
            }

            AddedRooms.Add(room);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<RoomResponse>> ListForUserAsync(Guid hostUserId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RoomResponse>>([]);

        public Task<RoomResponse?> FindSummaryByCodeForUserAsync(Guid hostUserId, string code, CancellationToken cancellationToken)
        {
            if (_seededRoom is not null && _seededOwnerId == hostUserId && _seededRoom.Code == code)
            {
                var r = _seededRoom;
                return Task.FromResult<RoomResponse?>(new RoomResponse(r.Id, r.Code, r.Topic, r.Status.ToString(), r.QuestionTimeLimitSeconds, r.CreatedAt, 0, 0));
            }

            return Task.FromResult<RoomResponse?>(null);
        }

        public Task<int> DeleteDeletableAsync(Guid hostUserId, string code, CancellationToken cancellationToken)
        {
            var deletable = _seededRoom is not null
                && _seededOwnerId == hostUserId
                && _seededRoom.Code == code
                && _seededRoom.Status is RoomStatus.Draft or RoomStatus.Finished;

            return Task.FromResult(deletable ? 1 : 0);
        }
    }
}
