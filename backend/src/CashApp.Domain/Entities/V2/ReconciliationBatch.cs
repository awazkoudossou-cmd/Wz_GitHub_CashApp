using CashApp.Domain.Common;
using CashApp.Domain.Enums;

namespace CashApp.Domain.Entities.V2;

public class ReconciliationBatch : BaseEntity
{
    public string Reference { get; set; } = string.Empty;
    public ReconciliationBatchType BatchType { get; set; }

    public int? CashRegisterId { get; set; }
    public CashRegister? CashRegister { get; set; }

    public int CreatedBy { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public ReconciliationStatus Status { get; set; } = ReconciliationStatus.OPEN;
    public string? Notes { get; set; }

    public ICollection<ReconciliationItem> Items { get; set; } = new List<ReconciliationItem>();
}
