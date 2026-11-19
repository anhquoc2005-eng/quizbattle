using QuizBattle.Domain.AiQuota;

namespace QuizBattle.Tests.Domain;

public sealed class VietnamDayTests
{
    [Fact]
    public void DayOf_At235959Vietnam_ReturnsSameDay()
    {
        var utc = new DateTimeOffset(2026, 11, 19, 16, 59, 59, TimeSpan.Zero); // 23:59:59 ngày 19 giờ VN

        Assert.Equal(new DateOnly(2026, 11, 19), VietnamDay.DayOf(utc));
    }

    [Fact]
    public void DayOf_At000000Vietnam_ReturnsNextDay()
    {
        var utc = new DateTimeOffset(2026, 11, 19, 17, 0, 0, TimeSpan.Zero); // 00:00:00 ngày 20 giờ VN

        Assert.Equal(new DateOnly(2026, 11, 20), VietnamDay.DayOf(utc));
    }

    [Fact]
    public void DayOf_AcrossYearBoundary_ReturnsNextYearDay()
    {
        var utc = new DateTimeOffset(2026, 12, 31, 17, 0, 0, TimeSpan.Zero);

        Assert.Equal(new DateOnly(2027, 1, 1), VietnamDay.DayOf(utc));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(-5)]
    public void DayOf_SameInstantWithDifferentOffsets_ReturnsSameDay(int offsetHours)
    {
        // Cùng một thời điểm (17:00:00Z) biểu diễn bằng offset khác nhau phải cho cùng một ngày VN.
        var instant = new DateTimeOffset(2026, 11, 19, 17, 0, 0, TimeSpan.Zero).ToOffset(TimeSpan.FromHours(offsetHours));

        Assert.Equal(new DateOnly(2026, 11, 20), VietnamDay.DayOf(instant));
    }

    [Fact]
    public void NextResetAt_ReturnsMidnightVietnamOfNextDay()
    {
        var reset = VietnamDay.NextResetAt(new DateOnly(2026, 11, 19));

        Assert.Equal(TimeSpan.FromHours(7), reset.Offset);
        Assert.Equal(new DateTimeOffset(2026, 11, 20, 0, 0, 0, TimeSpan.FromHours(7)), reset);
        Assert.Equal(new DateTimeOffset(2026, 11, 19, 17, 0, 0, TimeSpan.Zero), reset.ToUniversalTime());
    }

    [Fact]
    public void NextResetAt_IsExactlyWhereDayOfRollsOver()
    {
        var day = new DateOnly(2026, 11, 19);
        var reset = VietnamDay.NextResetAt(day);

        Assert.Equal(day, VietnamDay.DayOf(reset.AddTicks(-1)));
        Assert.Equal(day.AddDays(1), VietnamDay.DayOf(reset));
    }
}
