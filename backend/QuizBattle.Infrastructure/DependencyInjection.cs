using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuizBattle.Application.Abstractions;
using QuizBattle.Application.Auth;
using QuizBattle.Infrastructure.Persistence;
using QuizBattle.Infrastructure.Security;

namespace QuizBattle.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Đăng ký database và các cài đặt cụ thể của tầng Infrastructure.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Đọc từ biến môi trường ConnectionStrings__Default hoặc user-secrets. Không đặt trong appsettings.
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Thiếu chuỗi kết nối 'ConnectionStrings:Default'. Xem hướng dẫn trong README.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IDatabaseHealthCheck, DatabaseHealthCheck>();

        // Validate ngay lúc khởi động, không chờ tới lần tạo token đầu tiên.
        services.AddSingleton(JwtOptions.Load(configuration));
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasherAdapter>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
