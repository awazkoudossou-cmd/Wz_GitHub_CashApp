using CashApp.Domain.Common;
using CashApp.Domain.Enums;

namespace CashApp.Domain.Entities.V2;

public class AnomalyCase : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }

    public int? CashRegisterId { get; set; }
    public CashRegister? CashRegister { get; set; }
    public int? CashSessionId { get; set; }
    public CashSession? CashSession { get; set; }

    public AnomalySeverity Severity { get; set; } = AnomalySeverity.MEDIUM;
    public AnomalyStatus Status { get; set; } = AnomalyStatus.OPEN;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTime DetectedAt { get; set; }
    public int? DetectedBy { get; set; }  // null = détection automatique
    public User? DetectedByUser { get; set; }

    public int? AssignedTo { get; set; }
    public User? AssignedToUser { get; set; }
    public DateTime? AssignedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }
    public int? ResolvedBy { get; set; }
    public User? ResolvedByUser { get; set; }
    public string? ResolutionComment { get; set; }

    public ICollection<AnomalyComment> Comments { get; set; } = new List<AnomalyComment>();
}
