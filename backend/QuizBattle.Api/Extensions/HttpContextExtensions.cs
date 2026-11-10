using Microsoft.IdentityModel.Tokens;
using QuizBattle.Application.Common;

namespace QuizBattle.Api.Extensions;

/// <summary>Lý do JWT bị từ chối, ghi vào HttpContext.Items ở JwtBearerEvents.OnAuthenticationFailed.</summary>
public static class AuthFailure
{
    public const string ItemKey = "auth.failure";

    public const string TokenExpired = "token_expired";

    public const string TokenInvalid = "token_invalid";

    public static string From(Exception exception) =>
        exception is SecurityTokenExpiredException ? TokenExpired : TokenInvalid;
}

public static class HttpContextExtensions
{
    /// <summary>
    /// Endpoint [AllowAnonymous] mà vẫn nhận diện được người dùng đã đăng nhập: JwtBearer nuốt lỗi token ở endpoint ẩn danh
    /// (User chỉ là ẩn danh), nên phải phân biệt "không gửi token" (khách, trả null) với "có gửi nhưng token hỏng/hết hạn"
    /// (ném 401 kèm code) - nếu không, người đã đăng nhập hết hạn sẽ âm thầm trở thành khách.
    /// </summary>
    public static Guid? GetOptionalUserId(this HttpContext context)
    {
        if (!context.Request.Headers.ContainsKey("Authorization"))
        {
            return null;
        }

        if (context.User.Identity?.IsAuthenticated == true)
        {
            return context.User.GetUserId();
        }

        var code = context.Items.TryGetValue(AuthFailure.ItemKey, out var value) && value is string reason
            ? reason
            : AuthFailure.TokenInvalid;
        throw new UnauthorizedException(
            code == AuthFailure.TokenExpired ? "Phiên đăng nhập đã hết hạn." : "Phiên đăng nhập không hợp lệ.",
            code);
    }
}
