using CashApp.Application.Users;
using CashApp.Application.Users.Dtos;
using CashApp.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = RoleCodes.Admin)]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users) => _users = users;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserListItemDto>>> List(CancellationToken ct)
        => Ok(await _users.ListAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDetailDto>> Get(int id, CancellationToken ct)
        => Ok(await _users.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<UserDetailDto>> Create([FromBody] CreateUserDto dto, CancellationToken ct)
    {
        var result = await _users.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDetailDto>> Update(int id, [FromBody] UpdateUserDto dto, CancellationToken ct)
        => Ok(await _users.UpdateAsync(id, dto, ct));

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateUserStatusDto dto, CancellationToken ct)
    {
        await _users.UpdateStatusAsync(id, dto, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordDto dto, CancellationToken ct)
    {
        await _users.ResetPasswordAsync(id, dto, ct);
        return NoContent();
    }
}
