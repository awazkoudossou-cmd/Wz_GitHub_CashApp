using CashApp.Domain.Common;
using CashApp.Domain.Enums;

namespace CashApp.Domain.Entities;

public class CashOperation : AuditableEntity, ISoftDeletable
{
    public string OperationRef { get; set; } = string.Empty;

    public int CashRegisterId { get; set; }
    public CashRegister CashRegister { get; set; } = null!;

    public int CashSessionId { get; set; }
    public CashSession CashSession { get; set; } = null!;

    public DateTime OperationDate { get; set; }
    public OperationDirection Direction { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "XOF";

    public PaymentMethod PaymentMethod { get; set; }

    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ExternalReference { get; set; }
    public string? ThirdPartyName { get; set; }

    public bool IsDeleted { get; set; }
    public int? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeleteReason { get; set; }

    // V2-A : opération en attente d'approbation. Exclue des calculs de solde tant que true.
    public bool IsPendingApproval { get; set; }

    // V2-A : annulation en attente d'approbation (op reste active jusqu'à décision).
    public bool IsPendingCancellation { get; set; }
}
