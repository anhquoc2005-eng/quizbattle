using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using QuizBattle.Api.Middleware;
using QuizBattle.Application.Common;

namespace QuizBattle.Tests.Api;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task GlobalExceptionHandler_UnauthorizedWithCode_WritesCodeExtension()
    {
        var (problem, status) = await HandleAsync(new UnauthorizedException("Phiên đăng nhập đã hết hạn.", "token_expired"));

        Assert.Equal(StatusCodes.Status401Unauthorized, status);
        Assert.Equal("token_expired", problem.Extensions["code"]);
    }

    [Fact]
    public async Task GlobalExceptionHandler_UnauthorizedWithoutCode_NoCodeExtension()
    {
        var (problem, status) = await HandleAsync(new UnauthorizedException("Sai email hoặc mật khẩu."));

        Assert.Equal(StatusCodes.Status401Unauthorized, status);
        Assert.False(problem.Extensions.ContainsKey("code"));
    }

    [Fact]
    public async Task GlobalExceptionHandler_SessionReplaced_Returns401WithCode()
    {
        var (problem, status) = await HandleAsync(new SessionReplacedException("Bạn đã vào phòng từ thiết bị khác."));

        Assert.Equal(StatusCodes.Status401Unauthorized, status);
        Assert.Equal("session_replaced", problem.Extensions["code"]);
    }

    private static async Task<(ProblemDetails Problem, int Status)> HandleAsync(Exception exception)
    {
        var problemDetails = new CapturingProblemDetailsService();
        var handler = new GlobalExceptionHandler(problemDetails, NullLogger<GlobalExceptionHandler>.Instance);
        var context = new DefaultHttpContext();

        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        return (problemDetails.Captured!, context.Response.StatusCode);
    }

    private sealed class CapturingProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetails? Captured { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            Captured = context.ProblemDetails;
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            Captured = context.ProblemDetails;
            return ValueTask.FromResult(true);
        }
    }
}
