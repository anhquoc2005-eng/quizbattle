using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizBattle.Api.Extensions;
using QuizBattle.Application.Shop;

namespace QuizBattle.Api.Controllers;

[ApiController]
[Route("api/shop")]
public sealed class ShopController(IShopService shopService) : ControllerBase
{
    /// <summary>Danh mục món đồ, công khai. Danh mục chỉ đổi theo migration nên cho phép cache 1 giờ.</summary>
    [HttpGet("items")]
    [AllowAnonymous]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType<IReadOnlyList<AvatarItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AvatarItemDto>>> GetItems(CancellationToken cancellationToken) =>
        Ok(await shopService.GetCatalogAsync(cancellationToken));

    /// <summary>Mua một món đồ bằng Xu. Client chỉ gửi mã món; giá do server quyết định.</summary>
    [HttpPost("purchases")]
    [Authorize]
    [ProducesResponseType<PurchaseResultDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PurchaseResultDto>> Purchase(PurchaseRequest request, CancellationToken cancellationToken)
    {
        var result = await shopService.PurchaseAsync(User.GetUserId(), request.ItemKey, cancellationToken);
        return Created("/api/users/me/avatar", result);
    }
}
