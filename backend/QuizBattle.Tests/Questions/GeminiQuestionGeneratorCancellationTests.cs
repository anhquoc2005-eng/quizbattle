using Microsoft.Extensions.Logging.Abstractions;
using QuizBattle.Application.Common;
using QuizBattle.Infrastructure.Ai;

namespace QuizBattle.Tests.Questions;

/// <summary>
/// HttpClient.SendAsync ném TaskCanceledException cho CẢ client hủy request lẫn timeout. Hủy phải đi ra nguyên là
/// OperationCanceledException (không hoàn lượt); chỉ timeout / lỗi mạng mới thành AiUnavailableException (có hoàn lượt).
/// </summary>
public sealed class GeminiQuestionGeneratorCancellationTests
{
    [Fact]
    public async Task Generate_TokenCancelledDuringSend_ThrowsOperationCanceledNotAiUnavailable()
    {
        using var cts = new CancellationTokenSource();
        var handler = new FakeHandler(async (_, token) =>
        {
            await cts.CancelAsync(); // client đóng tab giữa lúc đang chờ Gemini
            await Task.Delay(Timeout.Infinite, token); // ném TaskCanceledException như HttpClient thật
            return new HttpResponseMessage();
        });

        var ex = await Record.ExceptionAsync(() => CreateGenerator(handler).GenerateAsync("chủ đề", 5, cts.Token));

        Assert.IsAssignableFrom<OperationCanceledException>(ex);
        Assert.IsNotType<AiUnavailableException>(ex);
    }

    [Fact]
    public async Task Generate_HandlerThrowsTaskCanceledWhileTokenNotCancelled_ThrowsAiUnavailable()
    {
        // Mô phỏng timeout của HttpClient: ném TaskCanceledException trong khi token của request vẫn còn sống.
        var handler = new FakeHandler((_, _) => throw new TaskCanceledException("timeout"));

        await Assert.ThrowsAsync<AiUnavailableException>(() => CreateGenerator(handler).GenerateAsync("chủ đề", 5, CancellationToken.None));
    }

    [Fact]
    public async Task Generate_HttpRequestException_ThrowsAiUnavailable()
    {
        var handler = new FakeHandler((_, _) => throw new HttpRequestException("network down"));

        await Assert.ThrowsAsync<AiUnavailableException>(() => CreateGenerator(handler).GenerateAsync("chủ đề", 5, CancellationToken.None));
    }

    public static GeminiQuestionGenerator CreateGenerator(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") },
            new AiOptions("test-key", "test-model"),
            NullLogger<GeminiQuestionGenerator>.Instance);

    /// <summary>HttpMessageHandler giả viết tay: hành vi do test truyền vào.</summary>
    public sealed class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> behavior) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            behavior(request, cancellationToken);
    }
}
