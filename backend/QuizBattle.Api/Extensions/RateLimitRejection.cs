using Microsoft.AspNetCore.RateLimiting;

namespace QuizBattle.Api.Extensions;

/// <summary>Mã lỗi `code` cho 429 của rate limiter, để client phân biệt "dồn dập" với "hết lượt trong ngày".</summary>
public static class RateLimitRejection
{
    public const string GenerateQuestionsPolicy = "generate-questions";

    public const string AiRateLimitCode = "AI_RATE_LIMIT";

    /// <summary>Chỉ policy sinh câu hỏi AI có mã; các policy khác (auth, join-room) giữ nguyên, không gắn code.</summary>
    public static string? CodeFor(string? policyName) => policyName == GenerateQuestionsPolicy ? AiRateLimitCode : null;

    public static string? CodeFor(HttpContext httpContext) =>
        CodeFor(httpContext.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName);
}
