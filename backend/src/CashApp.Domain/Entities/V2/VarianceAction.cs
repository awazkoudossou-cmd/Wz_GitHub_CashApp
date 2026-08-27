using CashApp.Domain.Common;

namespace CashApp.Domain.Entities.V2;

public class VarianceAction : BaseEntity
{
    public int VarianceCaseId { get; set; }
    public VarianceCase VarianceCase { get; set; } = null!;

    public string ActionType { get; set; } = string.Empty;  // JUSTIFY / ESCALATE / WRITE_OFF / RECOVER
    public int PerformedBy { get; set; }
    public User PerformedByUser { get; set; } = null!;
    public DateTime PerformedAt { get; set; }
    public string? Comment { get; set; }
}
