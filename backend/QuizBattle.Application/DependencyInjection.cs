using Microsoft.Extensions.DependencyInjection;
using QuizBattle.Application.Auth;
using QuizBattle.Application.Health;

namespace QuizBattle.Application;

public static class DependencyInjection
{
    /// <summary>Đăng ký các service của tầng Application.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IHealthService, HealthService>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
