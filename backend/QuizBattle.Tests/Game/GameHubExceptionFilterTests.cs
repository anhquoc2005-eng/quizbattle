using System.Security.Claims;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using QuizBattle.Api.Hubs;
using QuizBattle.Application.Common;

namespace QuizBattle.Tests.Game;

public sealed class GameHubExceptionFilterTests
{
    [Fact]
    public async Task GameHubExceptionFilter_SessionReplaced_SendsSessionReplacedCode()
    {
        var client = new CapturingClientProxy();
        var filter = new GameHubExceptionFilter(new FakeHubContext(client), NullLogger<GameHubExceptionFilter>.Instance);
        var invocation = new HubInvocationContext(
            new FakeCallerContext("conn-1"),
            new ServiceCollection().BuildServiceProvider(),
            new DummyHub(),
            typeof(DummyHub).GetMethod(nameof(DummyHub.SubmitAnswer))!,
            []);

        await filter.InvokeMethodAsync(invocation, _ => throw new SessionReplacedException("Bạn đã vào phòng từ thiết bị khác."));

        Assert.Equal(GameHubEvents.ERROR, client.Method);
        var error = Assert.IsType<GameErrorDto>(Assert.Single(client.Args!));
        Assert.Equal("session_replaced", error.Code);
        Assert.Equal(nameof(DummyHub.SubmitAnswer), error.Method);
    }

    private sealed class DummyHub : Hub
    {
        public void SubmitAnswer()
        {
        }
    }

    private sealed class CapturingClientProxy : IClientProxy
    {
        public string? Method { get; private set; }

        public object?[]? Args { get; private set; }

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            Method = method;
            Args = args;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeHubContext(IClientProxy proxy) : IHubContext<GameHub>
    {
        public IHubClients Clients { get; } = new FakeHubClients(proxy);

        public IGroupManager Groups => throw new NotSupportedException();
    }

    private sealed class FakeHubClients(IClientProxy proxy) : IHubClients
    {
        public IClientProxy All => proxy;

        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => proxy;

        public IClientProxy Client(string connectionId) => proxy;

        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => proxy;

        public IClientProxy Group(string groupName) => proxy;

        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => proxy;

        public IClientProxy Groups(IReadOnlyList<string> groupNames) => proxy;

        public IClientProxy User(string userId) => proxy;

        public IClientProxy Users(IReadOnlyList<string> userIds) => proxy;
    }

    private sealed class FakeCallerContext(string connectionId) : HubCallerContext
    {
        public override string ConnectionId { get; } = connectionId;

        public override string? UserIdentifier => null;

        public override ClaimsPrincipal? User => null;

        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();

        public override IFeatureCollection Features { get; } = new FeatureCollection();

        public override CancellationToken ConnectionAborted => CancellationToken.None;

        public override void Abort()
        {
        }
    }
}
