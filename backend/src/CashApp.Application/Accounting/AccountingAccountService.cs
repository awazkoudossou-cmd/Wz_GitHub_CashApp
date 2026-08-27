using CashApp.Application.Accounting.Dtos;
using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Common.Models;
using CashApp.Domain.Entities.V2;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.Accounting;

public class AccountingAccountService : IAccountingAccountService
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _audit;

    public AccountingAccountService(IAppDbContext db, IDateTimeProvider clock, ICurrentUserService currentUser, IAuditLogger audit)
    {
        _db = db;
        _clock = clock;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<PagedResponse<AccountingAccountListItemDto>> ListAsync(AccountingAccountFilterDto filter, CancellationToken ct = default)
    {
        var page = Math.Max(1, filter.Page);
        var size = Math.Clamp(filter.PageSize, 1, 500);

        var q = _db.AccountingAccounts.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            q = q.Where(a => a.AccountNumber.Contains(s) || a.Name.Contains(s));
        }
        if (filter.Nature.HasValue) q = q.Where(a => a.Nature == filter.Nature.Value);
        if (filter.IsActive.HasValue) q = q.Where(a => a.IsActive == filter.IsActive.Value);

        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(a => a.AccountNumber)
            .Skip((page - 1) * size).Take(size)
            .Select(a => new AccountingAccountListItemDto(a.Id, a.AccountNumber, a.Name, a.Nature, a.IsActive, a.CreatedAt))
            .ToListAsync(ct);

        return new PagedResponse<AccountingAccountListItemDto> { Items = items, Page = page, PageSize = size, TotalCount = total };
    }

    public async Task<AccountingAccountDetailDto> GetAsync(int id, CancellationToken ct = default)
    {
        var a = await _db.AccountingAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(AccountingAccount), id);
        return Map(a);
    }

    public async Task<AccountingAccountDetailDto> CreateAsync(CreateAccountingAccountDto dto, CancellationToken ct = default)
    {
        var number = dto.AccountNumber.Trim();
        if (await _db.AccountingAccounts.AnyAsync(x => x.AccountNumber == number, ct))
            throw new BusinessRuleException("ACCOUNT_NUMBER_EXISTS", $"Le compte '{number}' existe déjà.");

        var entity = new AccountingAccount
        {
            AccountNumber = number,
            Name = dto.Name.Trim(),
            Nature = dto.Nature,
            IsActive = true
        };
        _db.AccountingAccounts.Add(entity);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditAction.CREATE, nameof(AccountingAccount), entity.Id, $"Compte {entity.AccountNumber}", newValues: dto, ct: ct);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<AccountingAccountDetailDto> UpdateAsync(int id, UpdateAccountingAccountDto dto, CancellationToken ct = default)
    {
        var entity = await _db.AccountingAccounts.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(AccountingAccount), id);

        var oldValues = new { entity.Name, entity.Nature };
        entity.Name = dto.Name.Trim();
        entity.Nature = dto.Nature;
        entity.UpdatedAt = _clock.UtcNow;

        await _audit.LogAsync(AuditAction.UPDATE, nameof(AccountingAccount), entity.Id, $"Compte {entity.AccountNumber}", oldValues: oldValues, newValues: dto, ct: ct);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<AccountingAccountDetailDto> UpdateStatusAsync(int id, UpdateAccountingAccountStatusDto dto, CancellationToken ct = default)
    {
        var entity = await _db.AccountingAccounts.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(AccountingAccount), id);

        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = _clock.UtcNow;

        await _audit.LogAsync(dto.IsActive ? AuditAction.APPROVE : AuditAction.CANCEL, nameof(AccountingAccount), entity.Id,
            dto.IsActive ? $"Compte {entity.AccountNumber} activé" : $"Compte {entity.AccountNumber} désactivé", ct: ct);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.AccountingAccounts.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(AccountingAccount), id);

        var inUse = await _db.CashRegisters.AnyAsync(r => r.AccountingAccountId == id, ct)
            || await _db.Categories.AnyAsync(c => c.AccountingAccountId == id, ct)
            || await _db.AccountingEntries.AnyAsync(e => e.AccountId == id, ct);
        if (inUse)
            throw new BusinessRuleException("ACCOUNT_IN_USE", "Ce compte est utilisé et ne peut pas être supprimé.");

        _db.AccountingAccounts.Remove(entity);
        await _audit.LogAsync(AuditAction.DELETE, nameof(AccountingAccount), id, $"Compte {entity.AccountNumber} supprimé", ct: ct);
        await _db.SaveChangesAsync(ct);
    }

    private static AccountingAccountDetailDto Map(AccountingAccount a) =>
        new(a.Id, a.AccountNumber, a.Name, a.Nature, a.IsActive, a.CreatedAt, a.UpdatedAt);
}
