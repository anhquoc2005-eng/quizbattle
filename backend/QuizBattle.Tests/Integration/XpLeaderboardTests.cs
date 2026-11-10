using Microsoft.EntityFrameworkCore;
using QuizBattle.Application.Profile;
using QuizBattle.Domain.Entities;
using QuizBattle.Infrastructure.Persistence;

namespace QuizBattle.Tests.Integration;

/// <summary>Bảng xếp hạng XP trên Postgres thật. Mỗi test dùng tiền tố tên riêng nên không phụ thuộc dữ liệu test khác.</summary>
[Collection(DatabaseCollection.Name)]
public sealed class XpLeaderboardTests(DatabaseFixture fixture)
{
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [RequiresDatabaseFact]
    public async Task XpLeaderboard_ExcludesZeroXpAndReturnsTop50InOrder()
    {
        // XP rất lớn để 55 tài khoản này luôn đứng trên mọi dữ liệu sót lại từ test khác (bảng dùng chung trong một lần chạy).
        await using (var db = fixture.CreateContext())
        {
            for (var i = 1; i <= 55; i++)
            {
                db.Users.Add(NewUser($"Rank{i:D2}", 1_000_000 + i, Start.AddMinutes(i)));
            }

            db.Users.Add(NewUser("ZeroXp", 0, Start));
            db.Users.Add(NewUser("TieLate", 1_000_055, Start.AddDays(1))); // hòa với Rank55, tạo muộn hơn nên xếp sau
            await db.SaveChangesAsync();
        }

        await using var read = fixture.CreateContext();
        var top = await new ProfileService(new UserRepository(read)).GetXpLeaderboardAsync(CancellationToken.None);

        Assert.Equal(50, top.Count);
        Assert.Equal(Enumerable.Range(1, 50), top.Select(e => e.Rank));
        Assert.Equal("Rank55", top[0].DisplayName);
        Assert.Equal("TieLate", top[1].DisplayName);
        Assert.Equal("Rank07", top[^1].DisplayName); // 56 tài khoản đủ điều kiện, lấy 50 đầu: Rank55, TieLate, rồi Rank54 ... Rank07
        Assert.DoesNotContain(top, e => e.DisplayName == "ZeroXp");
        Assert.Equal(top.Select(e => e.TotalXp).OrderByDescending(x => x), top.Select(e => e.TotalXp));
    }

    private static User NewUser(string name, int totalXp, DateTimeOffset createdAt)
    {
        var id = Guid.NewGuid();
        return new User { Id = id, Email = $"{name}-{id:N}@test.local".ToLowerInvariant(), PasswordHash = "h", DisplayName = name, CreatedAt = createdAt, TotalXp = totalXp };
    }
}
