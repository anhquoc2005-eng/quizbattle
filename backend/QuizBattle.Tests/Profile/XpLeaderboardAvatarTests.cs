using QuizBattle.Application.Avatars;
using QuizBattle.Application.Profile;
using QuizBattle.Domain.Avatars;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;
using QuizBattle.Tests.TestHelpers;

namespace QuizBattle.Tests.Profile;

/// <summary>Bảng xếp hạng XP công khai mang nhân vật đủ ô, nhưng không lộ định danh người dùng.</summary>
public sealed class XpLeaderboardAvatarTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static AvatarDto Default(Guid userId)
    {
        var s = DefaultAvatarGenerator.ForAccount(userId);
        return new AvatarDto(
            s.Get(AvatarSlot.Eyes)!, s.Get(AvatarSlot.Eyebrows)!, s.Get(AvatarSlot.Mouth)!, s.Get(AvatarSlot.Nose)!,
            s.Get(AvatarSlot.Hair)!, s.Get(AvatarSlot.Clothes)!, null, null, null, null, s.Get(AvatarSlot.Background));
    }

    private static UserAvatar Saved(Guid userId) => new()
    {
        UserId = userId, EyesItemKey = "eyes.variant02", EyebrowsItemKey = "eyebrows.variant03", MouthItemKey = "mouth.variant04",
        NoseItemKey = "nose.variant05", HairItemKey = "hair.variant62", ClothesItemKey = "clothes.variant07",
        GestureItemKey = "gesture.ok", BackgroundItemKey = "background.b9c6d6", UpdatedAt = Now,
    };

    [Fact]
    public async Task GetTop_AccountWithSavedAvatar_ShowsSaved_OthersShowStableDefault()
    {
        var repository = new FakeUserRepository();
        var withAvatar = ProfileServiceTests.NewUser("Có nhân vật", totalXp: 900);
        var withoutRow = ProfileServiceTests.NewUser("Chưa lưu", totalXp: 500);
        repository.Users.AddRange([withAvatar, withoutRow]);
        repository.UserAvatars[withAvatar.Id] = Saved(withAvatar.Id);
        var service = new ProfileService(repository);

        var top = await service.GetXpLeaderboardAsync(CancellationToken.None);
        var again = await service.GetXpLeaderboardAsync(CancellationToken.None);

        Assert.Equal(
            new AvatarDto("variant02", "variant03", "variant04", "variant05", "variant62", "variant07", null, null, "ok", null, "b9c6d6"),
            top[0].Avatar);
        Assert.Equal(Default(withoutRow.Id), top[1].Avatar);
        Assert.Equal(top[1].Avatar, again[1].Avatar); // cố định giữa các lần xem
    }

    [Fact]
    public async Task GetTop_EveryRowHasFullAvatar()
    {
        var repository = new FakeUserRepository();
        for (var i = 1; i <= 5; i++)
        {
            repository.Users.Add(ProfileServiceTests.NewUser($"U{i}", totalXp: i * 100));
        }

        var top = await new ProfileService(repository).GetXpLeaderboardAsync(CancellationToken.None);

        Assert.Equal(5, top.Count);
        Assert.All(top, e =>
        {
            Assert.All(new[] { e.Avatar.Eyes, e.Avatar.Eyebrows, e.Avatar.Mouth, e.Avatar.Nose, e.Avatar.Hair, e.Avatar.Clothes }, v => Assert.False(string.IsNullOrEmpty(v)));
            Assert.Equal("d4d4d4", e.Avatar.Background);
            Assert.DoesNotContain(e.Avatar.Hair, AvatarCatalog.HeadscarfVariantKeys);
        });
    }

    [Fact]
    public void Dto_DoesNotExposeUserIdentity()
    {
        var names = typeof(XpLeaderboardEntryDto).GetProperties().Select(p => p.Name).Concat(typeof(AvatarDto).GetProperties().Select(p => p.Name)).ToList();

        Assert.DoesNotContain(names, n => n.Contains("UserId", StringComparison.OrdinalIgnoreCase) || n is "Id" or "Email");
        Assert.Contains("Avatar", names);
    }
}
