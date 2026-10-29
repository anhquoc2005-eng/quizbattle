namespace QuizBattle.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
}
