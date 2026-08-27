using CashApp.Application.Admin;
using CashApp.Application.Common.Exceptions;
using CashApp.Application.Settings;
using CashApp.Application.Settings.Dtos;
using CashApp.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

public record ResetDatabaseRequest(string ConfirmationCode);

[ApiController]
[Route("api/settings")]
[Authorize(Roles = RoleCodes.Admin)]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _service;
    private readonly IDatabaseAdminService _admin;
    public SettingsController(ISettingsService service, IDatabaseAdminService admin)
    {
        _service = service;
        _admin = admin;
    }

    [HttpGet("general")]
    public async Task<ActionResult<GeneralSettingsDto>> GetGeneral(CancellationToken ct)
        => Ok(await _service.GetGeneralAsync(ct));

    [HttpPut("general")]
    public async Task<ActionResult<GeneralSettingsDto>> UpdateGeneral([FromBody] GeneralSettingsDto dto, CancellationToken ct)
        => Ok(await _service.UpdateGeneralAsync(dto, ct));

    [HttpGet("app-mode")]
    public async Task<ActionResult<AppModeDto>> GetAppMode(CancellationToken ct)
        => Ok(await _service.GetAppModeAsync(ct));

    [HttpPut("app-mode")]
    public async Task<ActionResult<AppModeDto>> UpdateAppMode([FromBody] UpdateAppModeDto dto, CancellationToken ct)
        => Ok(await _service.UpdateAppModeAsync(dto, ct));

    [HttpGet("features")]
    public async Task<ActionResult<IReadOnlyList<FeatureSettingDto>>> GetFeatures(CancellationToken ct)
        => Ok(await _service.GetFeaturesAsync(ct));

    [HttpPut("features")]
    public async Task<ActionResult<IReadOnlyList<FeatureSettingDto>>> UpdateFeatures([FromBody] UpdateFeatureSettingsDto dto, CancellationToken ct)
        => Ok(await _service.UpdateFeaturesAsync(dto, ct));

    [HttpGet("company")]
    public async Task<ActionResult<CompanyInfoDto>> GetCompany(CancellationToken ct)
        => Ok(await _service.GetCompanyAsync(ct));

    [HttpPut("company")]
    public async Task<ActionResult<CompanyInfoDto>> UpdateCompany([FromBody] CompanyInfoDto dto, CancellationToken ct)
        => Ok(await _service.UpdateCompanyAsync(dto, ct));

    // Réinitialisation complète de la base. ADMIN uniquement + code de confirmation "RESET".
    [HttpPost("reset-database")]
    public async Task<IActionResult> ResetDatabase([FromBody] ResetDatabaseRequest dto, CancellationToken ct)
    {
        if (!string.Equals(dto.ConfirmationCode, "RESET", StringComparison.Ordinal))
            throw new BusinessRuleException("INVALID_CONFIRMATION_CODE",
                "Code de confirmation invalide. Tapez 'RESET' pour confirmer.");

        await _admin.ResetAsync(ct);
        return Ok(new { message = "Base réinitialisée. Reconnectez-vous." });
    }
}
