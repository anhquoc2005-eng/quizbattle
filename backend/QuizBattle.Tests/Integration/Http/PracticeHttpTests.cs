using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Tests.Integration.Http;

/// <summary>/api/practice qua host thật (ApiFactory) trên DB tạm: xác thực, mã trạng thái, JSON thô không lộ đáp án, idempotent, rate limit.</summary>
[Collection(DatabaseCollection.Name)]
public sealed class PracticeHttpTests(DatabaseFixture fixture) : HttpTestBase(fixture)
{
    private readonly PracticeTestData _data = new(fixture);

    // ---- Dựng dữ liệu ----

    private sealed record Me(Guid UserId, HttpClient Client, SeededRoom Room);

    // User đăng nhập (JWT thật) là người chơi của một phòng đã kết thúc hợp lệ.
    private async Task<Me> SetupAsync(int questions = 3)
    {
        var (userId, client) = await Factory.CreateUserClientAsync(Fixture);
        var host = await _data.UserAsync("host");
        var room = await _data.FinishedRoomAsync(host, [IdOnly(userId), await _data.UserAsync("partner")], questions);
        return new Me(userId, client, room);
    }

    private static User IdOnly(Guid id) => new() { Id = id, Email = "x@test.local", PasswordHash = "x", DisplayName = "x" };

    private static async Task<(HttpResponseMessage Response, string Raw, JsonElement Json)> ReadAsync(HttpResponseMessage response)
    {
        var raw = await response.Content.ReadAsStringAsync();
        var json = string.IsNullOrEmpty(raw) ? default : JsonDocument.Parse(raw).RootElement.Clone();
        return (response, raw, json);
    }

    private static async Task<(HttpResponseMessage Response, string Raw, JsonElement Json)> StartAsync(HttpClient client, string roomCode) =>
        await ReadAsync(await client.PostAsJsonAsync("/api/practice/sessions", new { roomCode }));

    private static async Task<(HttpResponseMessage Response, string Raw, JsonElement Json)> GetSessionAsync(HttpClient client, Guid sessionId) =>
        await ReadAsync(await client.GetAsync($"/api/practice/sessions/{sessionId}"));

    private static async Task<(HttpResponseMessage Response, string Raw, JsonElement Json)> SubmitAsync(HttpClient client, Guid sessionId, Guid questionId, int selectedIndex) =>
        await ReadAsync(await client.PostAsJsonAsync($"/api/practice/sessions/{sessionId}/answers", new { questionId, selectedIndex }));

    private static Guid SessionIdOf(JsonElement json) => json.GetProperty("sessionId").GetGuid();

    private static Guid CurrentQuestionOf(JsonElement session) => session.GetProperty("current").GetProperty("questionId").GetGuid();

    // ---- Xác thực ----

