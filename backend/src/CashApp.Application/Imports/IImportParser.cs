using CashApp.Domain.Enums;

namespace CashApp.Application.Imports;

// Représente une ligne brute lue d'un fichier (Excel ou CSV).
public record ParsedRawRow(int LineNumber, IReadOnlyDictionary<string, string?> Fields);

// Représente une ligne validée et prête à être insérée.
public record ImportValidationResult(bool IsValid, string? ErrorMessage, object? ParsedData);

// Pipeline d'import par type de cible (Operations / Categories…).
public interface IImportParser
{
    ImportBatchType TargetType { get; }

    // Lit le fichier physique et retourne les lignes brutes.
    IAsyncEnumerable<ParsedRawRow> ReadAsync(Stream stream, string fileExtension, CancellationToken ct = default);

    // Valide une ligne brute. Retourne IsValid + ParsedData prêt à persister.
    Task<ImportValidationResult> ValidateAsync(ParsedRawRow row, int? cashRegisterId, CancellationToken ct = default);

    // Persiste la ligne (création d'entité). Retourne l'id de l'entité créée.
    Task<(string EntityType, int EntityId)> ImportAsync(object parsedData, CancellationToken ct = default);
}
