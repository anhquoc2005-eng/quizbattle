using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QuizBattle.Api.Controllers;
using QuizBattle.Api.Extensions;

namespace QuizBattle.Tests.Api;

public sealed class AiQuotaApiTests
{
    [Fact]
    public void CodeFor_GenerateQuestionsPolicy_ReturnsAiRateLimit() =>
        Assert.Equal("AI_RATE_LIMIT", RateLimitRejection.CodeFor("generate-questions"));

    [Theory]
    [InlineData("auth")]
    [InlineData("join-room")]
    [InlineData(null)]
    public void CodeFor_OtherPolicies_ReturnsNull(string? policy) =>
        Assert.Null(RateLimitRejection.CodeFor(policy));

    [Fact]
    public void CodeFor_EndpointWithGeneratePolicy_ReturnsAiRateLimit()
    {
        var context = new DefaultHttpContext();
        context.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(new EnableRateLimitingAttribute("generate-questions")), "generate"));

        Assert.Equal("AI_RATE_LIMIT", RateLimitRejection.CodeFor(context));
    }

    [Fact]
    public void CodeFor_EndpointWithoutRateLimitMetadata_ReturnsNull() =>
        Assert.Null(RateLimitRejection.CodeFor(new DefaultHttpContext()));

    [Fact]
    public void Generate_Endpoint_StillUsesGenerateQuestionsRateLimitPolicy()
    {
        // Chỉ khẳng định TÊN policy còn gắn trên endpoint; mức 10 request/giờ cấu hình trong Program.cs không đọc ra được
        // từ test (đã giữ nguyên, xem diff).
        var method = typeof(QuestionsController).GetMethod(nameof(QuestionsController.Generate))!;

        var attribute = method.GetCustomAttribute<EnableRateLimitingAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal("generate-questions", attribute.PolicyName);
    }

    [Fact]
    public void AiQuotaController_RequiresAuthorizationAndUsesAiQuotaRoute()
    {
        var type = typeof(AiQuotaController);

        Assert.NotNull(type.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Equal("api/ai-quota", type.GetCustomAttribute<RouteAttribute>()!.Template);
        Assert.NotNull(type.GetMethod(nameof(AiQuotaController.Get))!.GetCustomAttribute<HttpGetAttribute>());
    }
}
