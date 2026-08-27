using CashApp.Application.CategoryGroups;
using CashApp.Application.Common.Interfaces;
using CashApp.Domain.Entities;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.Imports.Parsers;

// Parser des catégories. Colonnes attendues :
//   code, label, direction, [group], [is_active]
public class CategoryImportParser : IImportParser
{
    private const string DefaultGroupName = "Non classé";

    private readonly IAppDbContext _db;
    private readonly ICategoryGroupService _groups;

    // Empêche l'import d'un même code catégorie plusieurs fois DANS LE MÊME FICHIER
    // (en complément de la vérification contre les catégories existantes, faite en base).
    private readonly HashSet<string> _seenCodesInBatch = new(StringComparer.OrdinalIgnoreCase);

    public ImportBatchType TargetType => ImportBatchType.CATEGORIES;

    public CategoryImportParser(IAppDbContext db, ICategoryGroupService groups)
    {
        _db = db;
        _groups = groups;
    }

    public IAsyncEnumerable<ParsedRawRow> ReadAsync(Stream stream, string fileExtension, CancellationToken ct = default)
    {
        _seenCodesInBatch.Clear();
        return FileReader.ReadAnyAsync(stream, fileExtension);
    }

    public async Task<ImportValidationResult> ValidateAsync(ParsedRawRow row, int? cashRegisterId, CancellationToken ct = default)
    {
        try
        {
            var codeRaw = Get(row, "code") ?? throw new InvalidOperationException("Colonne 'code' manquante.");
            var label = Get(row, "label") ?? Get(row, "libelle") ?? throw new InvalidOperationException("Colonne 'label' manquante.");
            var directionRaw = Get(row, "direction") ?? throw new InvalidOperationException("Colonne 'direction' manquante.");
            var groupName = Get(row, "group") ?? Get(row, "groupe") ?? DefaultGroupName;
            var isActiveRaw = Get(row, "is_active");

            var code = codeRaw.Trim().ToUpperInvariant();
            if (!Enum.TryParse<OperationDirection>(directionRaw.Trim(), ignoreCase: true, out var direction))
                return new(false, $"Direction '{directionRaw}' invalide (IN ou OUT).", null);

            if (!_seenCodesInBatch.Add(code))
                return new(false, $"La catégorie '{code}' apparaît plusieurs fois dans ce fichier.", null);

            var alreadyExists = await _db.Categories.AsNoTracking().AnyAsync(c => c.Code == code, ct);
            if (alreadyExists)
                return new(false, $"La catégorie '{code}' existe déjà.", null);

            var parsed = new CategoryParsed(code, label.Trim(), direction, groupName.Trim(), ParseIsActive(isActiveRaw));
            return new(true, null, parsed);
        }
        catch (Exception ex)
        {
            return new(false, ex.Message, null);
        }
    }

    public async Task<(string EntityType, int EntityId)> ImportAsync(object parsedData, CancellationToken ct = default)
    {
        var p = (CategoryParsed)parsedData;

        // Garde-fou : une catégorie a pu être créée manuellement (ou via une autre ligne) entre la prévisualisation et la confirmation.
        if (await _db.Categories.AsNoTracking().AnyAsync(c => c.Code == p.Code, ct))
            throw new InvalidOperationException($"La catégorie '{p.Code}' existe déjà.");

        var group = await _groups.FindOrCreateAsync(p.GroupName, ct);

        var entity = new Category
        {
            Code = p.Code,
            Label = p.Label,
            Direction = p.Direction,
            IsActive = p.IsActive,
            GroupId = group.Id
        };
        _db.Categories.Add(entity);
        await _db.SaveChangesAsync(ct);
        return (nameof(Category), entity.Id);
    }

    private static string? Get(ParsedRawRow row, string key) =>
        row.Fields.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : null;

    // Colonne optionnelle : absente => actif par défaut. Accepte true/false, 1/0, oui/non.
    private static bool ParseIsActive(string? raw)
    {
        if (raw is null) return true;
        var v = raw.Trim();
        if (bool.TryParse(v, out var parsed)) return parsed;
        if (v == "1") return true;
        if (v == "0") return false;
        if (v.Equals("oui", StringComparison.OrdinalIgnoreCase)) return true;
        if (v.Equals("non", StringComparison.OrdinalIgnoreCase)) return false;
        return true;
    }
}

public record CategoryParsed(string Code, string Label, OperationDirection Direction, string GroupName, bool IsActive);
