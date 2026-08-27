using CashApp.Domain.Common;

namespace CashApp.Domain.Entities.V2;

public class AccountingGenerationLog : BaseEntity
{
    public int GenerationId { get; set; }
    public AccountingGeneration Generation { get; set; } = null!;

    public int PerformedBy { get; set; }
    public User PerformedByUser { get; set; } = null!;
    public DateTime PerformedAt { get; set; }

    public int OperationCount { get; set; }
    public int EntryCount { get; set; }
    public int ProcessingTimeMs { get; set; }

    // Liste JSON des motifs de mise en attente rencontrés pendant cette génération.
    public string? ErrorsJson { get; set; }
}
