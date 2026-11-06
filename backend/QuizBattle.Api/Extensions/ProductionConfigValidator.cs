namespace QuizBattle.Api.Extensions;

public static class ProductionConfigValidator
{
    /// <summary>
    /// Khóa chỉ bắt buộc ở Production (khóa dùng chung như ConnectionStrings:Default, Jwt:* đã được validate ở
    /// AddInfrastructure). Ném lỗi nêu rõ tên biến môi trường để thiếu thì thấy ngay lúc khởi động.
    /// </summary>
    public static void Validate(IConfiguration configuration)
    {
        var problems = new List<string>();

        var origins = configuration["Cors:AllowedOrigins"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(origins) || origins.Split(',', StringSplitOptions.TrimEntries).Contains("*"))
        {
            problems.Add("Cors__AllowedOrigins (domain frontend, ví dụ https://quizbattle.vercel.app; không dùng *)");
        }

        if (string.IsNullOrWhiteSpace(configuration["Ai:ApiKey"]))
        {
            problems.Add("Ai__ApiKey (khóa Gemini)");
        }

        if (string.IsNullOrWhiteSpace(configuration["Ai:Model"]))
        {
            problems.Add("Ai__Model (tên model Gemini)");
        }

        if (problems.Count > 0)
        {
            throw new InvalidOperationException("Thiếu cấu hình bắt buộc ở Production. Hãy đặt biến môi trường: " + string.Join("; ", problems));
        }
    }
}
