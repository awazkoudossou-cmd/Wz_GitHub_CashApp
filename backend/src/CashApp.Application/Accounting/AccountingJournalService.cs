using CashApp.Application.Accounting.Dtos;
using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Domain.Entities.V2;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.Accounting;

public class AccountingJournalService : IAccountingJournalService
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _audit;

    public AccountingJournalService(IAppDbContext db, IDateTimeProvider clock, ICurrentUserService currentUser, IAuditLogger audit)
    {
        _db = db;
        _clock = clock;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<IReadOnlyList<AccountingJournalListItemDto>> ListAsync(CancellationToken ct = default)
    {
        return await _db.AccountingJournals.AsNoTracking()
            .OrderBy(j => j.Code)
            .Select(j => new AccountingJournalListItemDto(j.Id, j.Code, j.Name, j.Description, j.IsActive, j.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<AccountingJournalDetailDto> GetAsync(int id, CancellationToken ct = default)
    {
        var j = await _db.AccountingJournals.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(AccountingJournal), id);
        return Map(j);
    }

    public async Task<AccountingJournalDetailDto> CreateAsync(CreateAccountingJournalDto dto, CancellationToken ct = default)
    {
        var code = dto.Code.Trim().ToUpperInvariant();
        if (await _db.AccountingJournals.AnyAsync(x => x.Code == code, ct))
            throw new BusinessRuleException("JOURNAL_CODE_EXISTS", $"Le code journal '{code}' existe déjà.");

        var entity = new AccountingJournal
        {
            Code = code,
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            IsActive = true
        };
        _db.AccountingJournals.Add(entity);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditAction.CREATE, nameof(AccountingJournal), entity.Id, $"Journal {entity.Code}", newValues: dto, ct: ct);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<AccountingJournalDetailDto> UpdateAsync(int id, UpdateAccountingJournalDto dto, CancellationToken ct = default)
    {
        var entity = await _db.AccountingJournals.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(AccountingJournal), id);

        var oldValues = new { entity.Name, entity.Description };
        entity.Name = dto.Name.Trim();
        entity.Description = dto.Description?.Trim();
        entity.UpdatedAt = _clock.UtcNow;

        await _audit.LogAsync(AuditAction.UPDATE, nameof(AccountingJournal), entity.Id, $"Journal {entity.Code}", oldValues: oldValues, newValues: dto, ct: ct);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<AccountingJournalDetailDto> UpdateStatusAsync(int id, UpdateAccountingJournalStatusDto dto, CancellationToken ct = default)
    {
        var entity = await _db.AccountingJournals.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(AccountingJournal), id);

        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = _clock.UtcNow;

        await _audit.LogAsync(dto.IsActive ? AuditAction.APPROVE : AuditAction.CANCEL, nameof(AccountingJournal), entity.Id,
            dto.IsActive ? $"Journal {entity.Code} activé" : $"Journal {entity.Code} désactivé", ct: ct);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.AccountingJournals.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(AccountingJournal), id);

        var inUse = await _db.CashRegisters.AnyAsync(r => r.AccountingJournalId == id, ct)
            || await _db.AccountingEntries.AnyAsync(e => e.JournalId == id, ct);
        if (inUse)
            throw new BusinessRuleException("JOURNAL_IN_USE", "Ce journal est utilisé et ne peut pas être supprimé.");

        _db.AccountingJournals.Remove(entity);
        await _audit.LogAsync(AuditAction.DELETE, nameof(AccountingJournal), id, $"Journal {entity.Code} supprimé", ct: ct);
        await _db.SaveChangesAsync(ct);
    }

    private static AccountingJournalDetailDto Map(AccountingJournal j) =>
        new(j.Id, j.Code, j.Name, j.Description, j.IsActive, j.CreatedAt, j.UpdatedAt);
}
