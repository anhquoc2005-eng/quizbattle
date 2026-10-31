using QuizBattle.Application.Common;
using QuizBattle.Application.Questions;
using QuizBattle.Application.Rooms;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Tests.Questions;

public sealed class QuestionServiceTests
{
    [Fact]
    public async Task Generate_InvalidJsonTwice_ThrowsBadGateway()
    {
        var room = NewDraftRoom();
        var generator = new FakeQuestionGenerator(throwFormatErrorTimes: 2);
        var service = CreateService(room, new FakeQuestionRepository(), generator);

        await Assert.ThrowsAsync<BadGatewayException>(() =>
            service.GenerateAsync(room.HostUserId, room.Code, new GenerateQuestionsRequest { Count = 5 }, CancellationToken.None));

        Assert.Equal(2, generator.CallCount);
    }

    [Fact]
    public async Task Generate_InvalidThenValid_RetriesOnce()
    {
        var room = NewDraftRoom();
        var generator = new FakeQuestionGenerator(
            throwFormatErrorTimes: 1,
            validCandidates: [new QuestionCandidate("Câu 1", ["A", "B", "C", "D"], 0)]);
        var service = CreateService(room, new FakeQuestionRepository(), generator);

        var result = await service.GenerateAsync(room.HostUserId, room.Code, new GenerateQuestionsRequest { Count = 5 }, CancellationToken.None);

        Assert.Equal(2, generator.CallCount);
        Assert.Equal(1, result.Created);
    }

    [Fact]
    public async Task Generate_RoomNotDraft_ThrowsConflict()
    {
        var room = NewDraftRoom();
        room.Status = RoomStatus.Lobby;
        var generator = new FakeQuestionGenerator();
        var service = CreateService(room, new FakeQuestionRepository(), generator);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.GenerateAsync(room.HostUserId, room.Code, new GenerateQuestionsRequest { Count = 5 }, CancellationToken.None));

        Assert.Equal(0, generator.CallCount);
    }

    [Fact]
    public async Task Generate_ExceedsMaxQuestions_ThrowsConflict()
    {
        var room = NewDraftRoom();
        var questionRepository = new FakeQuestionRepository();
        questionRepository.SeedCount(room.Id, Room.MAX_QUESTIONS_PER_ROOM - 2);
        var generator = new FakeQuestionGenerator();
        var service = CreateService(room, questionRepository, generator);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.GenerateAsync(room.HostUserId, room.Code, new GenerateQuestionsRequest { Count = 5 }, CancellationToken.None));

        Assert.Equal(0, generator.CallCount);
    }

    [Fact]
    public async Task EditQuestion_SetsApproved()
    {
        var room = NewDraftRoom();
        var question = new Question
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            Text = "Cũ",
            Options = ["A", "B", "C", "D"],
            CorrectIndex = 0,
            ReviewStatus = QuestionReviewStatus.Pending,
        };
        var questionRepository = new FakeQuestionRepository();
        questionRepository.Seed(question);
        var service = CreateService(room, questionRepository, new FakeQuestionGenerator());

        var input = new QuestionInput { Text = "Mới", Options = ["A", "B", "C", "D"], CorrectIndex = 1 };
        var result = await service.UpdateAsync(room.HostUserId, room.Code, question.Id, input, CancellationToken.None);

        Assert.Equal("Approved", result.ReviewStatus);
        Assert.Equal(QuestionReviewStatus.Approved, question.ReviewStatus);
    }

    private static Room NewDraftRoom() => new()
    {
        Id = Guid.NewGuid(),
        Code = "ABCDEF",
        HostUserId = Guid.NewGuid(),
        Topic = "Java OOP",
        Status = RoomStatus.Draft,
        QuestionTimeLimitSeconds = 20,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private static QuestionService CreateService(Room room, IQuestionRepository questionRepository, IQuestionGenerator generator) =>
        new(questionRepository, new FakeRoomRepository(room), generator);

    private sealed class FakeRoomRepository(Room room) : IRoomRepository
    {
        public Task AddAsync(Room r, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<RoomResponse>> ListForUserAsync(Guid hostUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<RoomResponse?> FindSummaryByCodeForUserAsync(Guid hostUserId, string code, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<int> DeleteDeletableAsync(Guid hostUserId, string code, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Room?> FindEntityByCodeForUserAsync(Guid hostUserId, string code, CancellationToken cancellationToken) =>
            Task.FromResult(room.HostUserId == hostUserId && room.Code == code ? room : null);
    }

    private sealed class FakeQuestionGenerator(int throwFormatErrorTimes = 0, IReadOnlyList<QuestionCandidate>? validCandidates = null) : IQuestionGenerator
    {
        public int CallCount { get; private set; }

        public Task<IReadOnlyList<QuestionCandidate>> GenerateAsync(string topic, int count, CancellationToken cancellationToken)
        {
            CallCount++;
            if (CallCount <= throwFormatErrorTimes)
            {
                throw new AiResponseFormatException("JSON sai định dạng.");
            }

            return Task.FromResult(validCandidates ?? []);
        }
    }

    private sealed class FakeQuestionRepository : IQuestionRepository
    {
        private readonly List<Question> _questions = [];

        public void Seed(Question question) => _questions.Add(question);

        public void SeedCount(Guid roomId, int count)
        {
            for (var i = 0; i < count; i++)
            {
                _questions.Add(new Question { Id = Guid.NewGuid(), RoomId = roomId, Text = $"Câu {i}", Options = ["A", "B", "C", "D"], CorrectIndex = 0, OrderIndex = i });
            }
        }

        public Task<IReadOnlyList<Question>> ListByRoomIdAsync(Guid roomId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Question>>(_questions.Where(q => q.RoomId == roomId).ToList());

        public Task<Question?> FindByIdAsync(Guid roomId, Guid questionId, CancellationToken cancellationToken) =>
            Task.FromResult(_questions.SingleOrDefault(q => q.RoomId == roomId && q.Id == questionId));

        public Task<int> CountByRoomIdAsync(Guid roomId, CancellationToken cancellationToken) =>
            Task.FromResult(_questions.Count(q => q.RoomId == roomId));

        public Task<IReadOnlyList<string>> ListTextsByRoomIdAsync(Guid roomId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>(_questions.Where(q => q.RoomId == roomId).Select(q => q.Text).ToList());

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task DeleteAsync(Question question, CancellationToken cancellationToken)
        {
            _questions.Remove(question);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Question>> InsertWithRoomLockAsync(
            Guid roomId, Func<RoomSnapshot, IReadOnlyList<Question>> buildQuestions, CancellationToken cancellationToken)
        {
            var roomQuestions = _questions.Where(q => q.RoomId == roomId).ToList();
            var snapshot = new RoomSnapshot(
                RoomStatus.Draft,
                roomQuestions.Count,
                roomQuestions.Select(q => q.Text).ToList(),
                roomQuestions.Count == 0 ? 0 : roomQuestions.Max(q => q.OrderIndex) + 1);

            var toInsert = buildQuestions(snapshot);
            _questions.AddRange(toInsert);
            return Task.FromResult(toInsert);
        }
    }
}
