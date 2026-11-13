using QuizBattle.Application.Common;
using QuizBattle.Application.Shop;
using QuizBattle.Domain.Avatars;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;
using QuizBattle.Tests.TestHelpers;

namespace QuizBattle.Tests.Shop;

public sealed class ShopServicePurchaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 13, 9, 0, 0, TimeSpan.Zero);

    private static AvatarItem FromCatalog(string key)
    {
        var d = AvatarCatalog.FindByKey(key)!;
        return new AvatarItem { Key = d.Key, Slot = d.Slot, VariantKey = d.VariantKey, Name = d.Name, Tier = d.Tier, Price = d.Price, SortOrder = d.SortOrder };
    }

    // Món thật từ danh mục: hair.variant03 Thường 50, gesture.ok Hiếm 150, hair.variant01 miễn phí.
    private static (ShopService Service, FakeAvatarRepository Repository, User User) Setup(int balance)
    {
        var repository = new FakeAvatarRepository();
        repository.Catalog.AddRange(new[] { "hair.variant03", "gesture.ok", "hair.variant01" }.Select(FromCatalog));
        var user = new User
        {
            Id = Guid.NewGuid(), Email = "u@test.local", PasswordHash = "h", DisplayName = "U", CreatedAt = Now, CoinBalance = balance,
        };
        repository.Users.Add(user);
        return (new ShopService(repository, new FakeTimeProvider(Now)), repository, user);
    }

    private static void AssertNothingChanged(FakeAvatarRepository repository, User user, int balance)
    {
        Assert.Equal(balance, user.CoinBalance);
        Assert.Empty(repository.Ownerships);
        Assert.Empty(repository.CoinTransactions);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("hair")]
    [InlineData("hair.")]
    [InlineData(".variant03")]
    [InlineData("hair.variant03.x")]
    [InlineData("hair variant03")]
    public async Task Purchase_InvalidKeyFormat_ThrowsValidationFailed(string? key)
    {
        var (service, repository, user) = Setup(100);

        var ex = await Assert.ThrowsAsync<RequestValidationException>(() => service.PurchaseAsync(user.Id, key, CancellationToken.None));

        Assert.Equal("validation_failed", ex.Code);
        Assert.Equal("itemKey", ex.Field);
        AssertNothingChanged(repository, user, 100);
    }

    [Fact]
    public async Task Purchase_KeyLongerThan80_ThrowsValidationFailed()
    {
        var (service, _, user) = Setup(100);

        var ex = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.PurchaseAsync(user.Id, "hair." + new string('a', 80), CancellationToken.None));

        Assert.Equal("validation_failed", ex.Code);
    }

    [Fact]
    public async Task Purchase_UnknownItem_ThrowsNotFoundItemNotFound()
    {
        var (service, repository, user) = Setup(100);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.PurchaseAsync(user.Id, "hair.variant99", CancellationToken.None));

        Assert.Equal("item_not_found", ex.Code);
        AssertNothingChanged(repository, user, 100);
    }

    [Fact]
    public async Task Purchase_FreeItem_ThrowsValidationItemNotPurchasable()
    {
        var (service, repository, user) = Setup(100);

        var ex = await Assert.ThrowsAsync<RequestValidationException>(() => service.PurchaseAsync(user.Id, "hair.variant01", CancellationToken.None));

        Assert.Equal("item_not_purchasable", ex.Code);
        AssertNothingChanged(repository, user, 100);
    }

    [Fact]
    public async Task Purchase_AlreadyOwned_ThrowsConflictAlreadyOwned()
    {
        var (service, repository, user) = Setup(100);
        await service.PurchaseAsync(user.Id, "hair.variant03", CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => service.PurchaseAsync(user.Id, "hair.variant03", CancellationToken.None));

        Assert.Equal("already_owned", ex.Code);
        Assert.Equal(50, user.CoinBalance); // chỉ trừ một lần
        Assert.Single(repository.Ownerships);
        Assert.Single(repository.CoinTransactions);
    }

    [Fact]
    public async Task Purchase_InsufficientCoins_ThrowsConflictInsufficientCoins()
    {
        var (service, repository, user) = Setup(149);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => service.PurchaseAsync(user.Id, "gesture.ok", CancellationToken.None));

        Assert.Equal("insufficient_coins", ex.Code);
        AssertNothingChanged(repository, user, 149);
    }

    [Fact]
    public async Task Purchase_ExactlyEnoughCoins_SucceedsAndBalanceIsZero()
    {
        var (service, _, user) = Setup(150);

        var result = await service.PurchaseAsync(user.Id, "gesture.ok", CancellationToken.None);

        Assert.Equal(0, result.CoinBalance);
        Assert.Equal(0, user.CoinBalance);
    }

    [Fact]
    public async Task Purchase_OwnedAndPoor_ThrowsAlreadyOwnedNotInsufficient()
    {
        var (service, repository, user) = Setup(50);
        await service.PurchaseAsync(user.Id, "hair.variant03", CancellationToken.None); // còn 0 Xu

        var ex = await Assert.ThrowsAsync<ConflictException>(() => service.PurchaseAsync(user.Id, "hair.variant03", CancellationToken.None));

        Assert.Equal("already_owned", ex.Code); // kiểm sở hữu trước kiểm số dư
        Assert.Single(repository.Ownerships);
    }

    [Fact]
    public async Task Purchase_Success_ReturnsPriceAndNewBalance_AndWritesOwnershipAndNegativeLedgerRow()
    {
        var (service, repository, user) = Setup(120);

        var result = await service.PurchaseAsync(user.Id, "hair.variant03", CancellationToken.None);

        Assert.Equal(new PurchaseResultDto("hair.variant03", 50, 70), result);
        Assert.Equal(70, user.CoinBalance);
        var ownership = Assert.Single(repository.Ownerships);
        Assert.Equal((user.Id, "hair.variant03", Now), (ownership.UserId, ownership.ItemKey, ownership.AcquiredAt));
        var ledger = Assert.Single(repository.CoinTransactions);
        Assert.Equal(
            (user.Id, -50, CoinTransactionReason.ShopPurchase, (Guid?)ownership.Id, Now),
            (ledger.UserId, ledger.Amount, ledger.Reason, ledger.UserAvatarItemId, ledger.CreatedAt));
        Assert.Equal(user.CoinBalance, 120 + repository.CoinTransactions.Sum(c => c.Amount)); // sổ cái khớp số dư
    }

    [Fact]
    public async Task Purchase_PriceComesFromCatalogNotFromClient()
    {
        var (service, repository, user) = Setup(1000);

        var result = await service.PurchaseAsync(user.Id, "gesture.ok", CancellationToken.None);

        Assert.Equal(repository.Catalog.Single(i => i.Key == "gesture.ok").Price, result.Price);
        Assert.Equal(850, result.CoinBalance);
    }

    [Fact]
    public async Task Purchase_UnknownUser_ThrowsNotFound()
    {
        var (service, _, _) = Setup(100);

        await Assert.ThrowsAsync<NotFoundException>(() => service.PurchaseAsync(Guid.NewGuid(), "hair.variant03", CancellationToken.None));
    }
}
