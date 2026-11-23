using QuizBattle.Application.Common;
using QuizBattle.Application.Practice;
using QuizBattle.Tests.TestHelpers;

namespace QuizBattle.Tests.Practice;

/// <summary>Kiểm tra đầu vào của PracticeService chạy TRƯỚC khi chạm repository: lỗi 400 phải có code và trường để client map lên form.</summary>
public sealed class PracticeServiceValidationTests
{
    private static PracticeService Service() =>
        new(new ThrowingRepository(), new PracticeOptions(5, 100), new FakeTimeProvider(DateTimeOffset.UnixEpoch));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ABC12")]
    [InlineData("ABC1234")]
    [InlineData("ABC-12")]
    [InlineData("ABC 12")]
    [InlineData("ÁBC123")]
    public async Task StartSession_MalformedRoomCode_ThrowsValidationWithCodeAndField(string roomCode)
    {
        var ex = await Assert.ThrowsAsync<RequestValidationException>(() =>
            Service().StartSessionAsync(Guid.NewGuid(), new StartPracticeSessionRequest(roomCode), CancellationToken.None));

        Assert.Equal("roomCode", ex.Field);
        Assert.Equal("invalid_room_code", ex.Code);
    }

    [Fact]
    public async Task StartSession_NullRoomCode_ThrowsValidation()
    {
        var ex = await Assert.ThrowsAsync<RequestValidationException>(() =>
            Service().StartSessionAsync(Guid.NewGuid(), new StartPracticeSessionRequest(null!), CancellationToken.None));

        Assert.Equal("invalid_room_code", ex.Code);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(int.MaxValue)]
    public async Task SubmitAnswer_SelectedIndexOutOfRange_ThrowsValidationWithCode(int selectedIndex)
    {
        var ex = await Assert.ThrowsAsync<RequestValidationException>(() =>
            Service().SubmitAnswerAsync(Guid.NewGuid(), Guid.NewGuid(), new SubmitPracticeAnswerRequest(Guid.NewGuid(), selectedIndex), CancellationToken.None));

        Assert.Equal("selectedIndex", ex.Field);
        Assert.Equal("invalid_selected_index", ex.Code);
    }

    [Fact]
    public async Task SubmitAnswer_EmptyQuestionId_ThrowsValidationWithCode()
    {
        var ex = await Assert.ThrowsAsync<RequestValidationException>(() =>
            Service().SubmitAnswerAsync(Guid.NewGuid(), Guid.NewGuid(), new SubmitPracticeAnswerRequest(Guid.Empty, 0), CancellationToken.None));

        Assert.Equal("questionId", ex.Field);
        Assert.Equal("missing_question_id", ex.Code);
    }

    // Mọi lời gọi repository đều là lỗi test: đầu vào sai phải bị chặn trước đó.
    private sealed class ThrowingRepository : IPracticeRepository
    {
        public Task<IReadOnlyList<PracticeRoomRow>> ListRoomsAsync(Guid userId, CancellationToken cancellationToken) => throw new InvalidOperationException();

        public Task<int> GetXpTodayAsync(Guid userId, DateOnly day, CancellationToken cancellationToken) => throw new InvalidOperationException();

        public Task<PlayableRoom?> FindPlayableRoomAsync(Guid userId, string roomCode, CancellationToken cancellationToken) => throw new InvalidOperationException();

        public Task<PracticeSessionStart> CreateOrGetSessionAsync(Guid userId, PlayableRoom room, IReadOnlyList<Guid> shuffledQuestionIds, DateTimeOffset now, CancellationToken cancellationToken) => throw new InvalidOperationException();

        public Task<PracticeSessionState?> GetSessionAsync(Guid userId, Guid sessionId, DateTimeOffset now, CancellationToken cancellationToken) => throw new InvalidOperationException();

        public Task<PracticeSubmitOutcome> SubmitAnswerAsync(PracticeSubmitCommand command, CancellationToken cancellationToken) => throw new InvalidOperationException();
    }
}
