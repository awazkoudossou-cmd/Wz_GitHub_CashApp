using CashApp.Domain.Enums;

namespace CashApp.Application.Accounting.Dtos;

public record AccountingAccountListItemDto(
    int Id,
    string AccountNumber,
    string Name,
    AccountingAccountNature Nature,
    bool IsActive,
    DateTime CreatedAt);

public record AccountingAccountDetailDto(
    int Id,
    string AccountNumber,
    string Name,
    AccountingAccountNature Nature,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateAccountingAccountDto(string AccountNumber, string Name, AccountingAccountNature Nature);
public record UpdateAccountingAccountDto(string Name, AccountingAccountNature Nature);
public record UpdateAccountingAccountStatusDto(bool IsActive);

public record AccountingAccountFilterDto(
    string? Search,
    AccountingAccountNature? Nature,
    bool? IsActive,
    int Page = 1,
    int PageSize = 50);
