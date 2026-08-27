using CashApp.Domain.Common;
using CashApp.Domain.Enums;

namespace CashApp.Domain.Entities.V2;

// Trace de chaque action sur une demande d'approbation (historique).
public class ApprovalAction : BaseEntity
{
    public int ApprovalRequestId { get; set; }
    public ApprovalRequest ApprovalRequest { get; set; } = null!;

    public AuditAction ActionType { get; set; }
    public int PerformedBy { get; set; }
    public User PerformedByUser { get; set; } = null!;
    public DateTime PerformedAt { get; set; }
    public string? Comment { get; set; }
}
