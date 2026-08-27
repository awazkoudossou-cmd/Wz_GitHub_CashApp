using CashApp.Application.Common.Interfaces;
using CashApp.Application.Reports.Dtos;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.Reports;

public class AdvancedReportService : IAdvancedReportService
{
    private readonly IAppDbContext _db;
    private readonly ICashRegisterAccessService _access;

    public AdvancedReportService(IAppDbContext db, ICashRegisterAccessService access)
    {
        _db = db; _access = access;
    }

    public async Task<CashReportResultDto> CashAsync(CashReportFilterDto filter, CancellationToken ct = default)
    {
        var accessible = await _access.GetAccessibleRegisterIdsAsync(ct);
        var q = _db.CashOperations.AsNoTracking()
            .Include(o => o.CashRegister)
            .Where(o => o.OperationDate >= filter.From && o.OperationDate <= filter.To
                        && !o.IsDeleted
                        && accessible.Contains(o.CashRegisterId));
        if (filter.CashRegisterId.HasValue) q = q.Where(o => o.CashRegisterId == filter.CashRegisterId.Value);

        var raw = await q.Select(o => new { o.CashRegisterId, o.CashRegister.Code, o.Direction, o.Amount }).ToListAsync(ct);

        var rows = raw
            .GroupBy(o => new { o.CashRegisterId, o.Code })
            .Select(g =>
            {
                var totalIn = g.Where(x => x.Direction == OperationDirection.IN).Sum(x => x.Amount);
                var totalOut = g.Where(x => x.Direction == OperationDirection.OUT).Sum(x => x.Amount);
                return new CashReportRowDto(g.Key.CashRegisterId, g.Key.Code, totalIn, totalOut, totalIn - totalOut, g.Count());
            })
            .OrderBy(r => r.CashRegisterCode)
            .ToList();

        var summary = new CashReportSummaryDto(
            rows.Sum(r => r.TotalIn),
            rows.Sum(r => r.TotalOut),
            rows.Sum(r => r.NetMovement),
            rows.Sum(r => r.OperationCount));
        return new CashReportResultDto(summary, rows);
    }

    public async Task<CategoryReportResultDto> CategoriesAsync(CategoryReportFilterDto filter, CancellationToken ct = default)
    {
        var accessible = await _access.GetAccessibleRegisterIdsAsync(ct);
        var q = _db.CashOperations.AsNoTracking()
            .Include(o => o.Category)
            .Where(o => o.OperationDate >= filter.From && o.OperationDate <= filter.To
                        && !o.IsDeleted
                        && accessible.Contains(o.CashRegisterId));
        if (filter.CashRegisterId.HasValue) q = q.Where(o => o.CashRegisterId == filter.CashRegisterId.Value);
        if (filter.Direction.HasValue) q = q.Where(o => o.Direction == filter.Direction.Value);

        var raw = await q.Select(o => new { o.CategoryId, o.Category.Code, o.Category.Label, o.Direction, o.Amount }).ToListAsync(ct);

        var rows = raw
            .GroupBy(o => new { o.CategoryId, o.Code, o.Label, o.Direction })
            .Select(g => new CategoryReportRowDto(g.Key.CategoryId, g.Key.Code, g.Key.Label, g.Key.Direction, g.Sum(x => x.Amount), g.Count()))
            .OrderByDescending(r => r.Total)
            .ToList();
        return new CategoryReportResultDto(rows);
    }

    public async Task<VarianceReportResultDto> VariancesAsync(VarianceReportFilterDto filter, CancellationToken ct = default)
    {
        var accessible = await _access.GetAccessibleRegisterIdsAsync(ct);
        var q = _db.VarianceCases.AsNoTracking()
            .Include(v => v.CashSession).ThenInclude(s => s.CashRegister)
            .Where(v => v.DetectedAt >= filter.From && v.DetectedAt <= filter.To
                        && accessible.Contains(v.CashSession.CashRegisterId));
        if (filter.CashRegisterId.HasValue) q = q.Where(v => v.CashSession.CashRegisterId == filter.CashRegisterId.Value);
        if (filter.Status.HasValue) q = q.Where(v => v.Status == filter.Status.Value);

        var rows = await q.OrderByDescending(v => v.DetectedAt)
            .Select(v => new VarianceReportRowDto(v.Id, v.CashSessionId,
                v.CashSession.CashRegisterId, v.CashSession.CashRegister.Code,
                v.VarianceAmount, v.Status, v.DetectedAt))
            .ToListAsync(ct);
        return new VarianceReportResultDto(rows);
    }

