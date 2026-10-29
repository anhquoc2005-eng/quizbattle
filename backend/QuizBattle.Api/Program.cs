using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using QuizBattle.Api.Middleware;
using QuizBattle.Application;
using QuizBattle.Infrastructure;
using QuizBattle.Infrastructure.Security;

const string FrontendCorsPolicy = "Frontend";
const string AuthRateLimiterPolicy = "auth";

var builder = WebApplication.CreateBuilder(args);

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
        };
    });
builder.Services.AddAuthorization();

// Chặn brute-force cho register/login: tối đa 5 request/phút cho mỗi IP riêng. Vượt thì 429 (ProblemDetails qua UseStatusCodePages).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests; // mặc định của AddRateLimiter là 503, không phải 429
    options.AddPolicy(AuthRateLimiterPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));

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

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.Run();

// Để test tích hợp sau này có thể dùng WebApplicationFactory<Program>.
public partial class Program
{
}
