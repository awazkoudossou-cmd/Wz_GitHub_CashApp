using CashApp.Domain.Common;
using CashApp.Domain.Enums;

namespace CashApp.Domain.Entities.V2;

public class AuditLog : BaseEntity
{
    public AuditAction ActionType { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }

    public int? PerformedBy { get; set; }
    public User? PerformedByUser { get; set; }
    public DateTime PerformedAt { get; set; }

    public string? Description { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? MetadataJson { get; set; }
    public string? IpAddress { get; set; }
}
