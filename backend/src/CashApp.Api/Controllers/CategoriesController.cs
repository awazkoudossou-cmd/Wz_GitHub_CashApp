using CashApp.Application.Categories;
using CashApp.Application.Categories.Dtos;
using CashApp.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _service;
    public CategoriesController(ICategoryService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryListItemDto>>> List(CancellationToken ct)
        => Ok(await _service.ListAsync(ct));

    [HttpPost]
    [Authorize(Roles = $"{RoleCodes.Admin},{RoleCodes.Supervisor}")]
    public async Task<ActionResult<CategoryDetailDto>> Create([FromBody] CreateCategoryDto dto, CancellationToken ct)
        => Ok(await _service.CreateAsync(dto, ct));

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{RoleCodes.Admin},{RoleCodes.Supervisor}")]
    public async Task<ActionResult<CategoryDetailDto>> Update(int id, [FromBody] UpdateCategoryDto dto, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, dto, ct));

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = $"{RoleCodes.Admin},{RoleCodes.Supervisor}")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateCategoryStatusDto dto, CancellationToken ct)
    {
        await _service.UpdateStatusAsync(id, dto, ct);
        return NoContent();
    }
}
