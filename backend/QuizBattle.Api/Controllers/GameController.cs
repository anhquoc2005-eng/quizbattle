using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QuizBattle.Api.Extensions;
using QuizBattle.Application.Game;
using QuizBattle.Application.Rooms;

namespace QuizBattle.Api.Controllers;

[ApiController]
[Route("api/rooms/{code}")]
public sealed class GameController(IGameService gameService) : ControllerBase
{
    [Authorize]
    [HttpPost("open")]
    [ProducesResponseType<RoomResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RoomResponse>> Open(string code, CancellationToken cancellationToken)
    {
        var result = await gameService.OpenRoomAsync(User.GetUserId(), code, cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("players")]
    [EnableRateLimiting("join-room")]
    [ProducesResponseType<CreatePlayerResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CreatePlayerResponse>> CreatePlayer(string code, CreatePlayerRequest request, CancellationToken cancellationToken)
    {
        var result = await gameService.CreatePlayerAsync(code, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
