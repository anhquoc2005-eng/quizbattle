using System.Text.Json.Serialization;

namespace QuizBattle.Application.Health;

/// <summary>Kết quả kiểm tra sức khỏe hệ thống trả về cho client.</summary>
public sealed record HealthResponse(string Status, string Database)
{
    public const string StatusOk = "ok";
    public const string StatusDegraded = "degraded";
    public const string DatabaseUp = "up";
    public const string DatabaseDown = "down";

    /// <summary>Dùng nội bộ để chọn mã HTTP, không gửi xuống client.</summary>
    [JsonIgnore]
    public bool IsHealthy => Database == DatabaseUp;
}
