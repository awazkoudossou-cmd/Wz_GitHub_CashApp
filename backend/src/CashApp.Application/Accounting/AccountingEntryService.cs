using CashApp.Application.Accounting.Dtos;
using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Common.Models;
using CashApp.Domain.Entities.V2;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.Accounting;

public class AccountingEntryService : IAccountingEntryService
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditLogger _audit;

    public AccountingEntryService(IAppDbContext db, IDateTimeProvider clock, IAuditLogger audit)
    {
        _db = db;
        _clock = clock;
        _audit = audit;
    }

    public async Task<PagedResponse<AccountingEntryListItemDto>> ListAsync(AccountingEntryFilterDto filter, CancellationToken ct = default)
    {
        var page = Math.Max(1, filter.Page);
        var size = Math.Clamp(filter.PageSize, 1, 500);

        var q = ApplyFilters(BaseQuery(), filter);
        q = ApplySort(q, filter.SortBy, filter.SortDescending);

        var total = await q.CountAsync(ct);
        var pageItems = await q.Skip((page - 1) * size).Take(size).ToListAsync(ct);
        var items = await MapManyAsync(pageItems, ct);

        return new PagedResponse<AccountingEntryListItemDto> { Items = items, Page = page, PageSize = size, TotalCount = total };
    }

    public async Task<IReadOnlyList<AccountingEntryListItemDto>> ListAllAsync(AccountingEntryFilterDto filter, CancellationToken ct = default)
    {
        var q = ApplyFilters(BaseQuery(), filter);
        q = ApplySort(q, filter.SortBy, filter.SortDescending);

        // Garde-fou export : jamais plus de 20 000 lignes en une fois.
        var all = await q.Take(20_000).ToListAsync(ct);
        return await MapManyAsync(all, ct);
    }

    public async Task<AccountingEntryDetailDto> GetAsync(int id, CancellationToken ct = default)
    {
        var e = await BaseQuery()
            .Include(x => x.CashOperation).ThenInclude(o => o!.CashRegister)
            .Include(x => x.CashOperation).ThenInclude(o => o!.Category)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(AccountingEntry), id);

        var userId = e.CashOperation?.CreatedBy ?? e.Generation.GeneratedBy;
        var userName = (await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct))?.FullName ?? string.Empty;

        return MapDetail(e, userId, userName);
    }

    public async Task<AccountingEntryDetailDto> UpdateAsync(int id, UpdateAccountingEntryDto dto, CancellationToken ct = default)
    {
        var e = await _db.AccountingEntries.Include(x => x.Generation).FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(AccountingEntry), id);

        if (e.Generation.Exported)
            throw new BusinessRuleException("ENTRY_LOCKED", "Cette écriture est verrouillée (batch exporté) et ne peut plus être modifiée.");

        if (dto.Description is not null) e.Description = dto.Description.Trim();
        if (dto.Comment is not null) e.Comment = dto.Comment.Trim();
        e.UpdatedAt = _clock.UtcNow;

        await _audit.LogAsync(AuditAction.UPDATE, nameof(AccountingEntry), e.Id, "Écriture modifiée (libellé/commentaire)",
            newValues: dto, ct: ct);
        await _db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<AccountingLedgerStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var entryCount = await _db.AccountingEntries.AsNoTracking().CountAsync(ct);
        var batchCount = await _db.AccountingGenerations.AsNoTracking().CountAsync(ct);
        var exportedBatchCount = await _db.AccountingGenerations.AsNoTracking().CountAsync(g => g.Exported, ct);
        var pendingCount = await _db.AccountingPendings.AsNoTracking().CountAsync(p => !p.Resolved, ct);
        return new AccountingLedgerStatsDto(entryCount, batchCount, exportedBatchCount, pendingCount);
    }

    private IQueryable<AccountingEntry> BaseQuery() =>
        _db.AccountingEntries.AsNoTracking()
            .Include(e => e.Generation).ThenInclude(g => g.GeneratedByUser)
            .Include(e => e.Journal)
            .Include(e => e.Account)
            .Include(e => e.CashOperation);

    private static IQueryable<AccountingEntry> ApplyFilters(IQueryable<AccountingEntry> q, AccountingEntryFilterDto filter)
    {
        if (filter.From.HasValue) q = q.Where(e => e.OperationDate >= filter.From.Value);
        if (filter.To.HasValue) q = q.Where(e => e.OperationDate <= filter.To.Value);
        if (filter.JournalId.HasValue) q = q.Where(e => e.JournalId == filter.JournalId.Value);
        if (filter.AccountId.HasValue) q = q.Where(e => e.AccountId == filter.AccountId.Value);
        if (filter.CashRegisterId.HasValue) q = q.Where(e => e.CashOperation != null && e.CashOperation.CashRegisterId == filter.CashRegisterId.Value);
        if (filter.CategoryId.HasValue) q = q.Where(e => e.CashOperation != null && e.CashOperation.CategoryId == filter.CategoryId.Value);
        if (filter.GenerationId.HasValue) q = q.Where(e => e.GenerationId == filter.GenerationId.Value);
        if (filter.UserId.HasValue)
        {
            var uid = filter.UserId.Value;
            q = q.Where(e => (e.CashOperation != null && e.CashOperation.CreatedBy == uid) || (e.CashOperation == null && e.Generation.GeneratedBy == uid));
        }
        if (filter.Locked.HasValue) q = q.Where(e => e.Generation.Exported == filter.Locked.Value);
        if (filter.GenerationType.HasValue) q = q.Where(e => e.Generation.GenerationType == filter.GenerationType.Value);
        if (filter.GenerationMode.HasValue) q = q.Where(e => e.Generation.GenerationMode == filter.GenerationMode.Value);
        if (!string.IsNullOrWhiteSpace(filter.Reference)) q = q.Where(e => e.Reference.Contains(filter.Reference));
        if (!string.IsNullOrWhiteSpace(filter.PieceNumber)) q = q.Where(e => e.PieceNumber != null && e.PieceNumber.Contains(filter.PieceNumber));
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            q = q.Where(e =>
                e.Description.Contains(s) ||
                e.Reference.Contains(s) ||
                (e.PieceNumber != null && e.PieceNumber.Contains(s)) ||
                e.Account.AccountNumber.Contains(s) ||
                e.Account.Name.Contains(s) ||
                e.Journal.Code.Contains(s) ||
                e.Generation.Reference.Contains(s));
        }
        return q;
    }

    private static IQueryable<AccountingEntry> ApplySort(IQueryable<AccountingEntry> q, string? sortBy, bool desc)
    {
        return sortBy?.Trim().ToLowerInvariant() switch
        {
            "generationdate" or "batch" => desc ? q.OrderByDescending(e => e.Generation.GeneratedAt) : q.OrderBy(e => e.Generation.GeneratedAt),
            "operationdate" => desc ? q.OrderByDescending(e => e.OperationDate) : q.OrderBy(e => e.OperationDate),
            "accountingdate" or "entrydate" => desc ? q.OrderByDescending(e => e.EntryDate) : q.OrderBy(e => e.EntryDate),
            "journal" => desc ? q.OrderByDescending(e => e.Journal.Code) : q.OrderBy(e => e.Journal.Code),
            "reference" => desc ? q.OrderByDescending(e => e.Reference) : q.OrderBy(e => e.Reference),
            "piecenumber" => desc ? q.OrderByDescending(e => e.PieceNumber) : q.OrderBy(e => e.PieceNumber),
            "account" => desc ? q.OrderByDescending(e => e.Account.AccountNumber) : q.OrderBy(e => e.Account.AccountNumber),
            "description" or "label" => desc ? q.OrderByDescending(e => e.Description) : q.OrderBy(e => e.Description),
            // SQLite ne sait pas trier un type "decimal" (stocké en TEXT) : on caste en double pour le tri uniquement.
            "debit" => desc ? q.OrderByDescending(e => (double)e.Debit) : q.OrderBy(e => (double)e.Debit),
            "credit" => desc ? q.OrderByDescending(e => (double)e.Credit) : q.OrderBy(e => (double)e.Credit),
            "status" or "etat" => desc ? q.OrderByDescending(e => e.Generation.Exported) : q.OrderBy(e => e.Generation.Exported),
            "user" or "utilisateur" => desc
                ? q.OrderByDescending(e => e.CashOperationId != null ? e.CashOperation!.CreatedBy : (int?)e.Generation.GeneratedBy)
                : q.OrderBy(e => e.CashOperationId != null ? e.CashOperation!.CreatedBy : (int?)e.Generation.GeneratedBy),
            _ => desc ? q.OrderByDescending(e => e.EntryDate).ThenByDescending(e => e.Id) : q.OrderBy(e => e.EntryDate).ThenBy(e => e.Id)
        };
    }

    private async Task<List<AccountingEntryListItemDto>> MapManyAsync(List<AccountingEntry> entries, CancellationToken ct)
    {
        var userIds = entries.Select(e => e.CashOperation?.CreatedBy ?? e.Generation.GeneratedBy).Distinct().ToList();
        var userNames = userIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        return entries.Select(e =>
        {
            var userId = e.CashOperation?.CreatedBy ?? e.Generation.GeneratedBy;
            var userName = userNames.TryGetValue(userId, out var n) ? n : string.Empty;
            return new AccountingEntryListItemDto(
                e.Id, e.GenerationId, e.Generation.Reference, e.Generation.GeneratedAt, e.Generation.Status,
                e.CashOperationId, e.CashOperation?.OperationRef,
                e.OperationDate, e.EntryDate,
                e.JournalId, e.Journal.Code,
                e.Reference, e.PieceNumber,
                e.AccountId, e.Account.AccountNumber, e.Account.Name,
                e.Description, e.Comment,
                e.Debit, e.Credit,
                userId, userName,
                e.Generation.Exported);
        }).ToList();
    }

    private static AccountingEntryDetailDto MapDetail(AccountingEntry e, int userId, string userName) => new(
        e.Id, e.GenerationId, e.Generation.Reference, e.Generation.GeneratedAt, e.Generation.Status,
        e.CashOperationId, e.CashOperation?.OperationRef, e.CashOperation?.CashSessionId,
        e.CashOperation?.CashRegisterId, e.CashOperation?.CashRegister.Code,
        e.CashOperation?.CategoryId, e.CashOperation?.Category.Code, e.CashOperation?.Category.Label,
        userId, userName,
        e.JournalId, e.Journal.Code, e.Journal.Name,
        e.AccountId, e.Account.AccountNumber, e.Account.Name,
        e.OperationDate, e.EntryDate,
        e.Reference, e.PieceNumber,
        e.Description, e.Comment,
        e.Debit, e.Credit,
        e.Generation.Exported);
}
