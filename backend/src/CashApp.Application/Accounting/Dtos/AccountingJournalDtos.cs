namespace CashApp.Application.Accounting.Dtos;

public record AccountingJournalListItemDto(
    int Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAt);

public record AccountingJournalDetailDto(
    int Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateAccountingJournalDto(string Code, string Name, string? Description);
public record UpdateAccountingJournalDto(string Name, string? Description);
public record UpdateAccountingJournalStatusDto(bool IsActive);
