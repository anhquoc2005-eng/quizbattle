using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QuizBattle.Api.Extensions;
using QuizBattle.Application.Practice;

namespace QuizBattle.Api.Controllers;

/// <summary>Ôn tập một mình qua REST (không SignalR). Chỉ người dùng đăng nhập; token phiên của khách không hợp lệ ở đây.</summary>
[ApiController]
[Authorize]
[EnableRateLimiting(RateLimitRejection.PracticePolicy)]
[Route("api/practice")]
public sealed class PracticeController(IPracticeService practiceService) : ControllerBase
{
    [HttpGet("rooms")]
    [ProducesResponseType<PracticeRoomsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<PracticeRoomsResponse>> GetRooms(CancellationToken cancellationToken) =>
        Ok(await practiceService.GetRoomsAsync(User.GetUserId(), cancellationToken));

    /// <summary>201 + Location khi tạo lượt mới; 200 + cùng sessionId khi (user, phòng) đã có lượt đang mở.</summary>
    [HttpPost("sessions")]
    [ProducesResponseType<PracticeSessionRefDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<PracticeSessionRefDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<PracticeSessionRefDto>> StartSession(StartPracticeSessionRequest request, CancellationToken cancellationToken)
    {
        var started = await practiceService.StartSessionAsync(User.GetUserId(), request, cancellationToken);
        return started.Created
            ? Created($"/api/practice/sessions/{started.Session.SessionId}", started.Session)
            : Ok(started.Session);
    }

    [HttpGet("sessions/{id:guid}")]
    [ProducesResponseType<PracticeSessionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<PracticeSessionDto>> GetSession(Guid id, CancellationToken cancellationToken) =>
        Ok(await practiceService.GetSessionAsync(User.GetUserId(), id, cancellationToken));

    /// <summary>Luôn 200 cho câu đã chấm (phát lại kết quả đã lưu); 409 chỉ khi câu chưa chấm mà không phải câu hiện tại.</summary>
    [HttpPost("sessions/{id:guid}/answers")]
    [ProducesResponseType<PracticeAnswerResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<PracticeAnswerResultDto>> SubmitAnswer(Guid id, SubmitPracticeAnswerRequest request, CancellationToken cancellationToken) =>
        Ok(await practiceService.SubmitAnswerAsync(User.GetUserId(), id, request, cancellationToken));
}
