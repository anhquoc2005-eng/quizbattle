using Microsoft.EntityFrameworkCore;
using QuizBattle.Application.Practice;
using QuizBattle.Domain.Entities;
using QuizBattle.Infrastructure.Persistence;
using QuizBattle.Tests.TestHelpers;
using Xunit.Abstractions;

namespace QuizBattle.Tests.Integration;

/// <summary>
/// Test độ nhạy ("mutation") theo mẫu <c>ShopPurchaseSensitivityTests</c>, không dùng công cụ mutation: mỗi test chạy CÙNG kịch bản với
/// các khẳng định của test thường, một lần với bản gốc (PHẢI qua) và một lần với cờ <c>internal</c> bật (PHẢI đỏ). Thông điệp đỏ
/// được in ra để ghi vào quickstart.md.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class PracticeSensitivityTests(DatabaseFixture fixture, ITestOutputHelper output)
{
    private static readonly DateOnly Day = new(2026, 11, 21);
    private readonly PracticeTestData _data = new(fixture);

    private async Task<Exception> AssertRedAsync(string mutation, Func<Task> scenario)
    {
        var ex = await Assert.ThrowsAnyAsync<Exception>(scenario);
        var inner = ex.InnerException is null ? string.Empty : $" <- {ex.InnerException.GetType().Name}: {ex.InnerException.Message.ReplaceLineEndings(" | ")}";
        output.WriteLine($"{mutation} ĐỎ khi bật cờ: {ex.GetType().Name}: {ex.Message.ReplaceLineEndings(" | ")}{inner}");
        return ex;
    }

    // ---- M1: bỏ WHERE Xp < cap ----

    [RequiresDatabaseFact]
    public async Task Mutation_M1_NoWhereInReserveSql_LoweredCapTestTurnsRed()
    {
        await LoweredCapScenarioAsync(null);
        await AssertRedAsync("M1", () => LoweredCapScenarioAsync(r => r.ReserveVariant = PracticeReserveVariant.NoWhere));
    }

    // Đã dùng 100, hạ trần xuống 50: bộ đếm không được giảm và không được cộng thêm (khẳng định của Cap_LoweredBelowAlreadyUsed...).
    private async Task LoweredCapScenarioAsync(Action<PracticeRepository>? mutate)
    {
        var options = new PracticeOptions(5, 50);
        var me = await _data.UserAsync("me");
        var room = await _data.FinishedRoomAsync(await _data.UserAsync("host"), [me, await _data.UserAsync()], questions: 1);
        var sessionId = await _data.StartAsync(me, room, options: options);
        await using (var seed = fixture.CreateContext())
        {
            seed.PracticeXpDailies.Add(new PracticeXpDaily { UserId = me.Id, Day = Day, Xp = 100 });
            await seed.SaveChangesAsync();
        }

        var result = await SubmitCurrentAsync(me, sessionId, PracticeTestData.Correct, options, mutate);

        Assert.Equal((0, "daily_cap"), (result.XpAwarded, result.XpReason));
        Assert.Equal(100, await _data.DailyXpAsync(me.Id, Day));
    }

    // ---- M2 và M3: nhánh "phần còn lại" (cấu hình 7/10) ----

    [RequiresDatabaseFact]
    public async Task Mutation_M2_NoLeast_RemainderTestTurnsRed()
    {
        await RemainderScenarioAsync(null);
        await AssertRedAsync("M2", () => RemainderScenarioAsync(r => r.ReserveVariant = PracticeReserveVariant.NoLeast));
    }

    [RequiresDatabaseFact]
    public async Task Mutation_M3_RejectWholeQuestion_RemainderTestTurnsRed()
    {
        await RemainderScenarioAsync(null);
        await AssertRedAsync("M3", () => RemainderScenarioAsync(r => r.ReserveVariant = PracticeReserveVariant.RejectWhole));
    }

    // Khẳng định của Cap_RemainingLessThanQuestionXp_GrantsRemainderThenZero: 7 -> 3 -> 0, bộ đếm = trần = 10.
    private async Task RemainderScenarioAsync(Action<PracticeRepository>? mutate)
    {
        var options = new PracticeOptions(7, 10);
        var me = await _data.UserAsync("me");
        var room = await _data.FinishedRoomAsync(await _data.UserAsync("host"), [me, await _data.UserAsync()], questions: 3);
        var sessionId = await _data.StartAsync(me, room, options: options);

        var first = await SubmitCurrentAsync(me, sessionId, PracticeTestData.Correct, options, mutate);
        var second = await SubmitCurrentAsync(me, sessionId, PracticeTestData.Correct, options, mutate);
        var third = await SubmitCurrentAsync(me, sessionId, PracticeTestData.Correct, options, mutate);

        Assert.Equal((7, (string?)null), (first.XpAwarded, first.XpReason));
        Assert.Equal((3, "daily_cap_partial"), (second.XpAwarded, second.XpReason));
        Assert.Equal((0, "daily_cap"), (third.XpAwarded, third.XpReason));
        Assert.Equal(10, await _data.DailyXpAsync(me.Id, Day));
        Assert.Equal(10, await _data.TotalXpAsync(me.Id));
    }

    // ---- M4: tắt khóa Users ----

    [RequiresDatabaseFact]
    public async Task Mutation_M4_NoUserRowLock_InvariantTurnsRed_ButCapStillHolds()
    {
        await TwoRoomsSameUserScenarioAsync(mutate: false);
        await AssertRedAsync("M4 (bất biến TotalXp)", () => TwoRoomsSameUserScenarioAsync(mutate: true));

        // Với cờ bật, trần vẫn giữ nhờ câu SQL nguyên tử: hai lớp bảo vệ độc lập.
        var (me, daily) = await TwoRoomsCapOnlyAsync(mutate: true);
        Assert.True(daily <= 5, $"bộ đếm {daily} vượt trần 5 dù câu SQL nguyên tử");
        await using var db = fixture.CreateContext();
        Assert.Equal(5, (await db.PracticeXpDailies.AsNoTracking().SingleAsync(d => d.UserId == me.Id)).Xp);
    }

    [RequiresDatabaseFact]
    public async Task Mutation_M4_NoUserRowLock_ParallelSameQuestionTestTurnsRed()
    {
        await ParallelSameQuestionScenarioAsync(mutate: false);
        await AssertRedAsync("M4 (cùng câu đồng thời)", () => ParallelSameQuestionScenarioAsync(mutate: true));
    }

    // Hai phòng, cùng user, hai lượt: nộp đồng thời. Khẳng định: TotalXp bằng tổng dòng XP (bất biến), cả hai thành công.
    private async Task TwoRoomsSameUserScenarioAsync(bool mutate)
    {
        var (me, sessions, questions) = await PracticeCapTests.SeedParallelAsync(_data, 2);
        var rendezvous = new Rendezvous(2, TimeSpan.FromSeconds(1.5));

        var (outcomes, stats) = await ConcurrentRun.RunCatchingAsync(fixture, 2, (i, db) =>
            PracticeTestData.Service(db, PracticeTestData.Noon, configure: r => ConfigureMutation(r, mutate, rendezvous)).SubmitAnswerAsync(
                me.Id, sessions[i], new SubmitPracticeAnswerRequest(questions[i], PracticeTestData.Correct), CancellationToken.None));

        ConcurrentRun.AssertReallyConcurrent(stats);
        Assert.All(outcomes, o => Assert.True(o.Succeeded, o.Error?.ToString()));
        await using var check = fixture.CreateContext();
        await XpInvariant.AssertAsync(check, [me.Id]);
        Assert.Equal(10, await _data.TotalXpAsync(me.Id));
    }

    private async Task<(User Me, int Daily)> TwoRoomsCapOnlyAsync(bool mutate)
    {
        var (me, sessions, questions) = await PracticeCapTests.SeedParallelAsync(_data, 2);
        var options = new PracticeOptions(5, 5);
        var rendezvous = new Rendezvous(2, TimeSpan.FromSeconds(1.5));

        var (outcomes, stats) = await ConcurrentRun.RunCatchingAsync(fixture, 2, (i, db) =>
            PracticeTestData.Service(db, PracticeTestData.Noon, options, r => ConfigureMutation(r, mutate, rendezvous)).SubmitAnswerAsync(
                me.Id, sessions[i], new SubmitPracticeAnswerRequest(questions[i], PracticeTestData.Correct), CancellationToken.None));

        ConcurrentRun.AssertReallyConcurrent(stats);
        Assert.All(outcomes, o => Assert.True(o.Succeeded, o.Error?.ToString()));
        return (me, await _data.DailyXpAsync(me.Id, Day));
    }

    // Khẳng định của Submit_TwoParallelRequestsSameQuestion...: tất cả 200, một dòng XP, XP cộng một lần.
    private async Task ParallelSameQuestionScenarioAsync(bool mutate)
    {
        var me = await _data.UserAsync("me");
        var room = await _data.FinishedRoomAsync(await _data.UserAsync("host"), [me, await _data.UserAsync()], questions: 1);
        var sessionId = await _data.StartAsync(me, room);
        var questionId = room.Questions[0].Id;
        var rendezvous = new Rendezvous(2, TimeSpan.FromSeconds(1.5));

        var (outcomes, stats) = await ConcurrentRun.RunCatchingAsync(fixture, 2, (_, db) =>
            PracticeTestData.Service(db, PracticeTestData.Noon, configure: r => ConfigureMutation(r, mutate, rendezvous)).SubmitAnswerAsync(
                me.Id, sessionId, new SubmitPracticeAnswerRequest(questionId, PracticeTestData.Correct), CancellationToken.None));

        ConcurrentRun.AssertReallyConcurrent(stats);
        Assert.All(outcomes, o => Assert.True(o.Succeeded, o.Error?.ToString()));
        Assert.Equal(5, await _data.TotalXpAsync(me.Id));
    }

    private static void ConfigureMutation(PracticeRepository repository, bool mutate, Rendezvous rendezvous)
    {
        if (!mutate)
        {
            return;
        }

        repository.UseUserRowLock = false;
        repository.AfterReads = rendezvous.ArriveAsync;
    }

    // ---- M5: bỏ kiểm tra "đã nhận XP hôm nay cho câu này" ----

    [RequiresDatabaseFact]
    public async Task Mutation_M5_SkipAlreadyTodayCheck_ReplayFarmingTestTurnsRed()
    {
        await AlreadyTodayScenarioAsync(null);
        await AssertRedAsync("M5", () => AlreadyTodayScenarioAsync(r => r.SkipAlreadyTodayCheck = true));
    }

    // Khẳng định của Submit_SameQuestionNextSessionSameDay_ZeroXpAlreadyToday.
    private async Task AlreadyTodayScenarioAsync(Action<PracticeRepository>? mutate)
    {
        var me = await _data.UserAsync("me");
        var room = await _data.FinishedRoomAsync(await _data.UserAsync("host"), [me, await _data.UserAsync()], questions: 1);
        var first = await _data.StartAsync(me, room);
        await _data.AnswerCurrentAsync(me, first, PracticeTestData.Correct);
        var second = await _data.StartAsync(me, room);

        var result = await SubmitCurrentAsync(me, second, PracticeTestData.Correct, null, mutate);

        Assert.True(result.IsCorrect);
        Assert.Equal((0, "already_today"), (result.XpAwarded, result.XpReason));
        Assert.Equal(5, await _data.TotalXpAsync(me.Id));
    }

    // ---- M6: bỏ nhánh phát lại ----

    [RequiresDatabaseFact]
    public async Task Mutation_M6_SkipReplay_IdempotencyTestsTurnRed()
    {
        await ReplayScenarioAsync(null);
        await AssertRedAsync("M6", () => ReplayScenarioAsync(r => r.SkipReplay = true));
    }

    // Khẳng định của Submit_SameQuestionAgain... và Submit_AgainWithDifferentSelectedIndex...
    private async Task ReplayScenarioAsync(Action<PracticeRepository>? mutate)
    {
        var me = await _data.UserAsync("me");
        var room = await _data.FinishedRoomAsync(await _data.UserAsync("host"), [me, await _data.UserAsync()], questions: 3);
        var sessionId = await _data.StartAsync(me, room);
        var (questionId, first) = await _data.AnswerCurrentAsync(me, sessionId, PracticeTestData.Wrong);

        await using var db = fixture.CreateContext();
        var again = await PracticeTestData.Service(db, PracticeTestData.Noon, configure: r => mutate?.Invoke(r))
            .SubmitAnswerAsync(me.Id, sessionId, new SubmitPracticeAnswerRequest(questionId, PracticeTestData.Correct), CancellationToken.None);

        Assert.False(again.IsCorrect);
        Assert.Equal(first.XpReason, again.XpReason);
        Assert.Equal(0, again.XpAwarded);
        Assert.Equal(0, await _data.TotalXpAsync(me.Id));
    }

    // ---- M7: POST /sessions bỏ ON CONFLICT ----

    [RequiresDatabaseFact]
    public async Task Mutation_M7_PlainInsertInCreateSession_ParallelPostTestTurnsRed()
    {
        await ParallelPostScenarioAsync(mutate: false);
        await AssertRedAsync("M7", () => ParallelPostScenarioAsync(mutate: true));
    }

    // Khẳng định của CreateSession_TwoParallelRequests_ReturnSameSessionId.
    private async Task ParallelPostScenarioAsync(bool mutate)
    {
        var me = await _data.UserAsync("me");
        var room = await _data.FinishedRoomAsync(await _data.UserAsync("host"), [me, await _data.UserAsync()]);

        var (outcomes, stats) = await ConcurrentRun.RunCatchingAsync(fixture, 4, (_, db) =>
            PracticeTestData.Service(db, PracticeTestData.Noon, configure: r => r.UseOnConflict = !mutate)
                .StartSessionAsync(me.Id, new StartPracticeSessionRequest(room.Room.Code), CancellationToken.None));

        ConcurrentRun.AssertReallyConcurrent(stats);
        Assert.All(outcomes, o => Assert.True(o.Succeeded, o.Error?.ToString()));
        Assert.Single(outcomes.Select(o => o.Value!.Session.SessionId).Distinct());
    }

    // ---- M8 (thêm): đọc rồi ghi không nguyên tử + không khóa => trần đồng thời bị vượt ----

    [RequiresDatabaseFact]
    public async Task Mutation_M8_NonAtomicReserveWithoutLock_ConcurrentCapTestTurnsRed()
    {
        await ConcurrentCapScenarioAsync(mutate: false);
        await AssertRedAsync("M8", () => ConcurrentCapScenarioAsync(mutate: true));
    }

    // Khẳng định của Cap_ParallelSubmitsOverCap_NeverExceedsCap (thu nhỏ: 4 phòng x 5 XP, trần 5).
    private async Task ConcurrentCapScenarioAsync(bool mutate)
    {
        const int tasks = 4;
        var options = new PracticeOptions(5, 5);
        var (me, sessions, questions) = await PracticeCapTests.SeedParallelAsync(_data, tasks);
        var rendezvous = new Rendezvous(tasks, TimeSpan.FromSeconds(1.5));

        var (outcomes, stats) = await ConcurrentRun.RunCatchingAsync(fixture, tasks, (i, db) =>
            PracticeTestData.Service(db, PracticeTestData.Noon, options, r =>
            {
                if (mutate)
                {
                    r.ReserveVariant = PracticeReserveVariant.NonAtomic;
                }

                ConfigureMutation(r, mutate, rendezvous);
            }).SubmitAnswerAsync(me.Id, sessions[i], new SubmitPracticeAnswerRequest(questions[i], PracticeTestData.Correct), CancellationToken.None));

        ConcurrentRun.AssertReallyConcurrent(stats);
        Assert.All(outcomes, o => Assert.True(o.Succeeded, o.Error?.ToString()));
        Assert.Equal(5, outcomes.Sum(o => o.Value!.XpAwarded));
        Assert.Equal(5, await _data.DailyXpAsync(me.Id, Day));
        await using var check = fixture.CreateContext();
        await XpInvariant.AssertAsync(check, [me.Id]);
    }

    // ---- Chung ----

    private async Task<PracticeAnswerResultDto> SubmitCurrentAsync(
        User user, Guid sessionId, int selectedIndex, PracticeOptions? options, Action<PracticeRepository>? mutate)
    {
        await using var db = fixture.CreateContext();
        var service = PracticeTestData.Service(db, PracticeTestData.Noon, options, r => mutate?.Invoke(r));
        var state = await service.GetSessionAsync(user.Id, sessionId, CancellationToken.None);
        return await service.SubmitAnswerAsync(user.Id, sessionId, new SubmitPracticeAnswerRequest(state.Current!.QuestionId, selectedIndex), CancellationToken.None);
    }

    /// <summary>
    /// Điểm hẹn cho các giao dịch KHÔNG khóa: mỗi bên đến thì chờ tới khi đủ số người hoặc hết hạn. Hết hạn (khi khóa Users còn bật nên bên
    /// sau bị chặn trước khi tới đây) thì đi tiếp, nên bản gốc không bị treo.
    /// </summary>
    private sealed class Rendezvous(int participants, TimeSpan timeout)
    {
        private int _arrived;
        private readonly TaskCompletionSource _all = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task ArriveAsync()
        {
            if (Interlocked.Increment(ref _arrived) >= participants)
            {
                _all.TrySetResult();
            }

            await Task.WhenAny(_all.Task, Task.Delay(timeout));
        }
    }
}
