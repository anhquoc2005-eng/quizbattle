using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace QuizBattle.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Đọc userId từ claim "sub" của JWT đã xác thực (xem TokenService).</summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value;
        return Guid.Parse(sub);
    }
}
