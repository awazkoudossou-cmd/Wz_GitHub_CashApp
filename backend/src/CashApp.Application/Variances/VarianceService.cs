using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Common.Models;
using CashApp.Application.Variances.Dtos;
using CashApp.Domain.Entities.V2;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.Variances;

public class VarianceService : IVarianceService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditLogger _audit;

    public VarianceService(IAppDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock, IAuditLogger audit)
    {
        _db = db; _currentUser = currentUser; _clock = clock; _audit = audit;
    }

    public async Task<PagedResponse<VarianceCaseListItemDto>> ListAsync(VarianceFilterDto filter, CancellationToken ct = default)
    {
        var page = Math.Max(1, filter.Page);
        var size = Math.Clamp(filter.PageSize, 1, 200);

        var q = _db.VarianceCases.AsNoTracking()
            .Include(v => v.CashSession).ThenInclude(s => s.CashRegister)
            .Include(v => v.CashSession).ThenInclude(s => s.OpenedByUser)
            .Include(v => v.CashSession).ThenInclude(s => s.ClosedByUser)
            .AsQueryable();
        if (filter.Status.HasValue) q = q.Where(v => v.Status == filter.Status.Value);
        if (filter.CashRegisterId.HasValue) q = q.Where(v => v.CashSession.CashRegisterId == filter.CashRegisterId.Value);
        if (filter.From.HasValue) q = q.Where(v => v.DetectedAt >= filter.From.Value);
        if (filter.To.HasValue) q = q.Where(v => v.DetectedAt <= filter.To.Value);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(v => v.DetectedAt)
            .Skip((page - 1) * size).Take(size)
            .Select(v => new VarianceCaseListItemDto(
                v.Id, v.CashSessionId, v.CashSession.CashRegisterId,
                v.CashSession.CashRegister.Code, v.CashSession.CashRegister.Name,
                v.VarianceAmount, v.CurrencyCode, v.Status,
                v.DetectedAt, v.ApprovalRequestId, v.AnomalyCaseId,
                v.CashSession.OpenedAt, v.CashSession.ClosedAt,
                v.CashSession.OpenedByUser.FullName,
                v.CashSession.ClosedByUser != null ? v.CashSession.ClosedByUser.FullName : null,
                v.CashSession.OpeningBalance,
                v.CashSession.TheoreticalBalance, v.CashSession.PhysicalBalance))
            .ToListAsync(ct);

        return new PagedResponse<VarianceCaseListItemDto> { Items = items, Page = page, PageSize = size, TotalCount = total };
    }

    public async Task<VarianceCaseDetailDto> GetAsync(int id, CancellationToken ct = default)
    {
        var v = await Load(id, true, ct) ?? throw new NotFoundException(nameof(VarianceCase), id);
        return Map(v);
    }

    public async Task<VarianceCaseDetailDto?> FindBySessionAsync(int cashSessionId, CancellationToken ct = default)
    {
        var v = await _db.VarianceCases.AsNoTracking()
            .Include(x => x.CashSession).ThenInclude(s => s.CashRegister)
            .Include(x => x.Justifications).ThenInclude(j => j.ProvidedByUser)
            .Include(x => x.Actions).ThenInclude(a => a.PerformedByUser)
            .FirstOrDefaultAsync(x => x.CashSessionId == cashSessionId, ct);
        return v is null ? null : Map(v);
    }

    public async Task<VarianceCaseDetailDto> AddJustificationAsync(int id, CreateVarianceJustificationDto dto, CancellationToken ct = default)
    {
        var v = await _db.VarianceCases.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(VarianceCase), id);
        var userId = _currentUser.UserId ?? throw new ForbiddenException("Non authentifié.");

        _db.VarianceJustifications.Add(new VarianceJustification
        {
            VarianceCaseId = v.Id,
            ProvidedBy = userId,
            ProvidedAt = _clock.UtcNow,
            Comment = dto.Comment.Trim()
        });
        _db.VarianceActions.Add(new VarianceAction
        {
            VarianceCaseId = v.Id,
            ActionType = "JUSTIFY",
            PerformedBy = userId,
            PerformedAt = _clock.UtcNow,
            Comment = dto.Comment.Trim()
        });
        if (v.Status == VarianceStatus.OPEN) v.Status = VarianceStatus.JUSTIFIED;
        await _audit.LogAsync(AuditAction.UPDATE, nameof(VarianceCase), v.Id, "Justification ajoutée", ct: ct);
        await _db.SaveChangesAsync(ct);
        return await GetAsync(v.Id, ct);
    }

    public async Task<VarianceCase> CreateForSessionAsync(int cashSessionId, decimal varianceAmount, string currencyCode, CancellationToken ct = default)
    {
        var v = new VarianceCase
        {
            CashSessionId = cashSessionId,
            VarianceAmount = varianceAmount,
            CurrencyCode = currencyCode,
            Status = VarianceStatus.OPEN,
            DetectedAt = _clock.UtcNow
        };
        _db.VarianceCases.Add(v);
        await _audit.LogAsync(AuditAction.CREATE, nameof(VarianceCase), 0,
            $"Variance case for session #{cashSessionId} amount={varianceAmount}", ct: ct);
        return v;
    }

    private async Task<VarianceCase?> Load(int id, bool noTrack, CancellationToken ct)
    {
        var q = _db.VarianceCases
            .Include(v => v.CashSession).ThenInclude(s => s.CashRegister)
            .Include(v => v.CashSession).ThenInclude(s => s.OpenedByUser)
            .Include(v => v.CashSession).ThenInclude(s => s.ClosedByUser)
            .Include(v => v.Justifications).ThenInclude(j => j.ProvidedByUser)
            .Include(v => v.Actions).ThenInclude(a => a.PerformedByUser)
            .AsQueryable();
        if (noTrack) q = q.AsNoTracking();
        return await q.FirstOrDefaultAsync(v => v.Id == id, ct);
    }

    private static VarianceCaseDetailDto Map(VarianceCase v) =>
        new(v.Id, v.CashSessionId, v.CashSession.CashRegisterId, v.CashSession.CashRegister.Code,
            v.CashSession.CashRegister.Name,
            v.VarianceAmount, v.CurrencyCode, v.Status, v.DetectedAt,
            v.ApprovalRequestId, v.AnomalyCaseId, v.ClosedAt,
            v.CashSession.OpenedAt, v.CashSession.ClosedAt,
            v.CashSession.OpenedByUser.FullName,
            v.CashSession.ClosedByUser?.FullName,
            v.CashSession.OpeningBalance, v.CashSession.TheoreticalBalance, v.CashSession.PhysicalBalance,
            v.CashSession.CloseComment,
            v.Justifications.OrderBy(j => j.ProvidedAt)
                .Select(j => new VarianceJustificationDto(j.Id, j.ProvidedBy, j.ProvidedByUser.FullName, j.ProvidedAt, j.Comment))
                .ToList(),
            v.Actions.OrderBy(a => a.PerformedAt)
                .Select(a => new VarianceActionDto(a.Id, a.ActionType, a.PerformedBy, a.PerformedByUser.FullName, a.PerformedAt, a.Comment))
                .ToList());
}
