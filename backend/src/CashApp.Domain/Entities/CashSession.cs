using CashApp.Domain.Common;
using CashApp.Domain.Enums;

namespace CashApp.Domain.Entities;

public class CashSession : BaseEntity
{
    public int CashRegisterId { get; set; }
    public CashRegister CashRegister { get; set; } = null!;

    public int OpenedBy { get; set; }
    public User OpenedByUser { get; set; } = null!;
    public DateTime OpenedAt { get; set; }
    public decimal OpeningBalance { get; set; }

    public int? ClosedBy { get; set; }
    public User? ClosedByUser { get; set; }
    public DateTime? ClosedAt { get; set; }

    public decimal? TheoreticalBalance { get; set; }
    public decimal? PhysicalBalance { get; set; }
    public decimal? VarianceAmount { get; set; }

    public CashSessionStatus Status { get; set; } = CashSessionStatus.OPEN;

    public string? OpenComment { get; set; }
    public string? CloseComment { get; set; }

    public ICollection<CashOperation> CashOperations { get; set; } = new List<CashOperation>();
}
