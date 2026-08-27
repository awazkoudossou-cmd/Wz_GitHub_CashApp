using CashApp.Domain.Enums;

namespace CashApp.Application.CashRegisters.Dtos;

public record CashRegisterListItemDto(
    int Id,
    string Code,
    string Name,
    string CurrencyCode,
    bool IsActive,
    DateTime CreatedAt,
    OperationDirection DefaultDirection,
    PaymentMethod DefaultPaymentMethod);

public record CashRegisterDetailDto(
    int Id,
    string Code,
    string Name,
    string? Description,
    string CurrencyCode,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    OperationDirection DefaultDirection,
    PaymentMethod DefaultPaymentMethod,
    int? AccountingAccountId,
    string? AccountingAccountNumber,
    int? AccountingJournalId,
    string? AccountingJournalCode);

public record CreateCashRegisterDto(
    string Code,
    string Name,
    string? Description,
    string CurrencyCode,
    OperationDirection DefaultDirection,
    PaymentMethod DefaultPaymentMethod);

public record UpdateCashRegisterDto(
    string Name,
    string? Description,
    string CurrencyCode,
    OperationDirection DefaultDirection,
    PaymentMethod DefaultPaymentMethod);

public record UpdateCashRegisterStatusDto(bool IsActive);
