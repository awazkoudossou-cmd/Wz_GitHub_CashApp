using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Common.Models;
using CashApp.Application.Reconciliation.Dtos;
using CashApp.Domain.Entities.V2;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.Reconciliation;

public class ReconciliationService : IReconciliationService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditLogger _audit;
    private readonly ICashRegisterAccessService _access;

    public ReconciliationService(IAppDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock,
        IAuditLogger audit, ICashRegisterAccessService access)
    {
        _db = db; _currentUser = currentUser; _clock = clock; _audit = audit; _access = access;
    }

    public async Task<PagedResponse<ReconciliationBatchListItemDto>> ListAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var accessible = await _access.GetAccessibleRegisterIdsAsync(ct);

        var q = _db.ReconciliationBatches.AsNoTracking()
            .Include(b => b.CashRegister)
            .Include(b => b.CreatedByUser)
            .Where(b => !b.CashRegisterId.HasValue || accessible.Contains(b.CashRegisterId.Value));

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(b => new ReconciliationBatchListItemDto(
                b.Id, b.Reference, b.BatchType,
                b.CashRegisterId, b.CashRegister != null ? b.CashRegister.Code : null,
                b.CreatedBy, b.CreatedByUser.FullName, b.Status, b.CreatedAt))
            .ToListAsync(ct);
        return new PagedResponse<ReconciliationBatchListItemDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<ReconciliationBatchDetailDto> GetAsync(int id, CancellationToken ct = default)
    {
        var b = await _db.ReconciliationBatches.AsNoTracking()
            .Include(x => x.CashRegister)
            .Include(x => x.CreatedByUser)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(ReconciliationBatch), id);
        return MapDetail(b);
    }

    public async Task<ReconciliationBatchDetailDto> CreateAsync(CreateReconciliationBatchDto dto, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException("Non authentifié.");
        if (dto.CashRegisterId.HasValue) await _access.EnsureCanAccessAsync(dto.CashRegisterId.Value, ct);

        var entity = new ReconciliationBatch
        {
            Reference = $"RB-{_clock.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}",
            BatchType = dto.BatchType,
            CashRegisterId = dto.CashRegisterId,
            CreatedBy = userId,
            Status = ReconciliationStatus.OPEN,
            Notes = dto.Notes?.Trim()
        };
        _db.ReconciliationBatches.Add(entity);
        await _audit.LogAsync(AuditAction.CREATE, nameof(ReconciliationBatch), 0, $"Rapprochement {entity.Reference}", ct: ct);
        await _db.SaveChangesAsync(ct);
        return await GetAsync(entity.Id, ct);
    }

    public async Task<ReconciliationBatchDetailDto> MatchAsync(int id, ReconcileItemsDto dto, CancellationToken ct = default)
    {
        var batch = await _db.ReconciliationBatches.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(ReconciliationBatch), id);
        if (batch.Status == ReconciliationStatus.CLOSED)
            throw new BusinessRuleException("RECONCILIATION_CLOSED", "Rapprochement clôturé.");
        if (batch.CashRegisterId.HasValue) await _access.EnsureCanAccessAsync(batch.CashRegisterId.Value, ct);

        foreach (var pair in dto.Pairs)
        {
            // Anti-doublon : un même couple (Left, Right) ne doit pas exister 2x dans le batch.
            var exists = await _db.ReconciliationItems
                .AnyAsync(i => i.ReconciliationBatchId == batch.Id
                            && i.LeftEntityType == pair.LeftEntityType
                            && i.LeftEntityId == pair.LeftEntityId, ct);
            if (exists)
            {
                throw new BusinessRuleException("RECONCILIATION_DUPLICATE_LEFT",
                    $"L'élément {pair.LeftEntityType}#{pair.LeftEntityId} est déjà rapproché dans ce batch.");
            }

            _db.ReconciliationItems.Add(new ReconciliationItem
            {
                ReconciliationBatchId = batch.Id,
                LeftEntityType = pair.LeftEntityType,
                LeftEntityId = pair.LeftEntityId,
                RightEntityType = pair.RightEntityType,
                RightEntityId = pair.RightEntityId,
                MatchedAmount = pair.MatchedAmount,
                MatchStatus = pair.RightEntityId.HasValue ? ReconciliationMatchStatus.MATCHED : ReconciliationMatchStatus.UNMATCHED,
                Notes = pair.Notes
            });
        }

        if (batch.Status == ReconciliationStatus.OPEN) batch.Status = ReconciliationStatus.IN_PROGRESS;
        if (dto.CloseAfter) batch.Status = ReconciliationStatus.CLOSED;

        await _audit.LogAsync(AuditAction.UPDATE, nameof(ReconciliationBatch), batch.Id,
            $"Match : {dto.Pairs.Count} paires{(dto.CloseAfter ? " + fermeture" : "")}", ct: ct);
        await _db.SaveChangesAsync(ct);
        return await GetAsync(batch.Id, ct);
    }

    private static ReconciliationBatchDetailDto MapDetail(ReconciliationBatch b) =>
        new(b.Id, b.Reference, b.BatchType,
            b.CashRegisterId, b.CashRegister?.Code,
            b.CreatedBy, b.CreatedByUser.FullName, b.Status, b.Notes, b.CreatedAt,
            b.Items.Select(i => new ReconciliationItemDto(
                i.Id, i.LeftEntityType, i.LeftEntityId, i.RightEntityType, i.RightEntityId,
                i.MatchedAmount, i.MatchStatus, i.Notes)).ToList());
}
