namespace QuizBattle.Tests.Integration;

/// <summary>Fact chỉ chạy khi có biến môi trường QUIZBATTLE_TEST_DB; thiếu thì bị bỏ qua (không fail).</summary>
public sealed class RequiresDatabaseFactAttribute : FactAttribute
{
    public const string EnvironmentVariable = "QUIZBATTLE_TEST_DB";

    public RequiresDatabaseFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EnvironmentVariable)))
        {
            Skip = $"Thiếu biến môi trường {EnvironmentVariable}; bỏ qua test tích hợp Postgres.";
        }
    }
}
