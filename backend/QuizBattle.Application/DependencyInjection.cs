using Microsoft.Extensions.DependencyInjection;
using QuizBattle.Application.Auth;
using QuizBattle.Application.Game;
using QuizBattle.Application.Health;
using QuizBattle.Application.Profile;
using QuizBattle.Application.Questions;
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
        services.AddScoped<IQuestionService, QuestionService>();
        services.AddScoped<IGameService, GameService>();
        services.AddScoped<IProfileService, ProfileService>();
        return services;
    }
}
