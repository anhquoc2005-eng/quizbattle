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

    [Fact]
    public async Task GlobalExceptionHandler_ConflictWithCode_WritesCodeExtension()
    {
        var (problem, status) = await HandleAsync(new ConflictException("Bạn đã sở hữu món này.", "already_owned"));

        Assert.Equal(StatusCodes.Status409Conflict, status);
        Assert.Equal("already_owned", problem.Extensions["code"]);
    }

    [Fact]
    public async Task GlobalExceptionHandler_ConflictWithoutCode_NoCodeExtension()
    {
        var (problem, status) = await HandleAsync(new ConflictException("Email đã được sử dụng."));

        Assert.Equal(StatusCodes.Status409Conflict, status);
        Assert.False(problem.Extensions.ContainsKey("code"));
    }

    [Fact]
    public async Task GlobalExceptionHandler_NotFoundWithCode_WritesCodeExtension()
    {
        var (problem, status) = await HandleAsync(new NotFoundException("Không tìm thấy món đồ.", "item_not_found"));

        Assert.Equal(StatusCodes.Status404NotFound, status);
        Assert.Equal("item_not_found", problem.Extensions["code"]);
    }

    [Fact]
    public async Task GlobalExceptionHandler_NotFoundWithoutCode_NoCodeExtension()
    {
        var (problem, _) = await HandleAsync(new NotFoundException("Không tìm thấy phòng."));

        Assert.False(problem.Extensions.ContainsKey("code"));
    }

    [Fact]
    public async Task GlobalExceptionHandler_ValidationWithCode_WritesCodeAndErrors()
    {
        var (problem, status) = await HandleAsync(new RequestValidationException("hair", "Kiểu tóc không có.", "unknown_variant"));

        Assert.Equal(StatusCodes.Status400BadRequest, status);
        Assert.Equal("unknown_variant", problem.Extensions["code"]);
        var errors = Assert.IsType<Dictionary<string, string[]>>(problem.Extensions["errors"]);
        Assert.Equal(["Kiểu tóc không có."], errors["hair"]);
    }

    [Fact]
    public async Task GlobalExceptionHandler_ValidationWithoutCode_NoCodeExtension()
    {
        var (problem, _) = await HandleAsync(new RequestValidationException("limit", "limit phải từ 1 đến 50."));

        Assert.False(problem.Extensions.ContainsKey("code"));
    }

    [Fact]
    public async Task GlobalExceptionHandler_ConflictWithField_WritesCodeAndErrors()
    {
        var (problem, status) = await HandleAsync(new ConflictException("Bạn chưa sở hữu món này.", "item_not_owned", "hair"));

        Assert.Equal(StatusCodes.Status409Conflict, status);
        Assert.Equal("item_not_owned", problem.Extensions["code"]);
        var errors = Assert.IsType<Dictionary<string, string[]>>(problem.Extensions["errors"]);
        Assert.Equal(["Bạn chưa sở hữu món này."], errors["hair"]);
    }

    [Fact]
    public async Task GlobalExceptionHandler_ConflictWithoutField_NoErrorsExtension()
    {
        var (problem, _) = await HandleAsync(new ConflictException("Bạn đã sở hữu món này.", "already_owned"));

        Assert.False(problem.Extensions.ContainsKey("errors"));
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
