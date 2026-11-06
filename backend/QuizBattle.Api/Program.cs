using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using QuizBattle.Api.BackgroundServices;
using QuizBattle.Api.Extensions;
using QuizBattle.Api.Hubs;
using QuizBattle.Api.Middleware;
using QuizBattle.Application;
using QuizBattle.Infrastructure;
using QuizBattle.Infrastructure.Security;

const string FrontendCorsPolicy = "Frontend";
const string AuthRateLimiterPolicy = "auth";
const string GenerateQuestionsRateLimiterPolicy = "generate-questions";
const string JoinRoomRateLimiterPolicy = "join-room";

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
    ProductionConfigValidator.Validate(builder.Configuration);
}

// Render (và các nơi host tương tự) cấp cổng qua biến PORT; không có thì giữ cấu hình mặc định (dev: launchSettings, cổng 5000).
var port = builder.Configuration["PORT"];
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Validate lại ngay ở đây (cùng cấu hình AddInfrastructure đã validate) để cấu hình AddJwtBearer dùng được giá trị.
var jwtOptions = JwtOptions.Load(builder.Configuration);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Không remap "sub" sang URI dài (ClaimTypes.NameIdentifier) - giữ đúng tên claim đã phát hành ở TokenService.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30), // mặc định 5 phút, token sẽ hết hạn đúng ~60 phút như yêu cầu
        };
        options.Events = new JwtBearerEvents
        {
            // SignalR qua WebSocket không gửi được header Authorization - đọc token từ query access_token,
            // nhưng CHỈ cho path /hubs (REST vẫn phải dùng header). Mức log mặc định của
            // "Microsoft.AspNetCore" đã hạ xuống Warning (xem appsettings.Development.json) nên request có
            // access_token trong query không bị log ở Information - không thêm log nào ở đây để tránh vô
            // tình ghi lại query string chứa token.
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization();

// AddFilter<T>() là extension method của HubOptions - gọi trên AddSignalR(options => ...) để áp dụng
// cho mọi Hub, hoặc trên AddHubOptions<THub>(options => ...) để chỉ áp dụng cho 1 Hub cụ thể như ở đây
// (dự án chỉ có GameHub). Lỗi gặp lúc test tay: lúc đầu chỉ đăng ký GameHubExceptionFilter vào DI làm
// IHubFilter (AddSingleton<IHubFilter, T>()) mà KHÔNG gọi AddFilter() ở đâu cả - SignalR không tự áp
// dụng IHubFilter chỉ vì nó có trong DI, phải gọi AddFilter() tường minh thì filter mới vào pipeline.
builder.Services.AddScoped<GameHubExceptionFilter>();
builder.Services.AddSingleton<GameBroadcaster>();
builder.Services.AddHostedService<GameTimerService>();
builder.Services.AddSignalR().AddHubOptions<GameHub>(options => options.AddFilter<GameHubExceptionFilter>());

// Chặn brute-force cho register/login: tối đa 5 request/phút cho mỗi IP riêng. Vượt thì 429 (ProblemDetails qua UseStatusCodePages).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests; // mặc định của AddRateLimiter là 503, không phải 429
    options.AddPolicy(AuthRateLimiterPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));

    // Theo user (claim "sub"), không theo IP: nhiều người chơi cùng mạng/NAT không được gộp chung hạn mức.
    // UseAuthorization() chạy trước UseRateLimiter() (xem thứ tự middleware dưới) nên user đã xác thực xong.
    options.AddPolicy(GenerateQuestionsRateLimiterPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.GetUserId().ToString(),
            factory: _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromHours(1) }));

    // Endpoint ẩn danh (người chơi vào phòng, không có JWT) - chỉ chặn được theo IP. 60/phút vì cả lớp học thường
    // chung một Wi-Fi (cùng IP); số người mỗi phòng đã bị giới hạn bởi Room.MAX_PLAYERS.
    options.AddPolicy(JoinRoomRateLimiterPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1) }));

    // UseStatusCodePages tự dùng IProblemDetailsService nhưng không tự điền Title - điền ở đây cho đúng format chung.
    options.OnRejected = async (context, cancellationToken) =>
    {
        var problemDetailsService = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context.HttpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Quá nhiều yêu cầu, vui lòng thử lại sau.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.20",
            },
        });
    };
});

// Chỉ cho phép domain frontend gọi API (đọc từ Cors:AllowedOrigins, nhiều domain cách nhau bằng dấu phẩy).
var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
    options.AddPolicy(FrontendCorsPolicy, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials())); // SignalR cần AllowCredentials

var app = builder.Build();

// Phải đứng đầu: rate limiter/CORS/auth phía sau cần IP và scheme thật của khách. Chỉ bật ở Production (sau proxy của host).
if (app.Environment.IsProduction())
{
    var clientIpHeader = builder.Configuration["ForwardedHeaders:ClientIpHeader"];
    var forwardedHeadersOptions = ClientIpForwarding.CreateOptions(clientIpHeader);
    app.UseForwardedHeaders(forwardedHeadersOptions);
    app.Logger.LogInformation("ForwardedHeaders bật, đọc IP khách từ header {Header}", forwardedHeadersOptions.ForwardedForHeaderName);
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHub<GameHub>("/hubs/game");

app.Run();

// Để test tích hợp sau này có thể dùng WebApplicationFactory<Program>.
public partial class Program
{
}
