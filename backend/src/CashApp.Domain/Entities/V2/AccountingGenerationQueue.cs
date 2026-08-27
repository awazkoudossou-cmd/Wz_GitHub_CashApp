using CashApp.Domain.Common;
using CashApp.Domain.Enums;

namespace CashApp.Domain.Entities.V2;

// Demande de génération en file d'attente, traitée de façon asynchrone par AccountingWorker.
// Jamais de génération directe depuis la clôture de caisse : tout passe par cette Queue.
public class AccountingGenerationQueue : BaseEntity
{
    public DateTime CreatedDate { get; set; }

    public int RequestedBy { get; set; }
    public User RequestedByUser { get; set; } = null!;

    public AccountingGenerationMode GenerationMode { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public QueueStatus Status { get; set; } = QueueStatus.PENDING;
    public int Priority { get; set; }

    // Liste des Id de CashRegister ciblés, sérialisée en JSON (null = toutes les caisses).
    public string? CashRegisterIdsJson { get; set; }

    public string? Remarks { get; set; }
    public int RetryCount { get; set; }

    public DateTime? StartedDate { get; set; }
    public DateTime? CompletedDate { get; set; }

    // Batch produit une fois la génération terminée avec succès.
    public int? ResultGenerationId { get; set; }
    public AccountingGeneration? ResultGeneration { get; set; }
}
