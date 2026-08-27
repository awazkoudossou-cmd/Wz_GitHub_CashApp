using CashApp.Application.CategoryGroups;
using CashApp.Application.CategoryGroups.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/category-groups")]
[Authorize]
public class CategoryGroupsController : ControllerBase
{
    private readonly ICategoryGroupService _service;
    public CategoryGroupsController(ICategoryGroupService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryGroupDto>>> List(CancellationToken ct)
        => Ok(await _service.ListAsync(ct));
}
