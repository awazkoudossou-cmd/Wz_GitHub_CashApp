namespace CashApp.Application.Accounting.Dtos;

public record WizardCashRegisterDto(
    int Id,
    string Code,
    string Name,
    bool IsActive,
    int? AccountingJournalId,
    string? JournalCode,
    string? JournalName,
    int? AccountingAccountId,
    string? AccountNumber,
    string? AccountName);

public record AssignCashRegisterJournalDto(int AccountingJournalId);
public record AssignCashRegisterAccountDto(int AccountingAccountId);

public record WizardCategoryDto(
    int Id,
    string Code,
    string Label,
    bool IsActive,
    bool IsUsed,
    int? AccountingAccountId,
    string? AccountNumber,
    string? AccountName);

public record AssignCategoryAccountDto(int AccountingAccountId);

public record AccountingChecklistItemDto(string Code, string Label, bool Ok, string? Detail);
public record AccountingChecklistDto(IReadOnlyList<AccountingChecklistItemDto> Items, bool AllOk);

public record AccountingPreviewRequestDto(string Template);
public record AccountingPreviewResultDto(string Rendered);
