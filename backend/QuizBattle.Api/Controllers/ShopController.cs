using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
}
