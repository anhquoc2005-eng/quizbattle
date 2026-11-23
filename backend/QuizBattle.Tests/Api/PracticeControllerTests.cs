using System.Security.Claims;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QuizBattle.Api.Controllers;
using QuizBattle.Api.Extensions;
using QuizBattle.Application.Common;
using QuizBattle.Application.Practice;

namespace QuizBattle.Tests.Api;

/// <summary>
/// Hình dạng HTTP của PracticeController (201 + Location, 200 khi trả lại lượt, yêu cầu đăng nhập, rate limit policy). Ánh xạ lỗi nghiệp vụ
/// sang 400/404/409 nằm ở GlobalExceptionHandlerTests và PracticeHttpTests; hành vi qua host thật nằm ở PracticeHttpTests.
/// </summary>
public sealed class PracticeControllerTests
{
    private static readonly Guid UserId = Guid.Parse("cccccccc-0000-0000-0000-000000000003");
    private static readonly Guid SessionId = Guid.Parse("dddddddd-0000-0000-0000-000000000004");

    private static ControllerContext ContextFor(Guid userId) => new()
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", userId.ToString())], "test")),
        },
    };

    [Fact]
    public void PracticeController_HasEnableRateLimitingPracticePolicy()
    {
        var attribute = typeof(PracticeController).GetCustomAttribute<EnableRateLimitingAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal("practice", attribute.PolicyName);
        Assert.Equal(RateLimitRejection.PracticePolicy, attribute.PolicyName);
    }

    [Fact]
    public void PracticeController_RequiresAuthorizationAndAllowsNoAnonymousAction()
    {
        Assert.NotNull(typeof(PracticeController).GetCustomAttribute<AuthorizeAttribute>());
        var actions = typeof(PracticeController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        Assert.NotEmpty(actions);
        Assert.All(actions, m => Assert.Null(m.GetCustomAttribute<AllowAnonymousAttribute>()));
    }

    [Fact]
    public void CodeFor_PracticePolicy_ReturnsPracticeRateLimit() =>
        Assert.Equal("PRACTICE_RATE_LIMIT", RateLimitRejection.CodeFor("practice"));

    [Fact]
    public void CodeFor_EndpointWithPracticePolicy_ReturnsPracticeRateLimit()
    {
        var context = new DefaultHttpContext();
        context.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(new EnableRateLimitingAttribute("practice")), "practice"));

        Assert.Equal("PRACTICE_RATE_LIMIT", RateLimitRejection.CodeFor(context));
    }

    [Theory]
    [InlineData("auth")]
    [InlineData("join-room")]
    [InlineData(null)]
    public void CodeFor_OtherPolicies_StillHaveNoPracticeCode(string? policy) =>
        Assert.NotEqual("PRACTICE_RATE_LIMIT", RateLimitRejection.CodeFor(policy));

    [Fact]
    public async Task StartSession_Created_Returns201WithLocationAndBody()
    {
        var controller = new PracticeController(new StubService(created: true)) { ControllerContext = ContextFor(UserId) };

        var result = await controller.StartSession(new StartPracticeSessionRequest("ABC123"), CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.Equal($"/api/practice/sessions/{SessionId}", created.Location);
        Assert.Equal(new PracticeSessionRefDto(SessionId, 3), created.Value);
    }

    [Fact]
    public async Task StartSession_ExistingOpenSession_Returns200WithSameBody()
    {
        var controller = new PracticeController(new StubService(created: false)) { ControllerContext = ContextFor(UserId) };

        var result = await controller.StartSession(new StartPracticeSessionRequest("ABC123"), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(new PracticeSessionRefDto(SessionId, 3), ok.Value);
    }

    [Fact]
    public async Task Actions_PassCurrentUserIdToService()
    {
        var service = new StubService(created: true);
        var controller = new PracticeController(service) { ControllerContext = ContextFor(UserId) };

        await controller.GetRooms(CancellationToken.None);
        await controller.StartSession(new StartPracticeSessionRequest("ABC123"), CancellationToken.None);
        await controller.GetSession(SessionId, CancellationToken.None);
        await controller.SubmitAnswer(SessionId, new SubmitPracticeAnswerRequest(Guid.NewGuid(), 1), CancellationToken.None);

        Assert.Equal([UserId, UserId, UserId, UserId], service.Callers);
    }

    private sealed class StubService(bool created) : IPracticeService
    {
        public List<Guid> Callers { get; } = [];

        public Task<PracticeRoomsResponse> GetRoomsAsync(Guid userId, CancellationToken cancellationToken)
        {
            Callers.Add(userId);
            return Task.FromResult(new PracticeRoomsResponse(5, 100, 0, 100, DateTimeOffset.UnixEpoch, []));
        }

        public Task<PracticeSessionStartDto> StartSessionAsync(Guid userId, StartPracticeSessionRequest request, CancellationToken cancellationToken)
        {
            Callers.Add(userId);
            return Task.FromResult(new PracticeSessionStartDto(new PracticeSessionRefDto(SessionId, 3), created));
        }

        public Task<PracticeSessionDto> GetSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
        {
            Callers.Add(userId);
            return Task.FromResult(new PracticeSessionDto(sessionId, "Chủ đề", 3, 0, false, null, null, null));
        }

        public Task<PracticeAnswerResultDto> SubmitAnswerAsync(Guid userId, Guid sessionId, SubmitPracticeAnswerRequest request, CancellationToken cancellationToken)
        {
            Callers.Add(userId);
            return Task.FromResult(new PracticeAnswerResultDto(true, 0, 5, null, 5, 95, false));
        }
    }
}
