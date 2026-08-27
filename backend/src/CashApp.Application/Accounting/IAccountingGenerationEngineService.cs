using CashApp.Application.Accounting.Dtos;

namespace CashApp.Application.Accounting;

// Seul service autorisé à produire des AccountingEntry. Aucun controller ne génère directement.
public interface IAccountingGenerationEngineService
{
    Task<AccountingGenerationPreviewDto> PreviewAsync(GenerateAccountingEntriesDto dto, CancellationToken ct = default);

    // actingUserId : permet à l'appelant (ex. AccountingWorker, hors contexte HTTP) de préciser
    // explicitement l'utilisateur pour le compte duquel la génération est effectuée. Si omis,
    // retombe sur l'utilisateur HTTP courant (comportement historique, API inchangée).
    Task<AccountingGenerationDetailDto> GenerateAsync(GenerateAccountingEntriesDto dto, CancellationToken ct = default, int? actingUserId = null);
    Task<AccountingGenerationDetailDto> GeneratePendingAsync(CancellationToken ct = default);

    // Supprime les écritures existantes du batch puis relance le moteur sur sa même période.
    // Refusé si le batch a déjà été exporté (verrouillé).
    Task<AccountingGenerationDetailDto> RegenerateAsync(int batchId, CancellationToken ct = default);
}
