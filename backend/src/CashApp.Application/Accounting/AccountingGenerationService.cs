using CashApp.Application.Accounting.Dtos;
using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Common.Models;
using CashApp.Domain.Entities.V2;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.Accounting;

public class AccountingGenerationService : IAccountingGenerationService
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditLogger _audit;

    public AccountingGenerationService(IAppDbContext db, IDateTimeProvider clock, IAuditLogger audit)
    {
        _db = db;
        _clock = clock;
        _audit = audit;
    }

    public async Task<PagedResponse<AccountingGenerationListItemDto>> ListAsync(AccountingGenerationFilterDto filter, CancellationToken ct = default)
    {
        var page = Math.Max(1, filter.Page);
        var size = Math.Clamp(filter.PageSize, 1, 200);

        var q = _db.AccountingGenerations.AsNoTracking()
            .Include(g => g.GeneratedByUser)
            .AsQueryable();

        if (filter.Status.HasValue) q = q.Where(g => g.Status == filter.Status.Value);
        if (filter.From.HasValue) q = q.Where(g => g.StartDate >= filter.From.Value);
        if (filter.To.HasValue) q = q.Where(g => g.EndDate <= filter.To.Value);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(g => g.GeneratedAt)
            .Skip((page - 1) * size).Take(size)
            .Select(g => new AccountingGenerationListItemDto(
                g.Id, g.Reference, g.GenerationType, g.GenerationMode, g.StartDate, g.EndDate,
                g.Status, g.GeneratedBy, g.GeneratedByUser.FullName, g.GeneratedAt,
                g.TotalOperations, g.TotalEntries, g.Exported))
            .ToListAsync(ct);

        return new PagedResponse<AccountingGenerationListItemDto> { Items = items, Page = page, PageSize = size, TotalCount = total };
    }

    public async Task<AccountingGenerationDetailDto> GetAsync(int id, CancellationToken ct = default)
    {
        var g = await _db.AccountingGenerations.AsNoTracking()
            .Include(x => x.GeneratedByUser)
            .Include(x => x.Entries).ThenInclude(e => e.Journal)
            .Include(x => x.Entries).ThenInclude(e => e.Account)
            .Include(x => x.Entries).ThenInclude(e => e.CashOperation)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(AccountingGeneration), id);

        var processingTimeMs = await _db.AccountingGenerationLogs.AsNoTracking()
            .Where(l => l.GenerationId == id)
            .OrderByDescending(l => l.PerformedAt)
            .Select(l => (int?)l.ProcessingTimeMs)
            .FirstOrDefaultAsync(ct);

        return Map(g, processingTimeMs);
    }

    public async Task<AccountingGenerationDetailDto> CancelAsync(int id, CancellationToken ct = default)
    {
        var g = await _db.AccountingGenerations.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(AccountingGeneration), id);

        if (g.Status is AccountingGenerationStatus.EXPORTED or AccountingGenerationStatus.CANCELLED)
            throw new BusinessRuleException("GENERATION_INVALID_STATUS", $"Statut '{g.Status}' non annulable.");

        g.Status = AccountingGenerationStatus.CANCELLED;
        g.UpdatedAt = _clock.UtcNow;

        await _audit.LogAsync(AuditAction.CANCEL, nameof(AccountingGeneration), g.Id, $"Génération {g.Reference} annulée", ct: ct);
        await _db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var batch = await _db.AccountingGenerations.FirstOrDefaultAsync(g => g.Id == id, ct)
            ?? throw new NotFoundException(nameof(AccountingGeneration), id);

        if (batch.Exported)
            throw new BusinessRuleException("BATCH_EXPORTED", "Ce batch a été exporté et ne peut plus être supprimé.");

        var entries = await _db.AccountingEntries.Where(e => e.GenerationId == id).ToListAsync(ct);
        _db.AccountingEntries.RemoveRange(entries);
        var logs = await _db.AccountingGenerationLogs.Where(l => l.GenerationId == id).ToListAsync(ct);
        _db.AccountingGenerationLogs.RemoveRange(logs);
        _db.AccountingGenerations.Remove(batch);

        await _audit.LogAsync(AuditAction.DELETE, nameof(AccountingGeneration), id,
            $"Batch {batch.Reference} supprimé ({entries.Count} écriture(s))", ct: ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PagedResponse<AccountingPendingDto>> ListPendingAsync(AccountingPendingFilterDto filter, CancellationToken ct = default)
    {
        var page = Math.Max(1, filter.Page);
        var size = Math.Clamp(filter.PageSize, 1, 200);

        var q = _db.AccountingPendings.AsNoTracking().Include(p => p.CashOperation).AsQueryable();
        if (filter.Resolved.HasValue) q = q.Where(p => p.Resolved == filter.Resolved.Value);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(p => p.CreatedDate)
            .Skip((page - 1) * size).Take(size)
            .Select(p => new AccountingPendingDto(
                p.Id, p.CashOperationId, p.CashOperation.OperationRef, p.CashOperation.OperationDate,
                p.Reason, p.CreatedDate, p.Resolved, p.ResolvedDate))
            .ToListAsync(ct);

        return new PagedResponse<AccountingPendingDto> { Items = items, Page = page, PageSize = size, TotalCount = total };
    }

    internal static AccountingEntryDto MapEntry(AccountingEntry e) =>
        new(e.Id, e.GenerationId, e.CashOperationId, e.CashOperation?.OperationRef,
            e.JournalId, e.Journal.Code, e.AccountId, e.Account.AccountNumber, e.Account.Name,
            e.EntryDate, e.OperationDate, e.Reference, e.PieceNumber, e.Description, e.Comment, e.Debit, e.Credit);

    private static AccountingGenerationDetailDto Map(AccountingGeneration g, int? processingTimeMs) =>
        new(g.Id, g.Reference, g.GenerationType, g.GenerationMode, g.StartDate, g.EndDate, g.Status,
            g.GeneratedBy, g.GeneratedByUser.FullName, g.GeneratedAt, g.Exported, g.ExportedAt,
            g.TotalOperations, g.TotalEntries,
            g.Entries.Sum(e => e.Debit), g.Entries.Sum(e => e.Credit),
            processingTimeMs, g.Remarks,
            g.Entries.Select(MapEntry).ToList());
}