    [RequiresDatabaseFact]
    public async Task AnyEndpoint_NoToken_Returns401()
    {
        var anonymous = Factory.CreateClient();
        var id = Guid.NewGuid();

        var responses = new[]
        {
            await anonymous.GetAsync("/api/practice/rooms"),
            await anonymous.PostAsJsonAsync("/api/practice/sessions", new { roomCode = "ABC123" }),
            await anonymous.GetAsync($"/api/practice/sessions/{id}"),
            await anonymous.PostAsJsonAsync($"/api/practice/sessions/{id}/answers", new { questionId = id, selectedIndex = 0 }),
        };

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode));
    }

    [RequiresDatabaseFact]
    public async Task AnyEndpoint_GuestSessionToken_Returns401()
    {
        // Token phiên của khách là chuỗi ngẫu nhiên do server cấp khi vào phòng, không phải JWT: scheme Bearer không chấp nhận.
        var guest = Factory.CreateClient();
        guest.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Convert.ToHexString(Guid.NewGuid().ToByteArray()));
        var id = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.GetAsync("/api/practice/rooms")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.PostAsJsonAsync("/api/practice/sessions", new { roomCode = "ABC123" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.GetAsync($"/api/practice/sessions/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.PostAsJsonAsync($"/api/practice/sessions/{id}/answers", new { questionId = id, selectedIndex = 0 })).StatusCode);
    }

    // ---- 404 / 400 ----

    [RequiresDatabaseFact]
    public async Task CreateSession_IneligibleOrMissingRoom_Returns404Same()
    {
        var me = await SetupAsync();
        var foreign = await _data.FinishedRoomAsync(await _data.UserAsync("host"), [await _data.UserAsync(), await _data.UserAsync()]);

        var ineligible = await AssertProblemAsync(await me.Client.PostAsJsonAsync("/api/practice/sessions", new { roomCode = foreign.Room.Code }), HttpStatusCode.NotFound);
        var missing = await AssertProblemAsync(await me.Client.PostAsJsonAsync("/api/practice/sessions", new { roomCode = "ZZZZZZ" }), HttpStatusCode.NotFound);

        Assert.Equal(missing.GetProperty("title").GetString(), ineligible.GetProperty("title").GetString());
        Assert.Equal("not_found", CodeOf(ineligible));
        Assert.Equal("not_found", CodeOf(missing));
    }

    [RequiresDatabaseFact]
    public async Task CreateSession_MalformedRoomCode_Returns400WithCodeAndErrors()
    {
        var me = await SetupAsync();

        var problem = await AssertProblemAsync(await me.Client.PostAsJsonAsync("/api/practice/sessions", new { roomCode = "AB-1" }), HttpStatusCode.BadRequest);

        Assert.Equal("invalid_room_code", CodeOf(problem));
        Assert.Equal(1, problem.GetProperty("errors").GetProperty("roomCode").GetArrayLength());
    }

    [RequiresDatabaseFact]
    public async Task GetSession_OtherUsersSession_Returns404()
    {
        var mine = await SetupAsync();
        var started = await StartAsync(mine.Client, mine.Room.Room.Code);
        var sessionId = SessionIdOf(started.Json);
        var (_, stranger) = await Factory.CreateUserClientAsync(Fixture);

        var problem = await AssertProblemAsync(await stranger.GetAsync($"/api/practice/sessions/{sessionId}"), HttpStatusCode.NotFound);
        var answer = await stranger.PostAsJsonAsync($"/api/practice/sessions/{sessionId}/answers", new { questionId = mine.Room.Questions[0].Id, selectedIndex = 0 });

        Assert.Equal("not_found", CodeOf(problem));
        Assert.Equal(HttpStatusCode.NotFound, answer.StatusCode);
    }

    [RequiresDatabaseFact]
    public async Task GetSession_UnknownId_Returns404()
    {
        var me = await SetupAsync();

        await AssertProblemAsync(await me.Client.GetAsync($"/api/practice/sessions/{Guid.NewGuid()}"), HttpStatusCode.NotFound);
    }

    [RequiresDatabaseFact]
    public async Task Submit_QuestionNotInSession_Returns404()
    {
        var me = await SetupAsync();
        var sessionId = SessionIdOf((await StartAsync(me.Client, me.Room.Room.Code)).Json);
        var otherRoom = await _data.FinishedRoomAsync(await _data.UserAsync("host"), [await _data.UserAsync(), await _data.UserAsync()]);

        await AssertProblemAsync(await me.Client.PostAsJsonAsync($"/api/practice/sessions/{sessionId}/answers", new { questionId = Guid.NewGuid(), selectedIndex = 0 }), HttpStatusCode.NotFound);
        await AssertProblemAsync(await me.Client.PostAsJsonAsync($"/api/practice/sessions/{sessionId}/answers", new { questionId = otherRoom.Questions[0].Id, selectedIndex = 0 }), HttpStatusCode.NotFound);
    }

    [RequiresDatabaseFact]
    public async Task Submit_SelectedIndexOutOfRange_Returns400WithCodeAndErrors()
    {
        var me = await SetupAsync();
        var sessionId = SessionIdOf((await StartAsync(me.Client, me.Room.Room.Code)).Json);
        var session = await GetSessionAsync(me.Client, sessionId);

        foreach (var bad in new[] { -1, 4 })
        {
            var problem = await AssertProblemAsync(
                await me.Client.PostAsJsonAsync($"/api/practice/sessions/{sessionId}/answers", new { questionId = CurrentQuestionOf(session.Json), selectedIndex = bad }),
                HttpStatusCode.BadRequest);

            Assert.Equal("invalid_selected_index", CodeOf(problem));
            Assert.Equal(1, problem.GetProperty("errors").GetProperty("selectedIndex").GetArrayLength());
        }
    }

    [RequiresDatabaseFact]
    public async Task Submit_MissingQuestionId_Returns400WithCodeAndErrors()
    {
        var me = await SetupAsync();
        var sessionId = SessionIdOf((await StartAsync(me.Client, me.Room.Room.Code)).Json);

        var response = await me.Client.PostAsync(
            $"/api/practice/sessions/{sessionId}/answers",
            new StringContent("""{"selectedIndex":0}""", System.Text.Encoding.UTF8, "application/json"));

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("missing_question_id", CodeOf(problem));
        Assert.Equal(1, problem.GetProperty("errors").GetProperty("questionId").GetArrayLength());
    }

    [RequiresDatabaseFact]
    public async Task Submit_BodyWithWrongJsonType_StaysDefaultAspNet400WithoutCode()
    {
        // Hạn chế chung đã biết (contract): body sai kiểu bị ASP.NET model binding từ chối, chưa có trường "code". Ghi nhận, không sửa ở đợt này.
        var me = await SetupAsync();
        var sessionId = SessionIdOf((await StartAsync(me.Client, me.Room.Room.Code)).Json);

        var response = await me.Client.PostAsync(
            $"/api/practice/sessions/{sessionId}/answers",
            new StringContent("""{"questionId":"khong-phai-guid","selectedIndex":"abc"}""", System.Text.Encoding.UTF8, "application/json"));

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Null(CodeOf(problem));
    }

    // ---- JSON thô: không lộ đáp án ----

    [RequiresDatabaseFact]
    public async Task RawPayload_Rooms_ContainsNoCorrectIndex()
    {
        var me = await SetupAsync();
        await StartAsync(me.Client, me.Room.Room.Code); // có cả lượt đang mở trong danh sách

        var response = await me.Client.GetAsync("/api/practice/rooms");
        var raw = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(me.Room.Room.Code, raw);
        Assert.DoesNotContain("correctindex", raw, StringComparison.OrdinalIgnoreCase);
    }

    [RequiresDatabaseFact]
    public async Task RawPayload_PostSessions_ContainsNoCorrectIndex()
    {
        var me = await SetupAsync();

        var first = await StartAsync(me.Client, me.Room.Room.Code);
        var second = await StartAsync(me.Client, me.Room.Room.Code);

        Assert.DoesNotContain("correctindex", first.Raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("correctindex", second.Raw, StringComparison.OrdinalIgnoreCase);
    }

    [RequiresDatabaseFact]
    public async Task RawPayload_GetSession_NoCorrectIndexUntilAnswered_ThenOnlyInsideLastResult()
    {
        var me = await SetupAsync();
        var sessionId = SessionIdOf((await StartAsync(me.Client, me.Room.Room.Code)).Json);

        var before = await GetSessionAsync(me.Client, sessionId);
        Assert.DoesNotContain("correctindex", before.Raw, StringComparison.OrdinalIgnoreCase);

        await SubmitAsync(me.Client, sessionId, CurrentQuestionOf(before.Json), PracticeTestData.Wrong);
        var after = await GetSessionAsync(me.Client, sessionId);

        var occurrences = System.Text.RegularExpressions.Regex.Matches(after.Raw, "correctindex", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;
        Assert.Equal(1, occurrences); // đúng một chỗ: lastResult
        Assert.False(after.Json.GetProperty("current").TryGetProperty("correctIndex", out _));
        Assert.Equal(PracticeTestData.Correct, after.Json.GetProperty("lastResult").GetProperty("correctIndex").GetInt32());
    }

    [RequiresDatabaseFact]
    public async Task Submit_Response_HasCorrectIndexAndXpFields()
    {
        var me = await SetupAsync();
        var sessionId = SessionIdOf((await StartAsync(me.Client, me.Room.Room.Code)).Json);
        var session = await GetSessionAsync(me.Client, sessionId);

        var answer = await SubmitAsync(me.Client, sessionId, CurrentQuestionOf(session.Json), PracticeTestData.Correct);

        Assert.Equal(HttpStatusCode.OK, answer.Response.StatusCode);
        Assert.Contains("correctIndex", answer.Raw);
        Assert.True(answer.Json.GetProperty("isCorrect").GetBoolean());
        Assert.Equal(PracticeTestData.Correct, answer.Json.GetProperty("correctIndex").GetInt32());
        Assert.Equal(5, answer.Json.GetProperty("xpAwarded").GetInt32());
        Assert.Equal(JsonValueKind.Null, answer.Json.GetProperty("xpReason").ValueKind);
        Assert.Equal(5, answer.Json.GetProperty("xpGainedToday").GetInt32());
        Assert.Equal(95, answer.Json.GetProperty("remainingToday").GetInt32());
        Assert.False(answer.Json.GetProperty("completed").GetBoolean());
    }

    // ---- POST /sessions: 201 rồi 200 ----

    [RequiresDatabaseFact]
    public async Task CreateSession_FirstReturns201WithLocationThenReturns200WithSameSessionId()
    {
        var me = await SetupAsync();

        var first = await StartAsync(me.Client, me.Room.Room.Code);
        var second = await StartAsync(me.Client, me.Room.Room.Code);

        var sessionId = SessionIdOf(first.Json);
        Assert.Equal(HttpStatusCode.Created, first.Response.StatusCode);
        Assert.Equal($"/api/practice/sessions/{sessionId}", first.Response.Headers.Location?.ToString());
        Assert.Equal(3, first.Json.GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.OK, second.Response.StatusCode);
        Assert.Equal(sessionId, SessionIdOf(second.Json));
        Assert.Null(second.Response.Headers.Location);
    }

    [RequiresDatabaseFact]
    public async Task CreateSession_TwoParallelRequests_SameSessionId()
    {
        var me = await SetupAsync();

        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => StartAsync(me.Client, me.Room.Room.Code)));

        Assert.All(results, r => Assert.True(r.Response.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK, r.Raw));
        Assert.Single(results.Select(r => SessionIdOf(r.Json)).Distinct());
        Assert.Equal(1, results.Count(r => r.Response.StatusCode == HttpStatusCode.Created));
    }

    // ---- Nộp: idempotent, 409 ----

    [RequiresDatabaseFact]
    public async Task Submit_Replay_Returns200SameResultEvenWithDifferentSelectedIndex()
    {
        var me = await SetupAsync();
        var sessionId = SessionIdOf((await StartAsync(me.Client, me.Room.Room.Code)).Json);
        var questionId = CurrentQuestionOf((await GetSessionAsync(me.Client, sessionId)).Json);

        var first = await SubmitAsync(me.Client, sessionId, questionId, PracticeTestData.Wrong);
        var sameBody = await SubmitAsync(me.Client, sessionId, questionId, PracticeTestData.Wrong);
        var differentBody = await SubmitAsync(me.Client, sessionId, questionId, PracticeTestData.Correct);

        Assert.All(new[] { first, sameBody, differentBody }, r => Assert.Equal(HttpStatusCode.OK, r.Response.StatusCode));
        Assert.All(new[] { sameBody, differentBody }, r =>
        {
            Assert.False(r.Json.GetProperty("isCorrect").GetBoolean());
            Assert.Equal("wrong", r.Json.GetProperty("xpReason").GetString());
            Assert.Equal(0, r.Json.GetProperty("xpAwarded").GetInt32());
            Assert.Equal(PracticeTestData.Correct, r.Json.GetProperty("correctIndex").GetInt32());
        });
    }

    [RequiresDatabaseFact]
    public async Task Submit_TwoParallelRequestsSameQuestion_BothReturn200SameResult()
    {
        var me = await SetupAsync();
        var sessionId = SessionIdOf((await StartAsync(me.Client, me.Room.Room.Code)).Json);
        var questionId = CurrentQuestionOf((await GetSessionAsync(me.Client, sessionId)).Json);

        var results = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => SubmitAsync(me.Client, sessionId, questionId, PracticeTestData.Correct)));

        Assert.All(results, r => Assert.Equal(HttpStatusCode.OK, r.Response.StatusCode));
        Assert.All(results, r =>
        {
            Assert.True(r.Json.GetProperty("isCorrect").GetBoolean());
            Assert.Equal(5, r.Json.GetProperty("xpAwarded").GetInt32());
        });
        var profile = await me.Client.GetAsync("/api/users/me");
        Assert.Equal(5, (await profile.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("totalXp").GetInt32()); // XP cộng đúng một lần
    }

    [RequiresDatabaseFact]
    public async Task Submit_UnansweredNotCurrent_Returns409NotCurrentQuestion()
    {
        var me = await SetupAsync();
        var sessionId = SessionIdOf((await StartAsync(me.Client, me.Room.Room.Code)).Json);
        var current = CurrentQuestionOf((await GetSessionAsync(me.Client, sessionId)).Json);
        var notCurrent = me.Room.QuestionIds.First(id => id != current);

        var problem = await AssertProblemAsync(
            await me.Client.PostAsJsonAsync($"/api/practice/sessions/{sessionId}/answers", new { questionId = notCurrent, selectedIndex = 0 }),
            HttpStatusCode.Conflict);

        Assert.Equal("not_current_question", CodeOf(problem));
    }

    [RequiresDatabaseFact]
    public async Task GetSession_AfterSubmit_HasLastResult()
    {
        var me = await SetupAsync();
        var sessionId = SessionIdOf((await StartAsync(me.Client, me.Room.Room.Code)).Json);
        var questionId = CurrentQuestionOf((await GetSessionAsync(me.Client, sessionId)).Json);
        await SubmitAsync(me.Client, sessionId, questionId, PracticeTestData.Correct);

        var session = await GetSessionAsync(me.Client, sessionId); // như F5 sau khi nộp

        var last = session.Json.GetProperty("lastResult");
        Assert.Equal(questionId, last.GetProperty("questionId").GetGuid());
        Assert.True(last.GetProperty("isCorrect").GetBoolean());
        Assert.Equal(5, last.GetProperty("xpAwarded").GetInt32());
        Assert.Equal(1, session.Json.GetProperty("answeredCount").GetInt32());
        Assert.NotEqual(questionId, CurrentQuestionOf(session.Json));
    }

    [RequiresDatabaseFact]
    public async Task Rooms_ListsEligibleRoomWithOpenSessionId()
    {
        var me = await SetupAsync();
        var sessionId = SessionIdOf((await StartAsync(me.Client, me.Room.Room.Code)).Json);

        var rooms = await ReadAsync(await me.Client.GetAsync("/api/practice/rooms"));

        var item = Assert.Single(rooms.Json.GetProperty("rooms").EnumerateArray());
        Assert.Equal(me.Room.Room.Code, item.GetProperty("roomCode").GetString());
        Assert.Equal("player", item.GetProperty("role").GetString());
        Assert.Equal(3, item.GetProperty("questionCount").GetInt32());
        Assert.Equal(sessionId, item.GetProperty("openSessionId").GetGuid());
        Assert.Equal(5, rooms.Json.GetProperty("xpPerCorrect").GetInt32());
        Assert.Equal(100, rooms.Json.GetProperty("dailyXpCap").GetInt32());
        Assert.EndsWith("+07:00", rooms.Json.GetProperty("resetAt").GetString());
    }

    [RequiresDatabaseFact]
    public async Task FullFlow_CreateAnswerAllCompleteSummary_RemainingTodayCorrect()
    {
        var me = await SetupAsync();
        var sessionId = SessionIdOf((await StartAsync(me.Client, me.Room.Room.Code)).Json);

        JsonElement lastAnswer = default;
        for (var i = 0; i < 3; i++)
        {
            var session = await GetSessionAsync(me.Client, sessionId);
            lastAnswer = (await SubmitAsync(me.Client, sessionId, CurrentQuestionOf(session.Json), i == 1 ? PracticeTestData.Wrong : PracticeTestData.Correct)).Json;
        }

        Assert.True(lastAnswer.GetProperty("completed").GetBoolean());
        var done = await GetSessionAsync(me.Client, sessionId);
        Assert.True(done.Json.GetProperty("completed").GetBoolean());
        Assert.Equal(JsonValueKind.Null, done.Json.GetProperty("current").ValueKind);
        var summary = done.Json.GetProperty("summary");
        Assert.Equal(2, summary.GetProperty("correctCount").GetInt32());
        Assert.Equal(10, summary.GetProperty("xpGainedInSession").GetInt32());
        Assert.Equal(10, summary.GetProperty("xpGainedToday").GetInt32());
        Assert.Equal(90, summary.GetProperty("remainingToday").GetInt32());
        Assert.EndsWith("+07:00", summary.GetProperty("resetAt").GetString());
    }

    [RequiresDatabaseFact]
    public async Task GetSession_RemainingQuestionsDeleted_ClosesSessionWithoutTouchingXp()
    {
        var me = await SetupAsync();
        var sessionId = SessionIdOf((await StartAsync(me.Client, me.Room.Room.Code)).Json);
        var answered = CurrentQuestionOf((await GetSessionAsync(me.Client, sessionId)).Json);
        await SubmitAsync(me.Client, sessionId, answered, PracticeTestData.Correct);
        await using (var db = Fixture.CreateContext())
        {
            db.Questions.RemoveRange(db.Questions.Where(q => q.RoomId == me.Room.Room.Id && q.Id != answered));
            await db.SaveChangesAsync();
        }

        var closed = await GetSessionAsync(me.Client, sessionId);

        Assert.True(closed.Json.GetProperty("completed").GetBoolean());
        Assert.Equal(5, closed.Json.GetProperty("summary").GetProperty("xpGainedInSession").GetInt32());
        Assert.Equal(5, await _data.TotalXpAsync(me.UserId)); // đóng lượt không đụng XP
    }

    // ---- Rate limit ----

    [RequiresDatabaseFact]
    public async Task Practice_Over120PerMinute_Returns429WithPracticeRateLimitCode()
    {
        var me = await SetupAsync();

        for (var i = 0; i < 120; i++)
        {
            var ok = await me.Client.GetAsync("/api/practice/rooms");
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        }

        var limited = await me.Client.GetAsync("/api/practice/rooms");

        var problem = await AssertProblemAsync(limited, HttpStatusCode.TooManyRequests);
        Assert.Equal("PRACTICE_RATE_LIMIT", CodeOf(problem));
    }

    [RequiresDatabaseFact]
    public async Task Practice_RateLimitIsPerUser_AnotherUserIsNotLimited()
    {
        var heavy = await SetupAsync();
        var (_, other) = await Factory.CreateUserClientAsync(Fixture);
        for (var i = 0; i < 121; i++)
        {
            await heavy.Client.GetAsync("/api/practice/rooms");
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await heavy.Client.GetAsync("/api/practice/rooms")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await other.GetAsync("/api/practice/rooms")).StatusCode);
    }
}
