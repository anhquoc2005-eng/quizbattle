using System.ComponentModel.DataAnnotations;
using QuizBattle.Application.AiQuota;
using QuizBattle.Application.Common;
using QuizBattle.Application.Questions;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;
using QuizBattle.Tests.TestHelpers;

namespace QuizBattle.Tests.Questions;

/// <summary>Giữ chỗ / hoàn lượt AI trong QuestionService.GenerateAsync (fake viết tay, không Moq).</summary>
public sealed class QuestionServiceQuotaTests
{
    private static readonly QuestionCandidate[] OneValid = [new("Câu 1", ["A", "B", "C", "D"], 0)];
    private static readonly GenerateQuestionsRequest Five = new() { Count = 5 };

    // ---- D: kiểm tra rẻ chạy TRƯỚC giữ chỗ ----

    [Fact]
    public async Task Generate_RoomNotFound_DoesNotReserve()
    {
        var (room, quota, generator, service, _) = Setup();

        await Assert.ThrowsAsync<NotFoundException>(() => service.GenerateAsync(room.HostUserId, "ZZZZZZ", Five, CancellationToken.None));

        AssertUntouched(quota, generator);
    }

    [Fact]
    public async Task Generate_NotRoomOwner_DoesNotReserve()
    {
        var (room, quota, generator, service, _) = Setup();

        await Assert.ThrowsAsync<NotFoundException>(() => service.GenerateAsync(Guid.NewGuid(), room.Code, Five, CancellationToken.None));

        AssertUntouched(quota, generator);
    }

    [Fact]
    public async Task Generate_RoomNotDraft_DoesNotReserve()
    {
        var (room, quota, generator, service, _) = Setup();
        room.Status = RoomStatus.Lobby;

        await Assert.ThrowsAsync<ConflictException>(() => service.GenerateAsync(room.HostUserId, room.Code, Five, CancellationToken.None));

        AssertUntouched(quota, generator);
    }

