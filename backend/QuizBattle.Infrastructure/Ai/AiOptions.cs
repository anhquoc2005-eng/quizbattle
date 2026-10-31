using Microsoft.Extensions.Configuration;

namespace QuizBattle.Infrastructure.Ai;

/// <summary>
/// Đọc Ai:ApiKey/Ai:Model từ config. KHÔNG throw lúc khởi động (khác JwtOptions.Load) - AI là tùy chọn,
/// thiếu thì app vẫn chạy, chỉ endpoint generate trả 503.
/// </summary>
public sealed record AiOptions(string? ApiKey, string? Model)
{
    public static AiOptions Load(IConfiguration configuration) =>
        new(configuration["Ai:ApiKey"], configuration["Ai:Model"]);

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(Model);
}
