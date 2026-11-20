using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace QuizBattle.Tests.Integration.Http;

[Collection(DatabaseCollection.Name)]
public sealed class InfrastructureHttpTests(DatabaseFixture fixture) : HttpTestBase(fixture)
{
    [RequiresDatabaseFact]
    public async Task CreateRoom_ViaHttp_PersistsInTempDatabaseNotDevDatabase()
    {
        var (_, client) = await Factory.CreateUserClientAsync(Fixture);

        var code = await CreateRoomAsync(client);

        await using var db = Fixture.CreateContext();
        Assert.True(await db.Rooms.AnyAsync(r => r.Code == code));
        var appDb = new NpgsqlConnectionStringBuilder(
            Factory.Services.GetRequiredService<IConfiguration>()["ConnectionStrings:Default"]).Database;
        Assert.StartsWith("qb_test_", appDb);
    }

    [RequiresDatabaseFact]
    public async Task Health_ReturnsOkAndDatabaseUp()
    {
        var response = await Factory.CreateClient().GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("up", body.GetProperty("database").GetString());
        Assert.Equal("ok", body.GetProperty("status").GetString());
    }

    [RequiresDatabaseFact]
    public async Task Cors_AllowedOrigin_ReturnsAllowOriginAndCredentials()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.Add("Origin", ApiFactory.AllowedOrigin);

        var response = await Factory.CreateClient().SendAsync(request);

        Assert.Equal(ApiFactory.AllowedOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").Single());
    }

    [RequiresDatabaseFact]
    public async Task ShopItems_Anonymous_Returns200WithVaryOriginAndPublicCache()
    {
        var response = await Factory.CreateClient().GetAsync("/api/shop/items");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Origin", response.Headers.Vary);
        var cache = response.Headers.CacheControl;
        Assert.True(cache?.Public);
        Assert.Equal(TimeSpan.FromSeconds(3600), cache?.MaxAge);
    }

    [RequiresDatabaseFact]
    public async Task Cors_UnknownOrigin_HasNoAllowOrigin()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.Add("Origin", "http://evil.example");

        var response = await Factory.CreateClient().SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
