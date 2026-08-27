using CashApp.Application.Common.Interfaces;
using CashApp.Domain.Entities.V2;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.Imports.Parsers;

// Parser du Plan Comptable. Colonnes attendues :
//   account_number, name, nature, [is_active]
public class AccountingAccountImportParser : IImportParser
{
    private readonly IAppDbContext _db;

    // Empêche l'import d'un même numéro de compte plusieurs fois DANS LE MÊME FICHIER
    // (en complément de la vérification contre le Plan Comptable existant, faite en base).
    private readonly HashSet<string> _seenAccountNumbersInBatch = new(StringComparer.OrdinalIgnoreCase);

    public ImportBatchType TargetType => ImportBatchType.ACCOUNTING_ACCOUNTS;

    public AccountingAccountImportParser(IAppDbContext db)
    {
        _db = db;
    }

    public IAsyncEnumerable<ParsedRawRow> ReadAsync(Stream stream, string fileExtension, CancellationToken ct = default)
    {
        _seenAccountNumbersInBatch.Clear();
        return FileReader.ReadAnyAsync(stream, fileExtension);
    }

    public async Task<ImportValidationResult> ValidateAsync(ParsedRawRow row, int? cashRegisterId, CancellationToken ct = default)
    {
        try
        {
            var numberRaw = Get(row, "account_number") ?? Get(row, "compte") ?? throw new InvalidOperationException("Colonne 'account_number' manquante.");
            var name = Get(row, "name") ?? Get(row, "libelle") ?? Get(row, "label") ?? throw new InvalidOperationException("Colonne 'name' manquante.");
            var natureRaw = Get(row, "nature") ?? throw new InvalidOperationException("Colonne 'nature' manquante.");
            var isActiveRaw = Get(row, "is_active");

            var number = numberRaw.Trim();
            if (!Enum.TryParse<AccountingAccountNature>(natureRaw.Trim(), ignoreCase: true, out var nature))
                return new(false, $"Nature '{natureRaw}' invalide (ASSET, LIABILITY, EXPENSE, REVENUE, BANK, CASH, EQUITY, OTHER).", null);

            if (!_seenAccountNumbersInBatch.Add(number))
                return new(false, $"Le compte '{number}' apparaît plusieurs fois dans ce fichier.", null);

            var alreadyExists = await _db.AccountingAccounts.AsNoTracking().AnyAsync(a => a.AccountNumber == number, ct);
            if (alreadyExists)
                return new(false, $"Le compte '{number}' existe déjà dans le Plan Comptable.", null);

            var parsed = new AccountingAccountParsed(number, name.Trim(), nature, ParseIsActive(isActiveRaw));
            return new(true, null, parsed);
        }
        catch (Exception ex)
        {
            return new(false, ex.Message, null);
        }
    }

    public async Task<(string EntityType, int EntityId)> ImportAsync(object parsedData, CancellationToken ct = default)
    {
        var p = (AccountingAccountParsed)parsedData;

        // Garde-fou : un compte a pu être créé manuellement (ou via une autre ligne) entre la prévisualisation et la confirmation.
        if (await _db.AccountingAccounts.AsNoTracking().AnyAsync(a => a.AccountNumber == p.AccountNumber, ct))
            throw new InvalidOperationException($"Le compte '{p.AccountNumber}' existe déjà dans le Plan Comptable.");

        var entity = new AccountingAccount
        {
            AccountNumber = p.AccountNumber,
            Name = p.Name,
            Nature = p.Nature,
            IsActive = p.IsActive
        };
        _db.AccountingAccounts.Add(entity);
        await _db.SaveChangesAsync(ct);
        return (nameof(AccountingAccount), entity.Id);
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

public record AccountingAccountParsed(string AccountNumber, string Name, AccountingAccountNature Nature, bool IsActive);
