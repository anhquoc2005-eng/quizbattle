using Microsoft.AspNetCore.RateLimiting;

namespace QuizBattle.Api.Extensions;

/// <summary>Mã lỗi `code` cho 429 của rate limiter, để client phân biệt "dồn dập" với "hết lượt trong ngày".</summary>
public static class RateLimitRejection
{
    public const string GenerateQuestionsPolicy = "generate-questions";

    public const string AiRateLimitCode = "AI_RATE_LIMIT";

    public const string PracticePolicy = "practice";

    public const string PracticeRateLimitCode = "PRACTICE_RATE_LIMIT";

    /// <summary>Policy sinh câu hỏi AI và ôn tập có mã; các policy khác (auth, join-room) giữ nguyên, không gắn code.</summary>
    public static string? CodeFor(string? policyName) => policyName switch
    {
        GenerateQuestionsPolicy => AiRateLimitCode,
        PracticePolicy => PracticeRateLimitCode,
        _ => null,
    };

    public static string? CodeFor(HttpContext httpContext) =>
        CodeFor(httpContext.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName);
}
