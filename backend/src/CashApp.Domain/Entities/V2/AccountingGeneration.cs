using CashApp.Domain.Common;
using CashApp.Domain.Enums;

namespace CashApp.Domain.Entities.V2;

public class AccountingGeneration : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public AccountingGenerationType GenerationType { get; set; }
    public AccountingGenerationMode GenerationMode { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public AccountingGenerationStatus Status { get; set; } = AccountingGenerationStatus.DRAFT;

    public int GeneratedBy { get; set; }
    public User GeneratedByUser { get; set; } = null!;
    public DateTime GeneratedAt { get; set; }

    public bool Exported { get; set; }
    public DateTime? ExportedAt { get; set; }

    // Sert de "Batch" comptable (BatchNumber = Reference, GenerationDate = GeneratedAt) :
    // une génération produit toujours un seul batch d'écritures.
    public int TotalOperations { get; set; }
    public int TotalEntries { get; set; }
    public string? Remarks { get; set; }

    public ICollection<AccountingEntry> Entries { get; set; } = new List<AccountingEntry>();
}
