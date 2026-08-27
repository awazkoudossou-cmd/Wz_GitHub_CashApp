namespace CashApp.Domain.Enums;

public enum ApprovalTargetType
{
    CASH_OPERATION = 1,
    CASH_OPERATION_CANCEL = 2,
    CASH_TRANSFER = 3,
    BANK_DEPOSIT = 4,
    CASH_SESSION_CLOSE = 5,
    CASH_SESSION_REOPEN = 6,
    VARIANCE_CASE = 7
}
