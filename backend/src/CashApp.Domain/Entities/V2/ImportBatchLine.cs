using CashApp.Domain.Common;
using CashApp.Domain.Enums;

namespace CashApp.Domain.Entities.V2;

public class ImportBatchLine : BaseEntity
{
    public int ImportBatchId { get; set; }
    public ImportBatch ImportBatch { get; set; } = null!;

    public int LineNumber { get; set; }
    public string RawDataJson { get; set; } = "{}";
    public string? ParsedDataJson { get; set; }

    public ImportLineStatus Status { get; set; } = ImportLineStatus.PENDING;
    public string? ErrorMessage { get; set; }

    public string? CreatedEntityType { get; set; }
    public int? CreatedEntityId { get; set; }
}
