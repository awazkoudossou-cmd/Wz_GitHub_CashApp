using CashApp.Application.ThirdParties;
using CashApp.Application.ThirdParties.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/third-parties")]
[Authorize]
public class ThirdPartiesController : ControllerBase
{
    private readonly IThirdPartyService _service;
    public ThirdPartiesController(IThirdPartyService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ThirdPartyDto>>> List([FromQuery] string? search, CancellationToken ct)
        => Ok(await _service.ListAsync(search, ct));
}
