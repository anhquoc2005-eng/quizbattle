using Microsoft.EntityFrameworkCore;
using QuizBattle.Infrastructure.Persistence;

namespace QuizBattle.Tests.TestHelpers;

/// <summary>
/// Các bất biến sổ cái của tài khoản, kiểm trên DB thật (CLAUDE.md, mục Dữ liệu):
/// <c>TotalXp = SUM(MatchRewards.Xp) + SUM(PracticeAnswers.XpAwarded)</c>, <c>CoinBalance = SUM(CoinTransactions.Amount)</c>,
/// và bộ đếm ngày <c>PracticeXpDaily.Xp = SUM(PracticeAnswers.XpAwarded)</c> theo (UserId, Day).
/// </summary>
public static class XpInvariant
{
    public static async Task AssertAsync(AppDbContext db, IEnumerable<Guid> userIds)
    {
        foreach (var userId in userIds)
        {
            var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == userId);
            var matchXp = await db.MatchRewards.Where(r => r.UserId == userId).SumAsync(r => r.Xp);
            var practiceXp = await db.PracticeAnswers.Where(a => a.UserId == userId).SumAsync(a => a.XpAwarded);
            var coins = await db.CoinTransactions.Where(c => c.UserId == userId).SumAsync(c => c.Amount);
            Assert.Equal(matchXp + practiceXp, user.TotalXp);
            Assert.Equal(coins, user.CoinBalance);

            var perDay = await db.PracticeAnswers.Where(a => a.UserId == userId)
                .GroupBy(a => a.Day).Select(g => new { Day = g.Key, Xp = g.Sum(a => a.XpAwarded) }).ToListAsync();
            var counters = await db.PracticeXpDailies.AsNoTracking().Where(d => d.UserId == userId).ToListAsync();
            Assert.Equal(
                perDay.Where(d => d.Xp > 0).OrderBy(d => d.Day).Select(d => (d.Day, d.Xp)),
                counters.Where(c => c.Xp > 0).OrderBy(c => c.Day).Select(c => (c.Day, c.Xp)));
        }
    }
}
