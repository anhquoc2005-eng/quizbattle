using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuizBattle.Application.Abstractions;
using QuizBattle.Infrastructure.Persistence;

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

        return services;
    }
}
