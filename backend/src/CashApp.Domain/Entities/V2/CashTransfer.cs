using CashApp.Domain.Common;
using CashApp.Domain.Enums;

namespace CashApp.Domain.Entities.V2;

public class CashTransfer : AuditableEntity
{
    public string TransferRef { get; set; } = string.Empty;

    public int SourceCashRegisterId { get; set; }
    public CashRegister SourceCashRegister { get; set; } = null!;
    public int? SourceSessionId { get; set; }
    public CashSession? SourceSession { get; set; }

    public int DestinationCashRegisterId { get; set; }
    public CashRegister DestinationCashRegister { get; set; } = null!;
    public int? DestinationSessionId { get; set; }
    public CashSession? DestinationSession { get; set; }

    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "XOF";

    public DateTime TransferDate { get; set; }
    public string Reason { get; set; } = string.Empty;

    public CashTransferStatus Status { get; set; } = CashTransferStatus.DRAFT;

    public int RequestedBy { get; set; }
    public User RequestedByUser { get; set; } = null!;

    public int? ApprovedBy { get; set; }
    public User? ApprovedByUser { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    // Liens vers les opérations miroir générées à la finalisation.
    public int? SourceOperationId { get; set; }
    public CashOperation? SourceOperation { get; set; }
    public int? DestinationOperationId { get; set; }
    public CashOperation? DestinationOperation { get; set; }

    // Lien vers la demande d'approbation si workflow requis.
    public int? ApprovalRequestId { get; set; }
    public ApprovalRequest? ApprovalRequest { get; set; }
}