    [Fact]
    public async Task Generate_ExceedsMaxQuestionsPerRoom_DoesNotReserve()
    {
        var (room, quota, generator, service, questions) = Setup();
        questions.SeedCount(room.Id, Room.MAX_QUESTIONS_PER_ROOM);

        await Assert.ThrowsAsync<ConflictException>(() => service.GenerateAsync(room.HostUserId, room.Code, Five, CancellationToken.None));

        AssertUntouched(quota, generator);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(21)]
    public void GenerateQuestionsRequest_CountOutOfRange_FailsModelValidationBeforeService(int count)
    {
        // Model binding của [ApiController] chạy trước controller/service nên request này không bao giờ tới bước giữ chỗ.
        var request = new GenerateQuestionsRequest { Count = count };

        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), [], validateAllProperties: true));
    }

    // ---- giữ chỗ / hoàn lượt ----

    [Fact]
    public async Task Generate_ReserveRejected_ThrowsAiDailyLimitAndNeverCallsGenerator()
    {
        var (room, quota, generator, service, _) = Setup();
        quota.RejectWith = AiLimitKind.Global;

        var ex = await Assert.ThrowsAsync<AiDailyLimitException>(() => service.GenerateAsync(room.HostUserId, room.Code, Five, CancellationToken.None));

        Assert.Equal("AI_DAILY_LIMIT_GLOBAL", ex.Code);
        Assert.Equal(0, generator.CallCount);
        Assert.Empty(quota.Refunds);
    }

    [Fact]
    public async Task Generate_Success_ReservesOnceAndDoesNotRefund()
    {
        var (room, quota, _, service, _) = Setup(validCandidates: OneValid);

        await service.GenerateAsync(room.HostUserId, room.Code, Five, CancellationToken.None);

        Assert.Equal(1, quota.ReserveCount);
        Assert.Empty(quota.Refunds);
    }

    [Fact]
    public async Task Generate_RetriesInsideOneRequest_StillReservesOnce()
    {
        // Một lượt = một request, bất kể bên trong gọi Gemini 1 hay 2 lần.
        var (room, quota, generator, service, _) = Setup(throwFormatErrorTimes: 1, validCandidates: OneValid);

        await service.GenerateAsync(room.HostUserId, room.Code, Five, CancellationToken.None);

        Assert.Equal(2, generator.CallCount);
        Assert.Equal(1, quota.ReserveCount);
    }

    [Fact]
    public async Task Generate_GeminiBadGateway_RefundsUserOnceWithReservedDay()
    {
        var (room, quota, _, service, _) = Setup(throwFormatErrorTimes: 2);
        quota.ReservedDay = new DateOnly(2026, 11, 19);

        await Assert.ThrowsAsync<BadGatewayException>(() => service.GenerateAsync(room.HostUserId, room.Code, Five, CancellationToken.None));

        var refund = Assert.Single(quota.Refunds);
        Assert.Equal(new AiReservation(room.HostUserId, new DateOnly(2026, 11, 19)), refund);
        Assert.Equal(["Reserve", "Refund"], quota.Calls);
    }

    [Fact]
    public async Task Generate_GeminiUnavailable_RefundsUserOnce()
    {
        var (room, quota, _, service, _) = Setup(generatorOverride: new ScriptedGenerator(_ => throw new AiUnavailableException("down")));

        await Assert.ThrowsAsync<AiUnavailableException>(() => service.GenerateAsync(room.HostUserId, room.Code, Five, CancellationToken.None));

        Assert.Single(quota.Refunds);
    }

    [Fact]
    public async Task Generate_CancelledWhileWaitingForGemini_DoesNotRefund()
    {
        // Generator giả ném thẳng OperationCanceledException: chỉ kiểm logic của service. Việc phân loại hủy/timeout của
        // generator THẬT nằm ở GeminiQuestionGeneratorCancellationTests và AiQuotaGenerateFlowTests.
        using var cts = new CancellationTokenSource();
        var (room, quota, _, service, _) = Setup(generatorOverride: new ScriptedGenerator(token =>
        {
            cts.Cancel();
            token.ThrowIfCancellationRequested();
            return OneValid;
        }));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GenerateAsync(room.HostUserId, room.Code, Five, cts.Token));

        Assert.Empty(quota.Refunds);
    }

    [Fact]
    public async Task Generate_AiUnavailableButRequestCancelled_DoesNotRefund()
    {
        // Phòng thủ lớp hai: dù lỗi hủy lọt ra dưới dạng AiUnavailableException, token đã hủy thì không hoàn.
        using var cts = new CancellationTokenSource();
        var (room, quota, _, service, _) = Setup(generatorOverride: new ScriptedGenerator(_ =>
        {
            cts.Cancel();
            throw new AiUnavailableException("bọc nhầm");
        }));

        await Assert.ThrowsAsync<AiUnavailableException>(() => service.GenerateAsync(room.HostUserId, room.Code, Five, cts.Token));

        Assert.Empty(quota.Refunds);
    }

    [Fact]
    public async Task Generate_InsertConflictAfterGemini_DoesNotRefund()
    {
        // Gemini đã trả câu hợp lệ; trong lúc chờ có request khác làm phòng đầy chỗ nên bước chèn ném Conflict.
        var (room, quota, _, service, questions) = Setup(generatorOverride: null);
        var generator = new ScriptedGenerator(_ =>
        {
            questions.SeedCount(room.Id, Room.MAX_QUESTIONS_PER_ROOM);
            return [new QuestionCandidate("Câu hỏi khác hẳn", ["A", "B", "C", "D"], 0)]; // không trùng câu đã seed
        });
        service = QuestionServiceTests.CreateService(room, questions, generator, quota);

        await Assert.ThrowsAsync<ConflictException>(() => service.GenerateAsync(room.HostUserId, room.Code, Five, CancellationToken.None));

        Assert.Equal(1, generator.CallCount);
        Assert.Empty(quota.Refunds);
    }

    // ---- helpers ----

    private static void AssertUntouched(FakeAiQuotaService quota, IQuestionGeneratorProbe generator)
    {
        Assert.Equal(0, quota.ReserveCount);
        Assert.Empty(quota.Refunds);
        Assert.Equal(0, generator.CallCount);
    }

    private static (Room Room, FakeAiQuotaService Quota, IQuestionGeneratorProbe Generator, QuestionService Service, QuestionServiceTests.FakeQuestionRepository Questions) Setup(
        int throwFormatErrorTimes = 0, IReadOnlyList<QuestionCandidate>? validCandidates = null, ScriptedGenerator? generatorOverride = null)
    {
        var room = QuestionServiceTests.NewDraftRoom();
        var quota = new FakeAiQuotaService();
        var questions = new QuestionServiceTests.FakeQuestionRepository();
        IQuestionGeneratorProbe probe = generatorOverride is not null
            ? generatorOverride
            : new FakeGeneratorProbe(new QuestionServiceTests.FakeQuestionGenerator(throwFormatErrorTimes, validCandidates));
        var service = QuestionServiceTests.CreateService(room, questions, (IQuestionGenerator)probe, quota);
        return (room, quota, probe, service, questions);
    }

    private interface IQuestionGeneratorProbe : IQuestionGenerator
    {
        int CallCount { get; }
    }

    private sealed class FakeGeneratorProbe(QuestionServiceTests.FakeQuestionGenerator inner) : IQuestionGeneratorProbe
    {
        public int CallCount => inner.CallCount;

        public Task<IReadOnlyList<QuestionCandidate>> GenerateAsync(string topic, int count, CancellationToken cancellationToken) =>
            inner.GenerateAsync(topic, count, cancellationToken);
    }

    /// <summary>Generator giả chạy một hàm do test truyền vào (ném lỗi hoặc trả câu hỏi).</summary>
    private sealed class ScriptedGenerator(Func<CancellationToken, IReadOnlyList<QuestionCandidate>> script) : IQuestionGeneratorProbe
    {
        public int CallCount { get; private set; }

        public Task<IReadOnlyList<QuestionCandidate>> GenerateAsync(string topic, int count, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(script(cancellationToken));
        }
    }
}
