using CashApp.Application.CashOperations;
using CashApp.Application.CashOperations.Dtos;
using CashApp.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/cash-operations")]
[Authorize]
public class CashOperationsController : ControllerBase
{
    private readonly ICashOperationService _service;
    public CashOperationsController(ICashOperationService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<PagedResponse<CashOperationListItemDto>>> List([FromQuery] CashOperationFilterDto filter, CancellationToken ct)
        => Ok(await _service.ListAsync(filter, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CashOperationDetailDto>> Get(int id, CancellationToken ct)
        => Ok(await _service.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<CashOperationDetailDto>> Create([FromBody] CreateCashOperationDto dto, CancellationToken ct)
    {
        var result = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CashOperationDetailDto>> Update(int id, [FromBody] UpdateCashOperationDto dto, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, dto, ct));

    [HttpPatch("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelCashOperationDto dto, CancellationToken ct)
    {
        await _service.CancelAsync(id, dto, ct);
        return NoContent();
    }
}
