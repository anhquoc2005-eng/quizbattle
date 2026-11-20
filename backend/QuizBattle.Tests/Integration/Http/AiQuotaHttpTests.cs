using System.Net;

namespace QuizBattle.Tests.Integration.Http;

[Collection(DatabaseCollection.Name)]
public sealed class AiQuotaHttpTests(DatabaseFixture fixture) : HttpTestBase(fixture)
{
    [RequiresDatabaseFact]
    public async Task GetQuota_NoToken_Returns401Problem()
    {
        var response = await Factory.CreateClient().GetAsync("/api/ai-quota");

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized);
    }

    [RequiresDatabaseFact]
    public async Task GetQuota_WithToken_ReturnsFullShape()
    {
        var (_, client) = await Factory.CreateUserClientAsync(Fixture);

        var body = await GetQuotaAsync(client);

        foreach (var name in new[] { "dailyLimit", "used", "remaining", "globalExhausted", "resetAt" })
        {
            Assert.True(body.TryGetProperty(name, out _), $"thiếu trường {name}");
        }

        Assert.Equal(0, body.GetProperty("used").GetInt32());
        Assert.False(body.GetProperty("globalExhausted").GetBoolean());
    }

    [RequiresDatabaseFact]
    public async Task Generate_UserDailyCapReached_Returns429WithCodeAndResetAt()
    {
        UseFactory(perUserLimit: 2);
        var (_, client) = await Factory.CreateUserClientAsync(Fixture);
        var room = await CreateRoomAsync(client);

        Assert.Equal(HttpStatusCode.Created, (await GenerateAsync(client, room)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await GenerateAsync(client, room)).StatusCode);
        var third = await AssertProblemAsync(await GenerateAsync(client, room), HttpStatusCode.TooManyRequests);

        Assert.Equal("AI_DAILY_LIMIT_USER", CodeOf(third));
        Assert.True(third.TryGetProperty("resetAt", out _));
        Assert.Equal(2, await UsedAsync(client));
    }

    [RequiresDatabaseFact]
    public async Task Generate_NoToken_Returns401Problem()
    {
        // Bắt lỗi đặt UseRateLimiter trước UseAuthorization: hàm chia lượt gọi GetUserId() với request ẩn danh -> 500.
        var response = await GenerateAsync(Factory.CreateClient(), "ZZZZZZ");

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized);
    }

    [RequiresDatabaseFact]
    public async Task Generate_InvalidBody_Returns400AndDoesNotConsumeQuota()
    {
        var (_, client) = await Factory.CreateUserClientAsync(Fixture);
        var room = await CreateRoomAsync(client);

        await AssertProblemAsync(await GenerateAsync(client, room, count: 1), HttpStatusCode.BadRequest);

        Assert.Equal(0, await UsedAsync(client));
    }

    [RequiresDatabaseFact]
    public async Task Generate_UnknownRoom_Returns404AndDoesNotConsumeQuota()
    {
        var (_, client) = await Factory.CreateUserClientAsync(Fixture);

        await AssertProblemAsync(await GenerateAsync(client, "ZZZZZZ"), HttpStatusCode.NotFound);

        Assert.Equal(0, await UsedAsync(client));
    }

    [RequiresDatabaseFact]
    public async Task Generate_RoomFull_Returns409AndDoesNotConsumeQuota()
    {
        var (_, client) = await Factory.CreateUserClientAsync(Fixture);
        var room = await CreateRoomAsync(client);
        await FillRoomAsync(room);

        await AssertProblemAsync(await GenerateAsync(client, room), HttpStatusCode.Conflict);

        Assert.Equal(0, await UsedAsync(client));
        Assert.Equal(0, Factory.Generator.Calls);
    }

    [RequiresDatabaseFact]
    public async Task Generate_AiUnavailable_Returns503AndRefundsUserQuota()
    {
        var (_, client) = await Factory.CreateUserClientAsync(Fixture);
        var room = await CreateRoomAsync(client);
        Factory.Generator.Mode = ControllableGenerator.Behavior.Unavailable;

        await AssertProblemAsync(await GenerateAsync(client, room), HttpStatusCode.ServiceUnavailable);

        Assert.Equal(0, await UsedAsync(client));
    }

    [RequiresDatabaseFact]
    public async Task Generate_AiReturnsInvalidQuestions_Returns502AndRefundsUserQuota()
    {
        var (_, client) = await Factory.CreateUserClientAsync(Fixture);
        var room = await CreateRoomAsync(client);
        Factory.Generator.Mode = ControllableGenerator.Behavior.Invalid;

        await AssertProblemAsync(await GenerateAsync(client, room), HttpStatusCode.BadGateway);

        Assert.Equal(2, Factory.Generator.Calls); // thử lại đúng 1 lần
        Assert.Equal(0, await UsedAsync(client));
    }
}
