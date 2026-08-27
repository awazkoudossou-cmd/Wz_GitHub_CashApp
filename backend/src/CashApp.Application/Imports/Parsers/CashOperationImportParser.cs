using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Settings;
using CashApp.Domain.Constants;
using CashApp.Domain.Entities;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.Imports.Parsers;

// Parser des opérations de caisse. Colonnes attendues :
//   date, direction, category_code, amount, payment_method, label, [description], [third_party]
public class CashOperationImportParser : IImportParser
{
    private readonly IAppDbContext _db;
    private readonly ISettingsService _settings;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IReferenceGeneratorService _refGen;

    public ImportBatchType TargetType => ImportBatchType.CASH_OPERATIONS;

    public CashOperationImportParser(IAppDbContext db, ISettingsService settings, ICurrentUserService currentUser,
        IDateTimeProvider clock, IReferenceGeneratorService refGen)
    {
        _db = db; _settings = settings; _currentUser = currentUser; _clock = clock; _refGen = refGen;
    }

    public IAsyncEnumerable<ParsedRawRow> ReadAsync(Stream stream, string fileExtension, CancellationToken ct = default)
        => FileReader.ReadAnyAsync(stream, fileExtension);

    public async Task<ImportValidationResult> ValidateAsync(ParsedRawRow row, int? cashRegisterId, CancellationToken ct = default)
    {
        try
        {
            var dateFormat = await _settings.GetRawAsync(SettingKeys.ImportDefaultDateFormat, ct) ?? "yyyy-MM-dd";
            var decSep = await _settings.GetRawAsync(SettingKeys.ImportDefaultDecimalSeparator, ct) ?? ".";

            if (!cashRegisterId.HasValue) return new(false, "CashRegisterId obligatoire pour ce batch.", null);

            var dateRaw = Get(row, "date") ?? throw new InvalidOperationException("Colonne 'date' manquante.");
            var directionRaw = Get(row, "direction") ?? throw new InvalidOperationException("Colonne 'direction' manquante.");
            var categoryCode = Get(row, "category_code") ?? Get(row, "category") ?? throw new InvalidOperationException("Colonne 'category_code' manquante.");
            var amountRaw = Get(row, "amount") ?? throw new InvalidOperationException("Colonne 'amount' manquante.");
            var paymentRaw = Get(row, "payment_method") ?? "CASH";
            var label = Get(row, "label") ?? Get(row, "libelle") ?? throw new InvalidOperationException("Colonne 'label' manquante.");

            var direction = Enum.Parse<OperationDirection>(directionRaw, ignoreCase: true);
            var paymentMethod = Enum.Parse<PaymentMethod>(paymentRaw, ignoreCase: true);
            var date = FileReader.ParseDate(dateRaw, dateFormat);
            var amount = FileReader.ParseDecimal(amountRaw, decSep);
            if (amount <= 0) return new(false, "Montant doit être > 0.", null);

            var category = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Code == categoryCode.ToUpperInvariant(), ct);
            if (category is null) return new(false, $"Catégorie '{categoryCode}' inexistante.", null);
            if (category.Direction != direction)
                return new(false, $"Catégorie '{category.Code}' ne correspond pas à la direction '{direction}'.", null);

            var session = await _db.CashSessions.AsNoTracking()
                .FirstOrDefaultAsync(s => s.CashRegisterId == cashRegisterId.Value && s.Status == CashSessionStatus.OPEN, ct);
            if (session is null) return new(false, "Aucune session ouverte sur la caisse cible.", null);

            var parsed = new CashOperationParsed(
                CashSessionId: session.Id,
                CashRegisterId: cashRegisterId.Value,
                OperationDate: date,
                Direction: direction,
                CategoryId: category.Id,
                Amount: amount,
                PaymentMethod: paymentMethod,
                Label: label.Trim(),
                Description: Get(row, "description"),
                ThirdPartyName: Get(row, "third_party") ?? Get(row, "tiers"));

            return new(true, null, parsed);
        }
        catch (Exception ex)
        {
            return new(false, ex.Message, null);
        }
    }

    public async Task<(string EntityType, int EntityId)> ImportAsync(object parsedData, CancellationToken ct = default)
    {
        var p = (CashOperationParsed)parsedData;
        var userId = _currentUser.UserId ?? throw new ForbiddenException("Non authentifié.");

        var session = await _db.CashSessions.AsNoTracking().FirstAsync(s => s.Id == p.CashSessionId, ct);
        var register = await _db.CashRegisters.AsNoTracking().FirstAsync(r => r.Id == p.CashRegisterId, ct);

        var op = new CashOperation
        {
            OperationRef = await _refGen.NextOperationRefAsync(p.CashRegisterId, p.OperationDate, ct),
            CashRegisterId = p.CashRegisterId,
            CashSessionId = p.CashSessionId,
            OperationDate = p.OperationDate,
            Direction = p.Direction,
            CategoryId = p.CategoryId,
            Amount = p.Amount,
            CurrencyCode = register.CurrencyCode,
            PaymentMethod = p.PaymentMethod,
            Label = p.Label,
            Description = p.Description,
            ThirdPartyName = p.ThirdPartyName,
            CreatedBy = userId
        };
        _db.CashOperations.Add(op);
        await _db.SaveChangesAsync(ct);
        return (nameof(CashOperation), op.Id);
    }

    private static string? Get(ParsedRawRow row, string key) =>
        row.Fields.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : null;
}

public record CashOperationParsed(
    int CashSessionId,
    int CashRegisterId,
    DateTime OperationDate,
    OperationDirection Direction,
    int CategoryId,
    decimal Amount,
    PaymentMethod PaymentMethod,
    string Label,
    string? Description,
    string? ThirdPartyName);
