using System.IdentityModel.Tokens.Jwt;
using QuizBattle.Domain.Entities;
using QuizBattle.Infrastructure.Security;
using QuizBattle.Tests.TestHelpers;

namespace QuizBattle.Tests.Security;

public sealed class TokenServiceTests
{
    private static readonly JwtOptions Options = new(
        Key: "this-is-a-test-key-that-is-at-least-32-bytes-long",
        Issuer: "quizbattle-test",
        Audience: "quizbattle-test");

    [Fact]
    public void CreateToken_ReturnsTokenWithExpectedClaimsAndExpiry()
    {
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        var service = new TokenService(Options, timeProvider);
        var user = new User { Id = Guid.NewGuid(), Email = "user@example.com", PasswordHash = "x", DisplayName = "Alice", CreatedAt = now };

        var result = service.CreateToken(user);

        Assert.Equal(now.AddMinutes(60), result.ExpiresAt);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        Assert.Equal(user.Id.ToString(), jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(user.Email, jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(user.DisplayName, jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Name).Value);
        Assert.Equal(now.AddMinutes(60).UtcDateTime, jwt.ValidTo);
    }
}
