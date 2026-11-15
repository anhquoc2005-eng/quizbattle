using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuizBattle.Api.Controllers;
using QuizBattle.Application.Avatars;
using QuizBattle.Application.Profile;
using QuizBattle.Application.Shop;

namespace QuizBattle.Tests.Api;

/// <summary>
/// Hình dạng HTTP của các endpoint cửa hàng và nhân vật: mã trạng thái thành công, Location, yêu cầu đăng nhập và tính chặt của JSON.
/// Ánh xạ lỗi nghiệp vụ → 400/404/409 + code nằm ở GlobalExceptionHandlerTests và test của service.
/// </summary>
public sealed class ShopAndAvatarControllerTests
{
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
    private static readonly AvatarDto Avatar = new("variant01", "variant01", "variant01", "variant01", "variant01", "variant01", null, null, null, null, "d4d4d4");

    private static ControllerContext ContextFor(Guid userId) => new()
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", userId.ToString())], "test")),
        },
    };

    [Fact]
    public async Task Purchase_Success_Returns201WithLocationAndBody()
    {
        var shop = new RecordingShopService();
        var controller = new ShopController(shop) { ControllerContext = ContextFor(UserId) };

        var result = await controller.Purchase(new PurchaseRequest("hair.variant03"), CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.Equal("/api/users/me/avatar", created.Location);
        Assert.Equal(new PurchaseResultDto("hair.variant03", 50, 70), created.Value);
        Assert.Equal((UserId, "hair.variant03"), shop.LastPurchase);
    }

    [Fact]
    public async Task GetAvatar_ReturnsOkWithMyAvatarOfCurrentUser()
    {
        var avatars = new RecordingAvatarService();
        var controller = new UsersController(new NoProfileService(), avatars) { ControllerContext = ContextFor(UserId) };

        var result = await controller.GetAvatar(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(new MyAvatarDto(Avatar, true, []).Avatar, Assert.IsType<MyAvatarDto>(ok.Value).Avatar);
        Assert.Equal(UserId, avatars.LastUserId);
    }

    [Fact]
    public async Task SaveAvatar_ReturnsOkWithSavedAvatar_AndPassesWholeRequest()
    {
        var avatars = new RecordingAvatarService();
        var controller = new UsersController(new NoProfileService(), avatars) { ControllerContext = ContextFor(UserId) };
        var request = new SaveAvatarRequest("variant01", "variant01", "variant01", "variant01", "variant01", "variant01", null, null, null, null, null);

        var result = await controller.SaveAvatar(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.False(Assert.IsType<SavedAvatarDto>(ok.Value).IsDefault);
        Assert.Equal((UserId, request), (avatars.LastUserId, avatars.LastRequest));
    }

    [Fact]
    public void CatalogIsPublic_PurchaseAndAvatarEndpointsRequireLogin()
    {
        static T? Attr<T>(Type type, string method) where T : Attribute =>
            type.GetMethod(method)!.GetCustomAttributes(typeof(T), inherit: true).OfType<T>().FirstOrDefault();

        Assert.NotNull(Attr<AllowAnonymousAttribute>(typeof(ShopController), nameof(ShopController.GetItems)));
        Assert.NotNull(Attr<AuthorizeAttribute>(typeof(ShopController), nameof(ShopController.Purchase)));
        Assert.Null(Attr<AllowAnonymousAttribute>(typeof(ShopController), nameof(ShopController.Purchase)));
        // UsersController có [Authorize] ở cấp lớp, và hai endpoint avatar không được bật AllowAnonymous.
        Assert.NotNull(typeof(UsersController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).FirstOrDefault());
        Assert.Null(Attr<AllowAnonymousAttribute>(typeof(UsersController), nameof(UsersController.GetAvatar)));
        Assert.Null(Attr<AllowAnonymousAttribute>(typeof(UsersController), nameof(UsersController.SaveAvatar)));
    }

    [Fact]
    public void SaveAvatarRequest_UnknownSlotInJson_IsRejected()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<SaveAvatarRequest>("""{"eyes":"variant01","wings":"big"}""", options));
    }

    [Fact]
    public void SaveAvatarRequest_MissingSlotsBindAsNull_SoTheServiceReportsValidationFailed()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        var request = JsonSerializer.Deserialize<SaveAvatarRequest>("""{"eyes":"variant01"}""", options)!;

        Assert.Equal("variant01", request.Eyes);
        Assert.Null(request.Hair);
    }

    [Fact]
    public void AvatarDto_SerializesSlotNamesInCamelCaseIncludingNullOptionalSlots()
    {
        var json = JsonSerializer.Serialize(Avatar, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Contains("\"clothesGraphic\":null", json);
        Assert.Contains("\"eyebrows\":\"variant01\"", json);
        Assert.Contains("\"background\":\"d4d4d4\"", json);
    }

    private sealed class RecordingShopService : IShopService
    {
        public (Guid UserId, string? ItemKey)? LastPurchase { get; private set; }

        public Task<IReadOnlyList<AvatarItemDto>> GetCatalogAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AvatarItemDto>>([]);

        public Task<PurchaseResultDto> PurchaseAsync(Guid userId, string? itemKey, CancellationToken cancellationToken)
        {
            LastPurchase = (userId, itemKey);
            return Task.FromResult(new PurchaseResultDto(itemKey!, 50, 70));
        }
    }

    private sealed class RecordingAvatarService : IAvatarService
    {
        public Guid? LastUserId { get; private set; }

        public SaveAvatarRequest? LastRequest { get; private set; }

        public Task<MyAvatarDto> GetMyAvatarAsync(Guid userId, CancellationToken cancellationToken)
        {
            LastUserId = userId;
            return Task.FromResult(new MyAvatarDto(Avatar, true, []));
        }

        public Task<SavedAvatarDto> SaveMyAvatarAsync(Guid userId, SaveAvatarRequest request, CancellationToken cancellationToken)
        {
            (LastUserId, LastRequest) = (userId, request);
            return Task.FromResult(new SavedAvatarDto(Avatar, false));
        }
    }

    private sealed class NoProfileService : IProfileService
    {
        public Task<ProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<RewardHistoryItemDto>> GetRewardsAsync(Guid userId, int limit, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<XpLeaderboardEntryDto>> GetXpLeaderboardAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
