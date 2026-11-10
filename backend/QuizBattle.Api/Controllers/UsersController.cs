using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizBattle.Api.Extensions;
using QuizBattle.Application.Profile;

namespace QuizBattle.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users/me")]
public sealed class UsersController(IProfileService profileService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ProfileDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProfileDto>> GetProfile(CancellationToken cancellationToken) =>
        Ok(await profileService.GetProfileAsync(User.GetUserId(), cancellationToken));

    [HttpGet("rewards")]
    [ProducesResponseType<IReadOnlyList<RewardHistoryItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RewardHistoryItemDto>>> GetRewards([FromQuery] int limit = 20, CancellationToken cancellationToken = default) =>
        Ok(await profileService.GetRewardsAsync(User.GetUserId(), limit, cancellationToken));
}
