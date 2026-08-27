using CashApp.Application.CashRegisters;
using CashApp.Application.CashRegisters.Dtos;
using CashApp.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/cash-registers")]
[Authorize]
public class CashRegistersController : ControllerBase
{
    private readonly ICashRegisterService _service;
    public CashRegistersController(ICashRegisterService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CashRegisterListItemDto>>> List(CancellationToken ct)
        => Ok(await _service.ListAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CashRegisterDetailDto>> Get(int id, CancellationToken ct)
        => Ok(await _service.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = $"{RoleCodes.Admin},{RoleCodes.Supervisor}")]
    public async Task<ActionResult<CashRegisterDetailDto>> Create([FromBody] CreateCashRegisterDto dto, CancellationToken ct)
    {
        var result = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{RoleCodes.Admin},{RoleCodes.Supervisor}")]
    public async Task<ActionResult<CashRegisterDetailDto>> Update(int id, [FromBody] UpdateCashRegisterDto dto, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, dto, ct));

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = RoleCodes.Admin)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateCashRegisterStatusDto dto, CancellationToken ct)
    {
        await _service.UpdateStatusAsync(id, dto, ct);
        return NoContent();
    }
}
