using System.ComponentModel.DataAnnotations;
using QuizBattle.Application.Auth;
using QuizBattle.Application.Common;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Tests.Auth;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task Register_EmailWithUppercase_StoresLowercase()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository);

        await service.RegisterAsync(
            new RegisterRequest { Email = "User@Example.COM", Password = "password123", DisplayName = "Alice" },
            CancellationToken.None);

        Assert.Equal("user@example.com", repository.AddedUser?.Email);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ThrowsConflict()
    {
        var repository = new FakeUserRepository();
        repository.Seed(new User { Id = Guid.NewGuid(), Email = "user@example.com", PasswordHash = "x", DisplayName = "Alice", CreatedAt = DateTimeOffset.UtcNow });
        var service = CreateService(repository);

        await Assert.ThrowsAsync<ConflictException>(() => service.RegisterAsync(
            new RegisterRequest { Email = "user@example.com", Password = "password123", DisplayName = "Bob" },
            CancellationToken.None));
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsUnauthorized()
    {
        var repository = new FakeUserRepository();
        repository.Seed(new User { Id = Guid.NewGuid(), Email = "user@example.com", PasswordHash = FakePasswordHasher.HashOf("right-password"), DisplayName = "Alice", CreatedAt = DateTimeOffset.UtcNow });
        var service = CreateService(repository);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => service.LoginAsync(
            new LoginRequest { Email = "user@example.com", Password = "wrong-password" },
            CancellationToken.None));

        Assert.Equal("Email hoặc mật khẩu không đúng.", ex.Message);
    }

    [Fact]
    public async Task Login_UnknownEmail_ThrowsUnauthorizedWithSameMessage()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => service.LoginAsync(
            new LoginRequest { Email = "khong-ton-tai@example.com", Password = "password123" },
            CancellationToken.None));

        Assert.Equal("Email hoặc mật khẩu không đúng.", ex.Message);
    }

    [Fact]
    public void Register_WhitespaceDisplayName_ReturnsValidationError()
    {
        var request = new RegisterRequest { Email = "user@example.com", Password = "password123", DisplayName = "   " };
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterRequest.DisplayName)));
    }

    [Fact]
    public void Login_PasswordOver128Chars_ReturnsValidationError()
    {
        var request = new LoginRequest { Email = "user@example.com", Password = new string('a', 129) };
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(LoginRequest.Password)));
    }

    private static AuthService CreateService(FakeUserRepository repository) =>
        new(repository, new FakePasswordHasher(), new FakeTokenService(), TimeProvider.System);

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly List<User> _users = [];

        public User? AddedUser { get; private set; }

        public void Seed(User user) => _users.Add(user);

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(_users.SingleOrDefault(u => u.Email == email));

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_users.SingleOrDefault(u => u.Id == id));

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            AddedUser = user;
            _users.Add(user);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => HashOf(password);

        public bool Verify(string hash, string password) => hash == HashOf(password);

        public static string HashOf(string password) => $"HASHED:{password}";
    }

    private sealed class FakeTokenService : ITokenService
    {
        public AuthToken CreateToken(User user) => new("fake-token", DateTimeOffset.UtcNow.AddMinutes(60));
    }
}
