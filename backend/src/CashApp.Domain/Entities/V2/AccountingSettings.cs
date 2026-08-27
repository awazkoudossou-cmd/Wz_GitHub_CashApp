using CashApp.Domain.Common;
using CashApp.Domain.Enums;

namespace CashApp.Domain.Entities.V2;

// Ligne unique (singleton) de configuration du moteur comptable.
public class AccountingSettings : BaseEntity
{
    public AccountingGenerationType GenerationType { get; set; } = AccountingGenerationType.DETAILED;
    public AccountingGenerationMode GenerationMode { get; set; } = AccountingGenerationMode.MANUAL;
    public string? NarrationTemplate { get; set; }
    public bool IsConfigured { get; set; }

    // Numérotation automatique compte/journal des caisses (V3_9).
    public string? CashAccountRootNumber { get; set; }
    public int? CashAccountNumberLength { get; set; }
    public string? CashJournalRootCode { get; set; }

    // Compteurs internes, jamais exposés/modifiables via l'API : ne font qu'augmenter,
    // pour garantir qu'un numéro/code n'est jamais réutilisé même si une caisse est supprimée.
    public int LastCashAccountSequence { get; set; }
    public int LastCashJournalSequence { get; set; }
}
