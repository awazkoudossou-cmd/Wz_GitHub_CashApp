using CashApp.Domain.Common;
using CashApp.Domain.Enums;

namespace CashApp.Domain.Entities.V2;

// Dossier de gestion d'écart attaché à une session clôturée.
public class VarianceCase : BaseEntity
{
    public int CashSessionId { get; set; }
    public CashSession CashSession { get; set; } = null!;

    public decimal VarianceAmount { get; set; }  // signé (peut être négatif)
    public string CurrencyCode { get; set; } = "XOF";

    public VarianceStatus Status { get; set; } = VarianceStatus.OPEN;

    public DateTime DetectedAt { get; set; }

    public int? ApprovalRequestId { get; set; }
    public ApprovalRequest? ApprovalRequest { get; set; }

    public int? AnomalyCaseId { get; set; }
    public AnomalyCase? AnomalyCase { get; set; }

    public DateTime? ClosedAt { get; set; }
    public int? ClosedBy { get; set; }

    public ICollection<VarianceJustification> Justifications { get; set; } = new List<VarianceJustification>();
    public ICollection<VarianceAction> Actions { get; set; } = new List<VarianceAction>();
}
