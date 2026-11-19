using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizBattle.Api.Extensions;
using QuizBattle.Application.AiQuota;

namespace QuizBattle.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/ai-quota")]
public sealed class AiQuotaController(IAiQuotaService aiQuotaService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<AiQuotaResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AiQuotaResponse>> Get(CancellationToken cancellationToken)
    {
        var result = await aiQuotaService.GetStatusAsync(User.GetUserId(), cancellationToken);
        return Ok(result);
    }
}
