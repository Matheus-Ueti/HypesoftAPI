using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hypesoft.Application.Commands;
using Hypesoft.Application.Queries;

namespace Hypesoft.API.Controllers;

[ApiController]
[Route("categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoriesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _mediator.Send(new GetCategoriesQuery()));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id) =>
        Ok(await _mediator.Send(new GetCategoryByIdQuery(id)));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateCategoryBody body)
    {
        var result = await _mediator.Send(new UpdateCategoryCommand(id, body.Name, body.Description));
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _mediator.Send(new DeleteCategoryCommand(id));
        return NoContent();
    }
}

public record UpdateCategoryBody(string Name, string? Description);
