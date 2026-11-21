using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;
using QuizBattle.Tests.TestHelpers;

namespace QuizBattle.Tests.Integration;

/// <summary>Bất biến XP hai nguồn (trận + ôn tập) và độ nhạy của chính helper <see cref="XpInvariant"/>.</summary>
[Collection(DatabaseCollection.Name)]
public sealed class XpInvariantTests(DatabaseFixture fixture)
{
    private static readonly DateOnly Day1 = new(2026, 11, 21);
    private static readonly DateOnly Day2 = new(2026, 11, 22);

    [RequiresDatabaseFact]
    public async Task XpInvariant_UserWithMatchAndPracticeRows_Holds()
    {
        // 80 XP từ trận + (5 + 3) hôm 21 + 5 hôm 22 từ ôn tập = 93.
        var user = await SeedAsync(totalXp: 93, dailyDay1: 8, dailyDay2: 5);

        await using var db = fixture.CreateContext();
        await XpInvariant.AssertAsync(db, [user.Id]);
    }

    [RequiresDatabaseFact]
    public async Task XpInvariant_TotalXpMissingPracticeXp_Fails()
    {
        // TotalXp chỉ tính XP trận (80): bất biến cũ sẽ đúng, bất biến hai nguồn phải bắt được.
        var user = await SeedAsync(totalXp: 80, dailyDay1: 8, dailyDay2: 5);

        await using var db = fixture.CreateContext();
        await Assert.ThrowsAnyAsync<Xunit.Sdk.XunitException>(() => XpInvariant.AssertAsync(db, [user.Id]));
    }

    [RequiresDatabaseFact]
    public async Task XpInvariant_DailyCounterDiffersFromAnswers_Fails()
    {
        var user = await SeedAsync(totalXp: 93, dailyDay1: 9, dailyDay2: 5); // bộ đếm ngày 21 là 9, tổng câu trả lời là 8

        await using var db = fixture.CreateContext();
        await Assert.ThrowsAnyAsync<Xunit.Sdk.XunitException>(() => XpInvariant.AssertAsync(db, [user.Id]));
    }

    [RequiresDatabaseFact]
    public async Task XpInvariant_UserWithoutAnyRows_Holds()
    {
        var user = await SeedAsync(totalXp: 0, dailyDay1: 0, dailyDay2: 0, withRows: false);

        await using var db = fixture.CreateContext();
        await XpInvariant.AssertAsync(db, [user.Id]);
    }

    // Câu trả lời ôn tập: ngày 21 có hai dòng (5 + 3), ngày 22 có một dòng (5); dòng trận 80 XP. Bộ đếm ngày do tham số.
    private async Task<User> SeedAsync(int totalXp, int dailyDay1, int dailyDay2, bool withRows = true)
    {
        var id = Guid.NewGuid();
        var user = new User
        {
            Id = id, Email = $"{id:N}@test.local", PasswordHash = "h", DisplayName = "Tester",
            CreatedAt = DateTimeOffset.UtcNow, TotalXp = totalXp,
        };

        await using var db = fixture.CreateContext();
        db.Users.Add(user);
        if (withRows)
        {
            db.MatchRewards.Add(new MatchReward
            {
                Id = Guid.NewGuid(), UserId = id, RoomCode = "ABC123", RoomTopic = "Chủ đề", FinishedAt = DateTimeOffset.UtcNow,
                Status = RewardStatus.Awarded, Score = 800, RewardRank = 1, Xp = 80,
            });
            db.PracticeAnswers.AddRange(
                Answer(id, Day1, 5, null),
                Answer(id, Day1, 3, PracticeXpReason.DailyCapPartial),
                Answer(id, Day2, 5, null));
            db.PracticeXpDailies.AddRange(
                new PracticeXpDaily { UserId = id, Day = Day1, Xp = dailyDay1 },
                new PracticeXpDaily { UserId = id, Day = Day2, Xp = dailyDay2 });
        }

        await db.SaveChangesAsync();
        return user;
    }

    private static PracticeAnswer Answer(Guid userId, DateOnly day, int xp, PracticeXpReason? reason) => new()
    {
        Id = Guid.NewGuid(), UserId = userId, SelectedIndex = 0, IsCorrect = true, XpAwarded = xp, XpReason = reason,
        Day = day, AnsweredAt = DateTimeOffset.UtcNow,
    };
}
