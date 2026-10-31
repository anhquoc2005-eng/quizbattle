using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using QuizBattle.Application.Common;

namespace QuizBattle.Api.Middleware;

/// <summary>
/// Bắt mọi exception chưa được xử lý. Exception nghiệp vụ (Conflict/Unauthorized) map sang
/// mã lỗi tương ứng, còn lại coi là lỗi không mong muốn (500). Không gửi stack trace xuống client.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, type) = exception switch
        {
            ConflictException => (StatusCodes.Status409Conflict, exception.Message, "https://tools.ietf.org/html/rfc9110#section-15.5.10"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, exception.Message, "https://tools.ietf.org/html/rfc9110#section-15.5.2"),
            NotFoundException => (StatusCodes.Status404NotFound, exception.Message, "https://tools.ietf.org/html/rfc9110#section-15.5.5"),
            BadGatewayException => (StatusCodes.Status502BadGateway, exception.Message, "https://tools.ietf.org/html/rfc9110#section-15.6.3"),
            AiUnavailableException => (StatusCodes.Status503ServiceUnavailable, exception.Message, "https://tools.ietf.org/html/rfc9110#section-15.6.4"),
            _ => (StatusCodes.Status500InternalServerError, "Đã xảy ra lỗi không mong muốn.", "https://tools.ietf.org/html/rfc9110#section-15.6.1"),
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Lỗi chưa được xử lý khi gọi {Path}", httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Type = type,
            },
        });
    }
}
