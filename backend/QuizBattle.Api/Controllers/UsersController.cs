using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizBattle.Api.Extensions;
using QuizBattle.Application.Avatars;
using QuizBattle.Application.Profile;

namespace QuizBattle.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users/me")]
public sealed class UsersController(IProfileService profileService, IAvatarService avatarService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ProfileDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProfileDto>> GetProfile(CancellationToken cancellationToken) =>
        Ok(await profileService.GetProfileAsync(User.GetUserId(), cancellationToken));

    [HttpGet("rewards")]
    [ProducesResponseType<IReadOnlyList<RewardHistoryItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RewardHistoryItemDto>>> GetRewards([FromQuery] int limit = 20, CancellationToken cancellationToken = default) =>
        Ok(await profileService.GetRewardsAsync(User.GetUserId(), limit, cancellationToken));

    /// <summary>Nhân vật hiện tại (đã lưu hoặc tự sinh) và danh sách món bán đã sở hữu.</summary>
    [HttpGet("avatar")]
    [ProducesResponseType<MyAvatarDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MyAvatarDto>> GetAvatar(CancellationToken cancellationToken) =>
        Ok(await avatarService.GetMyAvatarAsync(User.GetUserId(), cancellationToken));

    /// <summary>Thay toàn bộ nhân vật; một lỗi thì không lưu gì.</summary>
    [HttpPut("avatar")]
    [ProducesResponseType<SavedAvatarDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SavedAvatarDto>> SaveAvatar(SaveAvatarRequest request, CancellationToken cancellationToken) =>
        Ok(await avatarService.SaveMyAvatarAsync(User.GetUserId(), request, cancellationToken));
}
