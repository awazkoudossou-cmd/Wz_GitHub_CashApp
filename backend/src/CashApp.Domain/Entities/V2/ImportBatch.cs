using CashApp.Domain.Common;
using CashApp.Domain.Enums;

namespace CashApp.Domain.Entities.V2;

public class ImportBatch : BaseEntity
{
    public string BatchRef { get; set; } = string.Empty;
    public ImportBatchType BatchType { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty;

    public int UploadedBy { get; set; }
    public User UploadedByUser { get; set; } = null!;
    public DateTime UploadedAt { get; set; }

    public ImportBatchStatus Status { get; set; } = ImportBatchStatus.UPLOADED;

    public int TotalLines { get; set; }
    public int ValidLines { get; set; }
    public int InvalidLines { get; set; }
    public int ImportedLines { get; set; }

    public string? ErrorSummaryJson { get; set; }
    public int? CashRegisterId { get; set; }
    public CashRegister? CashRegister { get; set; }

    public ICollection<ImportBatchLine> Lines { get; set; } = new List<ImportBatchLine>();
}
