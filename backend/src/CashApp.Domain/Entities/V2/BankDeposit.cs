using CashApp.Domain.Common;
using CashApp.Domain.Enums;

namespace CashApp.Domain.Entities.V2;

public class BankDeposit : AuditableEntity
{
    public string DepositRef { get; set; } = string.Empty;

    public int CashRegisterId { get; set; }
    public CashRegister CashRegister { get; set; } = null!;
    public int? CashSessionId { get; set; }
    public CashSession? CashSession { get; set; }

    public DateTime DepositDate { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "XOF";

    public string BankName { get; set; } = string.Empty;
    public string? AccountReference { get; set; }
    public string? DepositSlipReference { get; set; }
    public string? Description { get; set; }

    public BankDepositStatus Status { get; set; } = BankDepositStatus.DRAFT;

    public int RequestedBy { get; set; }
    public User RequestedByUser { get; set; } = null!;

    public int? ApprovedBy { get; set; }
    public User? ApprovedByUser { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    // OUT op générée à la finalisation.
    public int? LinkedOperationId { get; set; }
    public CashOperation? LinkedOperation { get; set; }

    public int? ApprovalRequestId { get; set; }
    public ApprovalRequest? ApprovalRequest { get; set; }
}
