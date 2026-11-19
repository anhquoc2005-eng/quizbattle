namespace QuizBattle.Application.AiQuota;

/// <summary>Định dạng cột Scope của bảng AiUsageDaily - một chỗ duy nhất để repository, service và test dùng chung.</summary>
public static class AiQuotaScopes
{
    public const string Global = "global";

    public static string ForUser(Guid userId) => $"user:{userId:D}";
}
