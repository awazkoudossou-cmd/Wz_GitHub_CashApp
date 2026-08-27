using CashApp.Domain.Common;

namespace CashApp.Domain.Entities;

public class BackupLog : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int? CreatedBy { get; set; }
    public User? CreatedByUser { get; set; }
}
