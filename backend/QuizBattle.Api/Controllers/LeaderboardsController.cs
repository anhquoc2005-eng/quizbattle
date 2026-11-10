using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizBattle.Application.Profile;

namespace QuizBattle.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/leaderboards")]
public sealed class LeaderboardsController(IProfileService profileService) : ControllerBase
{
    [HttpGet("xp")]
    [ProducesResponseType<IReadOnlyList<XpLeaderboardEntryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<XpLeaderboardEntryDto>>> GetXp(CancellationToken cancellationToken) =>
        Ok(await profileService.GetXpLeaderboardAsync(cancellationToken));
}
