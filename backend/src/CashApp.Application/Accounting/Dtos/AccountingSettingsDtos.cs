using CashApp.Domain.Enums;

namespace CashApp.Application.Accounting.Dtos;

public record AccountingSettingsDto(
    int Id,
    AccountingGenerationType GenerationType,
    AccountingGenerationMode GenerationMode,
    string? NarrationTemplate,
    bool IsConfigured,
    string? CashAccountRootNumber,
    int? CashAccountNumberLength,
    string? CashJournalRootCode,
    DateTime? UpdatedAt);

public record UpdateAccountingSettingsDto(
    AccountingGenerationType GenerationType,
    AccountingGenerationMode GenerationMode,
    string? NarrationTemplate,
    bool IsConfigured,
    string? CashAccountRootNumber = null,
    int? CashAccountNumberLength = null,
    string? CashJournalRootCode = null);
