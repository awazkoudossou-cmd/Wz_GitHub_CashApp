namespace CashApp.Application.Accounting.Dtos;

public record AccountingDailyCountDto(DateOnly Date, int Count);
public record AccountingNamedCountDto(string Name, int Count);

public record AccountingDashboardDto(
    int AccountCount,
    int JournalCount,
    int ConfiguredCategoryCount,
    int ConfiguredCashRegisterCount,
    int BatchCount,
    int EntryCount,
    int PendingCount,
    string? LastGenerationReference,
    DateTime? LastGenerationAt,
    string? LastExportFileName,
    DateTime? LastExportAt,
    int BatchesToday,
    int EntriesToday,
    int ExportsToday,
    int ErrorsCount,
    IReadOnlyList<AccountingDailyCountDto> EntriesByDay,
    IReadOnlyList<AccountingDailyCountDto> GenerationsByDay,
    IReadOnlyList<AccountingNamedCountDto> JournalDistribution,
    IReadOnlyList<AccountingNamedCountDto> AccountDistribution);
