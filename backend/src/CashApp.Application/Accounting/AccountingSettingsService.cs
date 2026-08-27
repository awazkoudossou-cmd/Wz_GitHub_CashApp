using CashApp.Application.Accounting.Dtos;
using CashApp.Application.Common.Interfaces;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.Accounting;

public class AccountingSettingsService : IAccountingSettingsService
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _audit;

    public AccountingSettingsService(IAppDbContext db, IDateTimeProvider clock, ICurrentUserService currentUser, IAuditLogger audit)
    {
        _db = db;
        _clock = clock;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<AccountingSettingsDto> GetAsync(CancellationToken ct = default)
    {
        var s = await GetOrCreateAsync(ct);
        return Map(s);
    }

    public async Task<AccountingSettingsDto> UpdateAsync(UpdateAccountingSettingsDto dto, CancellationToken ct = default)
    {
        var s = await GetOrCreateAsync(ct);

        var oldValues = new { s.GenerationType, s.GenerationMode, s.NarrationTemplate, s.IsConfigured };
        s.GenerationType = dto.GenerationType;
        s.GenerationMode = dto.GenerationMode;
        s.NarrationTemplate = dto.NarrationTemplate?.Trim();
        s.IsConfigured = dto.IsConfigured;
        s.CashAccountRootNumber = string.IsNullOrWhiteSpace(dto.CashAccountRootNumber) ? null : dto.CashAccountRootNumber.Trim();
        s.CashAccountNumberLength = dto.CashAccountNumberLength;
        s.CashJournalRootCode = string.IsNullOrWhiteSpace(dto.CashJournalRootCode) ? null : dto.CashJournalRootCode.Trim().ToUpperInvariant();
        s.UpdatedAt = _clock.UtcNow;

        await _audit.LogAsync(AuditAction.CONFIG_CHANGE, nameof(Domain.Entities.V2.AccountingSettings), s.Id,
            "Paramètres comptables modifiés", oldValues: oldValues, newValues: dto, ct: ct);
        await _db.SaveChangesAsync(ct);
        return Map(s);
    }

    private async Task<Domain.Entities.V2.AccountingSettings> GetOrCreateAsync(CancellationToken ct)
    {
        var s = await _db.AccountingSettings.FirstOrDefaultAsync(ct);
        if (s is not null) return s;

        s = new Domain.Entities.V2.AccountingSettings
        {
            GenerationType = AccountingGenerationType.DETAILED,
            GenerationMode = AccountingGenerationMode.MANUAL,
            IsConfigured = false
        };
        _db.AccountingSettings.Add(s);
        await _db.SaveChangesAsync(ct);
        return s;
    }

    private static AccountingSettingsDto Map(Domain.Entities.V2.AccountingSettings s) =>
        new(s.Id, s.GenerationType, s.GenerationMode, s.NarrationTemplate, s.IsConfigured,
            s.CashAccountRootNumber, s.CashAccountNumberLength, s.CashJournalRootCode, s.UpdatedAt);
}
