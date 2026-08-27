using CashApp.Domain.Common;

namespace CashApp.Domain.Entities.V2;

public class VarianceJustification : BaseEntity
{
    public int VarianceCaseId { get; set; }
    public VarianceCase VarianceCase { get; set; } = null!;

    public int ProvidedBy { get; set; }
    public User ProvidedByUser { get; set; } = null!;
    public DateTime ProvidedAt { get; set; }

    public string Comment { get; set; } = string.Empty;
}
