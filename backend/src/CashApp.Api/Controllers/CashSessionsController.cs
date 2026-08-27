using CashApp.Application.CashSessions;
using CashApp.Application.CashSessions.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/cash-sessions")]
[Authorize]
public class CashSessionsController : ControllerBase
{
    private readonly ICashSessionService _service;
    public CashSessionsController(ICashSessionService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CashSessionListItemDto>>> List([FromQuery] int? cashRegisterId, CancellationToken ct)
        => Ok(await _service.ListAsync(cashRegisterId, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CashSessionDetailDto>> Get(int id, CancellationToken ct)
        => Ok(await _service.GetAsync(id, ct));

    [HttpPost("open")]
    public async Task<ActionResult<CashSessionDetailDto>> Open([FromBody] OpenCashSessionDto dto, CancellationToken ct)
        => Ok(await _service.OpenAsync(dto, ct));

    [HttpPost("{id:int}/close")]
    public async Task<ActionResult<CashSessionDetailDto>> Close(int id, [FromBody] CloseCashSessionDto dto, CancellationToken ct)
        => Ok(await _service.CloseAsync(id, dto, ct));

    [HttpGet("{id:int}/pending-items")]
    public async Task<ActionResult<SessionPendingItemsDto>> GetPendingItems(int id, CancellationToken ct)
        => Ok(await _service.GetPendingItemsAsync(id, ct));

    [HttpGet("opening-default")]
    public async Task<ActionResult<OpeningDefaultDto>> GetOpeningDefault([FromQuery] int cashRegisterId, CancellationToken ct)
        => Ok(await _service.GetOpeningDefaultAsync(cashRegisterId, ct));
}
