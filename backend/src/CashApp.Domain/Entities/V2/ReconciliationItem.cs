using CashApp.Domain.Common;
using CashApp.Domain.Enums;

namespace CashApp.Domain.Entities.V2;

public class ReconciliationItem : BaseEntity
{
    public int ReconciliationBatchId { get; set; }
    public ReconciliationBatch ReconciliationBatch { get; set; } = null!;

    public string LeftEntityType { get; set; } = string.Empty;   // ex: "BankDeposit"
    public int LeftEntityId { get; set; }
    public string? RightEntityType { get; set; }                  // ex: "CashOperation"
    public int? RightEntityId { get; set; }

    public decimal? MatchedAmount { get; set; }
    public ReconciliationMatchStatus MatchStatus { get; set; } = ReconciliationMatchStatus.UNMATCHED;
    public string? Notes { get; set; }

    // V1 héritage (rétro-compat)
    public int? CashOperationId { get; set; }
    public CashOperation? CashOperation { get; set; }
    public string? ExternalReference { get; set; }
    public decimal? ExternalAmount { get; set; }
    public DateTime? ExternalDate { get; set; }
    public string? MatchComment { get; set; }
}
