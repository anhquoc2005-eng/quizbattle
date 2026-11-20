using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using QuizBattle.Api.BackgroundServices;
using QuizBattle.Application.Auth;
using QuizBattle.Application.Common;
using QuizBattle.Application.Questions;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Tests.Integration.Http;

/// <summary>Generator giả điều khiển được: trả câu hợp lệ, ném AiUnavailableException, hoặc trả câu sai định dạng.</summary>
public sealed class ControllableGenerator : IQuestionGenerator
{
    public enum Behavior { Valid, Unavailable, Invalid }

    public Behavior Mode { get; set; } = Behavior.Valid;
    public int Calls { get; private set; }

    public Task<IReadOnlyList<QuestionCandidate>> GenerateAsync(string topic, int count, CancellationToken cancellationToken)
    {
        Calls++;
        return Mode switch
        {
            Behavior.Unavailable => throw new AiUnavailableException("AI giả: không khả dụng."),
            // Chỉ 3 đáp án nên QuestionCandidateValidator loại hết -> BadGatewayException sau 2 lần thử.
            Behavior.Invalid => Task.FromResult<IReadOnlyList<QuestionCandidate>>([new QuestionCandidate("Câu sai", ["a", "b", "c"], 0)]),
            _ => Task.FromResult<IReadOnlyList<QuestionCandidate>>(
                Enumerable.Range(0, count).Select(i => new QuestionCandidate($"Câu {Guid.NewGuid():N}", [$"A{i}", $"B{i}", $"C{i}", $"D{i}"], 0)).ToList()),
        };
    }
}

/// <summary>
/// Host thật của API chạy trên DB tạm của DatabaseFixture. Cấu hình đặt bằng BIẾN MÔI TRƯỜNG (Program.cs đọc ngay lúc
/// CreateBuilder; ConfigureAppConfiguration quá muộn, UseSetting có thể bị user-secrets/biến máy dev đè), môi trường
/// "Testing" để user-secrets không nạp. Mỗi test một factory => bộ đếm rate limiter (trong RAM) luôn sạch.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string JwtKey = "test-only-jwt-key-0123456789-abcdefghij";
    public const string AllowedOrigin = "http://localhost:5173";

    private readonly Dictionary<string, string?> _previousEnv = [];

    public ControllableGenerator Generator { get; } = new();

    public ApiFactory(DatabaseFixture fixture, int perUserLimit = 1000)
    {
        var settings = new Dictionary<string, string>
        {
            ["ConnectionStrings__Default"] = fixture.ConnectionString ?? throw new InvalidOperationException("Chưa có DB tạm."),
            ["Jwt__Key"] = JwtKey,
            ["Jwt__Issuer"] = "quizbattle-test",
            ["Jwt__Audience"] = "quizbattle-test",
            ["Cors__AllowedOrigins"] = AllowedOrigin,
            ["Ai__ApiKey"] = "test",
            ["Ai__Model"] = "test",
            ["Ai__DailyLimitPerUser"] = perUserLimit.ToString(),
            ["Ai__DailyLimitGlobal"] = "100000", // hàng global dùng chung cả DB tạm, không test nào cần chạm trần này
        };
        foreach (var (name, value) in settings)
        {
            _previousEnv[name] = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IQuestionGenerator>();
            services.AddSingleton<IQuestionGenerator>(Generator);

            // GameTimerService quét DB mỗi giây, không test nào cần và nó sẽ chạm vào DB tạm ngoài ý muốn.
            var timer = services.Single(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(GameTimerService));
            services.Remove(timer);
        });
    }

    /// <summary>Tạo user thẳng vào DB tạm rồi lấy JWT từ ITokenService thật (không qua /register vì rate limit "auth").</summary>
    public async Task<(Guid UserId, HttpClient Client)> CreateUserClientAsync(DatabaseFixture fixture)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid():N}@test.local",
            PasswordHash = "not-a-real-hash",
            DisplayName = "Tester",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        await using (var db = fixture.CreateContext())
        {
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        using var scope = Services.CreateScope();
        var token = scope.ServiceProvider.GetRequiredService<ITokenService>().CreateToken(user);
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return (user.Id, client);
    }

    protected override void Dispose(bool disposing)
    {
        try
        {
            base.Dispose(disposing); // dispose host TRƯỚC, rồi mới trả biến môi trường
        }
        finally
        {
            foreach (var (name, value) in _previousEnv)
            {
                Environment.SetEnvironmentVariable(name, value);
            }
        }
    }
}
