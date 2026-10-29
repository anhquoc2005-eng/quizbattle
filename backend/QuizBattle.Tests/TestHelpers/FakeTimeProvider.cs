namespace QuizBattle.Tests.TestHelpers;

/// <summary>TimeProvider cố định, điều khiển được trong test, không phụ thuộc đồng hồ hệ thống.</summary>
public sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
