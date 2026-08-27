using CashApp.Application.Accounting;
using CashApp.Application.Accounting.Dtos;
using CashApp.Application.Common.Interfaces;
using CashApp.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/accounting/wizard")]
[Authorize(Roles = RoleCodes.Supervisor)]
public class AccountingWizardController : ControllerBase
{
    private readonly IAccountingWizardService _service;
    private readonly IFeatureService _features;

    public AccountingWizardController(IAccountingWizardService service, IFeatureService features)
    {
        _service = service;
        _features = features;
    }

    [HttpGet("cash-registers")]
    public async Task<ActionResult<IReadOnlyList<WizardCashRegisterDto>>> ListCashRegisters(CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _service.ListCashRegistersAsync(ct));
    }

    [HttpPut("cash-registers/{id:int}/journal")]
    public async Task<ActionResult<WizardCashRegisterDto>> AssignJournal(int id, [FromBody] AssignCashRegisterJournalDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _service.AssignJournalAsync(id, dto, ct));
    }

    [HttpPut("cash-registers/{id:int}/account")]
    public async Task<ActionResult<WizardCashRegisterDto>> AssignAccount(int id, [FromBody] AssignCashRegisterAccountDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _service.AssignAccountAsync(id, dto, ct));
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<WizardCategoryDto>>> ListCategories(CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _service.ListCategoriesAsync(ct));
    }

    [HttpPut("categories/{id:int}/account")]
    public async Task<ActionResult<WizardCategoryDto>> AssignCategoryAccount(int id, [FromBody] AssignCategoryAccountDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _service.AssignCategoryAccountAsync(id, dto, ct));
    }

    [HttpGet("checklist")]
    public async Task<ActionResult<AccountingChecklistDto>> GetChecklist(CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _service.GetChecklistAsync(ct));
    }

    [HttpPost("preview")]
    public async Task<ActionResult<AccountingPreviewResultDto>> Preview([FromBody] AccountingPreviewRequestDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _service.PreviewAsync(dto, ct));
    }
}
