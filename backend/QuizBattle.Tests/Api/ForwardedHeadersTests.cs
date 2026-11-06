using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QuizBattle.Api.Extensions;

namespace QuizBattle.Tests.Api;

public sealed class ForwardedHeadersTests
{
    // Rate limiter dùng Connection.RemoteIpAddress làm partition key, nên chỉ cần kiểm tra giá trị này sau middleware.
    private static async Task<HttpContext> RunAsync(string? clientIpHeader, Action<HttpContext> setup)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1"); // IP của proxy, không nằm trong KnownProxies mặc định
        setup(context);
        var middleware = new ForwardedHeadersMiddleware(
            _ => Task.CompletedTask,
            NullLoggerFactory.Instance,
            Options.Create(ClientIpForwarding.CreateOptions(clientIpHeader)));
        await middleware.Invoke(context);
        return context;
    }

    [Fact]
    public async Task DefaultHeader_UsesLastForwardedIp_NotClientSuppliedOne()
    {
        var context = await RunAsync(null, c => c.Request.Headers["X-Forwarded-For"] = "9.9.9.9, 203.0.113.5");

        Assert.Equal(IPAddress.Parse("203.0.113.5"), context.Connection.RemoteIpAddress);
    }

    [Fact]
    public async Task CustomHeader_UsesCloudflareClientIp()
    {
        var context = await RunAsync("CF-Connecting-IP", c =>
        {
            c.Request.Headers["X-Forwarded-For"] = "203.0.113.5, 172.70.0.1";
            c.Request.Headers["CF-Connecting-IP"] = "203.0.113.5";
        });

        Assert.Equal(IPAddress.Parse("203.0.113.5"), context.Connection.RemoteIpAddress);
    }

    [Fact]
    public async Task ForwardedProto_SetsRequestScheme()
    {
        var context = await RunAsync(null, c => c.Request.Headers["X-Forwarded-Proto"] = "https");

        Assert.Equal("https", context.Request.Scheme);
    }
}
