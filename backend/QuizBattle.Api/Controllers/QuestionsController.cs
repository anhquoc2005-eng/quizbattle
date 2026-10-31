using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QuizBattle.Api.Extensions;
using QuizBattle.Application.Questions;

namespace QuizBattle.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/rooms/{code}/questions")]
public sealed class QuestionsController(IQuestionService questionService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<QuestionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<QuestionResponse>>> List(string code, CancellationToken cancellationToken)
    {
        var result = await questionService.GetQuestionsAsync(User.GetUserId(), code, cancellationToken);
        return Ok(result);
    }

    [HttpPost("generate")]
    [EnableRateLimiting("generate-questions")]
    [ProducesResponseType<GenerateQuestionsResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<GenerateQuestionsResponse>> Generate(string code, GenerateQuestionsRequest request, CancellationToken cancellationToken)
    {
        var result = await questionService.GenerateAsync(User.GetUserId(), code, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost]
    [ProducesResponseType<QuestionResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<QuestionResponse>> Add(string code, QuestionInput input, CancellationToken cancellationToken)
    {
        var result = await questionService.AddAsync(User.GetUserId(), code, input, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<QuestionResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<QuestionResponse>> Update(string code, Guid id, QuestionInput input, CancellationToken cancellationToken)
    {
        var result = await questionService.UpdateAsync(User.GetUserId(), code, id, input, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType<QuestionResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<QuestionResponse>> Approve(string code, Guid id, CancellationToken cancellationToken)
    {
        var result = await questionService.ApproveAsync(User.GetUserId(), code, id, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string code, Guid id, CancellationToken cancellationToken)
    {
        await questionService.DeleteAsync(User.GetUserId(), code, id, cancellationToken);
        return NoContent();
    }
}
