using System.Net;
using System.Net.Http.Json;

namespace QuizBattle.Tests.Integration.Http;

[Collection(DatabaseCollection.Name)]
public sealed class RateLimitHttpTests(DatabaseFixture fixture) : HttpTestBase(fixture)
{
    [RequiresDatabaseFact]
    public async Task Generate_EleventhRequestInHour_Returns429AiRateLimit_AndEachUserHasOwnWindow()
    {
        var (_, first) = await Factory.CreateUserClientAsync(Fixture);
        var (_, second) = await Factory.CreateUserClientAsync(Fixture);

        // Limiter đứng trước controller nên 404 (phòng không tồn tại) vẫn tính vào hạn mức.
        for (var i = 1; i <= 10; i++)
        {
            await AssertProblemAsync(await GenerateAsync(first, "ZZZZZZ"), HttpStatusCode.NotFound);
        }

        var limited = await AssertProblemAsync(await GenerateAsync(first, "ZZZZZZ"), HttpStatusCode.TooManyRequests);
        Assert.Equal("AI_RATE_LIMIT", CodeOf(limited));

        // User thứ hai không bị gộp chung nhóm: bắt lỗi nếu UseRateLimiter chạy trước khi biết user.
        for (var i = 1; i <= 10; i++)
        {
            await AssertProblemAsync(await GenerateAsync(second, "ZZZZZZ"), HttpStatusCode.NotFound);
        }
    }

    [RequiresDatabaseFact]
    public async Task Register_SixthRequestInMinute_Returns429WithoutCode()
    {
        var client = Factory.CreateClient();

        for (var i = 1; i <= 5; i++)
        {
            var ok = await client.PostAsJsonAsync("/api/auth/register",
                new { email = $"{Guid.NewGuid():N}@test.local", password = "password123", displayName = "T" });
            Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        }

        var sixth = await client.PostAsJsonAsync("/api/auth/register",
            new { email = $"{Guid.NewGuid():N}@test.local", password = "password123", displayName = "T" });

        var problem = await AssertProblemAsync(sixth, HttpStatusCode.TooManyRequests);
        Assert.Null(CodeOf(problem));
    }
}
