using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Tests.Integration.Http;

/// <summary>Mỗi test (mỗi instance lớp) có host riêng => rate limiter trong RAM luôn sạch.</summary>
public abstract class HttpTestBase(DatabaseFixture fixture) : IDisposable
{
    private ApiFactory? _factory;

    protected DatabaseFixture Fixture { get; } = fixture;

    protected ApiFactory Factory => _factory ??= new ApiFactory(Fixture);

    protected ApiFactory UseFactory(int perUserLimit) => _factory = new ApiFactory(Fixture, perUserLimit);

    public void Dispose() => _factory?.Dispose();

    /// <summary>Kiểm status + Content-Type problem+json, trả JSON để kiểm code/resetAt.</summary>
    protected static async Task<JsonElement> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    protected static string? CodeOf(JsonElement problem) =>
        problem.TryGetProperty("code", out var code) ? code.GetString() : null;

    protected static async Task<JsonElement> GetQuotaAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/ai-quota");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    protected static async Task<int> UsedAsync(HttpClient client) => (await GetQuotaAsync(client)).GetProperty("used").GetInt32();

    protected static async Task<string> CreateRoomAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/rooms", new { topic = "Lịch sử" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString()!;
    }

    protected static Task<HttpResponseMessage> GenerateAsync(HttpClient client, string code, int count = 5) =>
        client.PostAsJsonAsync($"/api/rooms/{code}/questions/generate", new { count });

    /// <summary>Chèn thẳng DB cho phòng đủ số câu tối đa (để thử nhánh 409 trước bước giữ chỗ).</summary>
    protected async Task FillRoomAsync(string code)
    {
        await using var db = Fixture.CreateContext();
        var room = db.Rooms.Single(r => r.Code == code);
        for (var i = 0; i < Room.MAX_QUESTIONS_PER_ROOM; i++)
        {
            db.Questions.Add(new Question { RoomId = room.Id, OrderIndex = i, Text = $"Q{i}", Options = ["a", "b", "c", "d"], CorrectIndex = 0 });
        }

        await db.SaveChangesAsync();
    }
}
