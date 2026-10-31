using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizBattle.Api.Extensions;
using QuizBattle.Application.Rooms;

namespace QuizBattle.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/rooms")]
public sealed class RoomsController(IRoomService roomService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<RoomResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<RoomResponse>> Create(CreateRoomRequest request, CancellationToken cancellationToken)
    {
        var result = await roomService.CreateRoomAsync(User.GetUserId(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RoomResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RoomResponse>>> List(CancellationToken cancellationToken)
    {
        var result = await roomService.GetRoomsAsync(User.GetUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{code}")]
    [ProducesResponseType<RoomResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RoomResponse>> Get(string code, CancellationToken cancellationToken)
    {
        var result = await roomService.GetRoomAsync(User.GetUserId(), code, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{code}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string code, CancellationToken cancellationToken)
    {
        await roomService.DeleteRoomAsync(User.GetUserId(), code, cancellationToken);
        return NoContent();
    }
}
