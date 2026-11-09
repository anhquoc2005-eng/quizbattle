using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using QuizBattle.Api.Extensions;
using QuizBattle.Application.Common;

namespace QuizBattle.Tests.Api;

public sealed class HttpContextExtensionsTests
{
    [Fact]
    public void GetOptionalUserId_NoAuthorizationHeader_ReturnsNull()
    {
        var context = new DefaultHttpContext();

        Assert.Null(context.GetOptionalUserId());
    }

    [Fact]
    public void GetOptionalUserId_ValidToken_ReturnsUserId()
    {
        var userId = Guid.NewGuid();
        var context = WithBearer();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())], "Bearer"));

        Assert.Equal(userId, context.GetOptionalUserId());
    }

    [Fact]
    public void GetOptionalUserId_ExpiredToken_ThrowsTokenExpired()
    {
        var context = WithBearer();
        context.Items[AuthFailure.ItemKey] = AuthFailure.From(new SecurityTokenExpiredException());

        var ex = Assert.Throws<UnauthorizedException>(() => context.GetOptionalUserId());

        Assert.Equal("token_expired", ex.Code);
    }

    [Fact]
    public void GetOptionalUserId_GarbageToken_ThrowsTokenInvalid()
    {
        var context = WithBearer();
        context.Items[AuthFailure.ItemKey] = AuthFailure.From(new SecurityTokenInvalidSignatureException());

        var ex = Assert.Throws<UnauthorizedException>(() => context.GetOptionalUserId());

        Assert.Equal("token_invalid", ex.Code);
    }

    [Fact]
    public void GetOptionalUserId_HeaderButNotAuthenticatedAndNoReason_ThrowsTokenInvalid()
    {
        // Ví dụ scheme không phải Bearer: không có sự kiện lỗi nhưng cũng không xác thực được - không được coi là khách.
        var context = WithBearer();

        var ex = Assert.Throws<UnauthorizedException>(() => context.GetOptionalUserId());

        Assert.Equal("token_invalid", ex.Code);
    }

    [Fact]
    public void AuthFailureFrom_ExpiredException_ReturnsTokenExpired() =>
        Assert.Equal("token_expired", AuthFailure.From(new SecurityTokenExpiredException()));

    [Fact]
    public void AuthFailureFrom_AnyOtherException_ReturnsTokenInvalid() =>
        Assert.Equal("token_invalid", AuthFailure.From(new SecurityTokenException("sai định dạng")));

    private static DefaultHttpContext WithBearer()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer token-bat-ky";
        return context;
    }
}
