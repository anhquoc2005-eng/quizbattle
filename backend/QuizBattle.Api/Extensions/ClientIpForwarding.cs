using Microsoft.AspNetCore.HttpOverrides;

namespace QuizBattle.Api.Extensions;

public static class ClientIpForwarding
{
    public const string DefaultClientIpHeader = "X-Forwarded-For";

    /// <summary>
    /// Cấu hình cho backend đứng sau proxy của nhà cung cấp hosting (Render). Không biết trước IP proxy nên xóa
    /// KnownProxies/KnownNetworks (mặc định chỉ tin loopback) - an toàn vì chỉ nhận đúng 1 giá trị (ForwardLimit = 1,
    /// lấy từ cuối header, phần do proxy gần nhất thêm vào). Rủi ro: ai gọi thẳng vào backend mà bỏ qua proxy có thể
    /// tự đặt header để né rate limit theo IP; Render không mở đường nào khác vào dịch vụ nên chấp nhận được.
    /// </summary>
    /// <param name="clientIpHeader">
    /// Header chứa IP khách. Sau Cloudflare, X-Forwarded-For có 2 IP (khách, edge) nên dùng CF-Connecting-IP.
    /// </param>
    public static ForwardedHeadersOptions CreateOptions(string? clientIpHeader)
    {
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = 1,
            ForwardedForHeaderName = string.IsNullOrWhiteSpace(clientIpHeader) ? DefaultClientIpHeader : clientIpHeader.Trim(),
        };
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
        return options;
    }
}
