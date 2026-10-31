using Microsoft.Extensions.DependencyInjection;
using QuizBattle.Application.Auth;
using QuizBattle.Application.Health;
using QuizBattle.Application.Rooms;

namespace QuizBattle.Application;

public static class DependencyInjection
{
    /// <summary>Đăng ký các service của tầng Application.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IHealthService, HealthService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRoomService, RoomService>();
        return services;
    }
}
