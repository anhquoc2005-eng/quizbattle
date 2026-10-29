using QuizBattle.Application.Common;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Application.Auth;

public sealed class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    TimeProvider timeProvider) : IAuthService
{
    // Dùng chung cho mọi lý do đăng nhập thất bại để không lộ email nào đã đăng ký (xem README, mục "Hạn chế đã biết").
    private const string LoginFailedMessage = "Email hoặc mật khẩu không đúng.";

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await userRepository.FindByEmailAsync(email, cancellationToken) is not null)
        {
            throw new ConflictException("Email đã được sử dụng.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = passwordHasher.Hash(request.Password),
            CreatedAt = timeProvider.GetUtcNow(),
        };

        await userRepository.AddAsync(user, cancellationToken);

        return BuildResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await userRepository.FindByEmailAsync(email, cancellationToken);

        if (user is null || !passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            throw new UnauthorizedException(LoginFailedMessage);
        }

        return BuildResponse(user);
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedException("Người dùng không tồn tại.");

        return ToDto(user);
    }

    private AuthResponse BuildResponse(User user)
    {
        var token = tokenService.CreateToken(user);
        return new AuthResponse(token.Token, token.ExpiresAt, ToDto(user));
    }

    private static UserDto ToDto(User user) => new(user.Id, user.Email, user.DisplayName);
}
