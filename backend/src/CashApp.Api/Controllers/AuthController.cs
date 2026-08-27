using CashApp.Application.Auth;
using CashApp.Application.Auth.Dtos;
using CashApp.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ICurrentUserService _currentUser;

    public AuthController(IAuthService auth, ICurrentUserService currentUser)
    {
        _auth = auth;
        _currentUser = currentUser;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto dto, CancellationToken ct)
        => Ok(await _auth.LoginAsync(dto, ct));

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<LoginResponseDto>> Me(CancellationToken ct)
    {
        var id = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        return Ok(await _auth.GetCurrentContextAsync(id, ct));
    }
}
