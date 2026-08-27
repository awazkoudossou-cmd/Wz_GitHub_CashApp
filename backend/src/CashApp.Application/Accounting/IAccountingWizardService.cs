using CashApp.Application.Accounting.Dtos;

namespace CashApp.Application.Accounting;

public interface IAccountingWizardService
{
    Task<IReadOnlyList<WizardCashRegisterDto>> ListCashRegistersAsync(CancellationToken ct = default);
    Task<WizardCashRegisterDto> AssignJournalAsync(int cashRegisterId, AssignCashRegisterJournalDto dto, CancellationToken ct = default);
    Task<WizardCashRegisterDto> AssignAccountAsync(int cashRegisterId, AssignCashRegisterAccountDto dto, CancellationToken ct = default);

    Task<IReadOnlyList<WizardCategoryDto>> ListCategoriesAsync(CancellationToken ct = default);
    Task<WizardCategoryDto> AssignCategoryAccountAsync(int categoryId, AssignCategoryAccountDto dto, CancellationToken ct = default);

    Task<AccountingChecklistDto> GetChecklistAsync(CancellationToken ct = default);
    Task<AccountingPreviewResultDto> PreviewAsync(AccountingPreviewRequestDto dto, CancellationToken ct = default);
}
