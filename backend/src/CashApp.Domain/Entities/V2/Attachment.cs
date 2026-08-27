using CashApp.Domain.Common;

namespace CashApp.Domain.Entities.V2;

public class Attachment : BaseEntity
{
    public string FileName { get; set; } = string.Empty;        // nom unique en stockage
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string StoredPath { get; set; } = string.Empty;      // chemin physique

    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }

    public int UploadedBy { get; set; }
    public User UploadedByUser { get; set; } = null!;
    public DateTime UploadedAt { get; set; }

    public string? Description { get; set; }
    public bool IsDeleted { get; set; }
}
