using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Settings;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.References;

public class ReferenceGeneratorService : IReferenceGeneratorService
{
    private readonly IAppDbContext _db;
    private readonly ISettingsService _settings;

    public ReferenceGeneratorService(IAppDbContext db, ISettingsService settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<string> NextOperationRefAsync(int cashRegisterId, DateTime operationDate, CancellationToken ct = default)
    {
        var register = await _db.CashRegisters
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == cashRegisterId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.CashRegister), cashRegisterId);

        var prefixSetting = await _settings.GetRawAsync(Domain.Constants.SettingKeys.OperationRefPrefix, ct);
        var prefix = string.IsNullOrWhiteSpace(prefixSetting) ? register.Code : prefixSetting!;

        var datePart = operationDate.ToString("yyyyMMdd");
        var prefixPattern = $"{prefix}-{datePart}-";

        // Recherche globale : `OperationRef` est unique au niveau de la table,
        // donc la séquence doit l'être aussi pour un préfixe + date donnés
        // (le préfixe peut être partagé entre plusieurs caisses via setting).
        var maxExisting = await _db.CashOperations
            .IgnoreQueryFilters()
            .Where(o => o.OperationRef.StartsWith(prefixPattern))
            .Select(o => o.OperationRef)
            .ToListAsync(ct);

        var nextSeq = maxExisting
            .Select(r => int.TryParse(r[prefixPattern.Length..], out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        return $"{prefixPattern}{nextSeq:D4}";
    }
}
