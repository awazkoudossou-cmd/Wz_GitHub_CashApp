using CashApp.Domain.Common;
using CashApp.Domain.Enums;

namespace CashApp.Domain.Entities.V2;

public static class AccountingExportType
{
    public const string Batch = "BATCH";
    public const string Ledger = "LEDGER";
}

// Historique des exports comptables (Centre d'Exports) : le fichier Excel est conservé sur disque
// (FilePath) pour permettre le re-téléchargement et le réexport sans régénérer les écritures.
public class AccountingExportLog : BaseEntity
{
    public string ExportNumber { get; set; } = string.Empty;
    public string ExportType { get; set; } = AccountingExportType.Batch;

    public int? GenerationId { get; set; }
    public AccountingGeneration? Generation { get; set; }

    // Dénormalisé depuis le batch au moment de l'export, pour filtrage/affichage rapide de l'historique.
    public AccountingGenerationType? GenerationType { get; set; }
    public AccountingGenerationMode? GenerationMode { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;

    public AccountingExportStatus Status { get; set; } = AccountingExportStatus.GENERATED;

    // Sérialisation du AccountingEntryFilterDto ayant produit l'export (affiché dans le détail).
    public string? FilterJson { get; set; }
    public int ProcessingTimeMs { get; set; }
    public string? Remarks { get; set; }

    public int ExportedBy { get; set; }
    public User ExportedByUser { get; set; } = null!;
    public DateTime ExportedAt { get; set; }
    public DateTime? DownloadedAt { get; set; }

    public int LineCount { get; set; }
}
