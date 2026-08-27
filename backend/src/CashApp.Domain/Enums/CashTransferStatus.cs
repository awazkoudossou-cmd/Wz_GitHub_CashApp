namespace CashApp.Domain.Enums;

public enum CashTransferStatus
{
    DRAFT = 1,
    PENDING_APPROVAL = 2,
    APPROVED = 3,
    REJECTED = 4,
    COMPLETED = 5,
    CANCELLED = 6
}