    public async Task<TransferReportResultDto> TransfersAsync(TransferReportFilterDto filter, CancellationToken ct = default)
    {
        var accessible = await _access.GetAccessibleRegisterIdsAsync(ct);
        var q = _db.CashTransfers.AsNoTracking()
            .Include(t => t.SourceCashRegister)
            .Include(t => t.DestinationCashRegister)
            .Where(t => t.TransferDate >= filter.From && t.TransferDate <= filter.To
                        && (accessible.Contains(t.SourceCashRegisterId) || accessible.Contains(t.DestinationCashRegisterId)));
        if (filter.CashRegisterId.HasValue)
            q = q.Where(t => t.SourceCashRegisterId == filter.CashRegisterId.Value || t.DestinationCashRegisterId == filter.CashRegisterId.Value);
        if (filter.Status.HasValue) q = q.Where(t => t.Status == filter.Status.Value);

        var rows = await q.OrderByDescending(t => t.TransferDate)
            .Select(t => new TransferReportRowDto(t.Id, t.TransferRef, t.SourceCashRegister.Code, t.DestinationCashRegister.Code,
                t.Amount, t.CurrencyCode, t.Status, t.TransferDate))
            .ToListAsync(ct);
        return new TransferReportResultDto(rows);
    }

    public async Task<DepositReportResultDto> DepositsAsync(DepositReportFilterDto filter, CancellationToken ct = default)
    {
        var accessible = await _access.GetAccessibleRegisterIdsAsync(ct);
        var q = _db.BankDeposits.AsNoTracking()
            .Include(d => d.CashRegister)
            .Where(d => d.DepositDate >= filter.From && d.DepositDate <= filter.To
                        && accessible.Contains(d.CashRegisterId));
        if (filter.CashRegisterId.HasValue) q = q.Where(d => d.CashRegisterId == filter.CashRegisterId.Value);
        if (filter.Status.HasValue) q = q.Where(d => d.Status == filter.Status.Value);

        var rows = await q.OrderByDescending(d => d.DepositDate)
            .Select(d => new DepositReportRowDto(d.Id, d.DepositRef, d.CashRegister.Code, d.BankName,
                d.Amount, d.CurrencyCode, d.Status, d.DepositDate))
            .ToListAsync(ct);
        return new DepositReportResultDto(rows);
    }

    public async Task<AnomalyReportResultDto> AnomaliesAsync(AnomalyReportFilterDto filter, CancellationToken ct = default)
    {
        var q = _db.AnomalyCases.AsNoTracking()
            .Include(a => a.CashRegister)
            .Where(a => a.DetectedAt >= filter.From && a.DetectedAt <= filter.To);
        if (filter.CashRegisterId.HasValue) q = q.Where(a => a.CashRegisterId == filter.CashRegisterId.Value);
        if (filter.Status.HasValue) q = q.Where(a => a.Status == filter.Status.Value);
        if (filter.Severity.HasValue) q = q.Where(a => a.Severity == filter.Severity.Value);

        var rows = await q.OrderByDescending(a => a.DetectedAt)
            .Select(a => new AnomalyReportRowDto(a.Id, a.Reference, a.Severity, a.Status,
                a.CashRegister != null ? a.CashRegister.Code : null, a.DetectedAt))
            .ToListAsync(ct);
        return new AnomalyReportResultDto(rows);
    }

    public async Task<ApprovalReportResultDto> ApprovalsAsync(ApprovalReportFilterDto filter, CancellationToken ct = default)
    {
        var q = _db.ApprovalRequests.AsNoTracking()
            .Where(r => r.RequestedAt >= filter.From && r.RequestedAt <= filter.To);
        if (filter.Status.HasValue) q = q.Where(r => r.Status == filter.Status.Value);
        if (filter.TargetType.HasValue) q = q.Where(r => r.TargetType == filter.TargetType.Value);

        var rows = await q.OrderByDescending(r => r.RequestedAt)
            .Select(r => new ApprovalReportRowDto(r.Id, r.RequestRef, r.TargetType, r.TargetEntityType,
                r.Status, r.Amount, r.RequestedAt))
            .ToListAsync(ct);
        return new ApprovalReportResultDto(rows);
    }
}
