using Microsoft.Extensions.DependencyInjection;
using QuizBattle.Application.Health;

namespace QuizBattle.Application;

public static class DependencyInjection
{
    /// <summary>Đăng ký các service của tầng Application.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IHealthService, HealthService>();
        return services;
    }
}
