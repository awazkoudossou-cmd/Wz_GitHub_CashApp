using CashApp.Application.Common.Interfaces;
using CashApp.Application.Dashboard.Dtos;
using CashApp.Domain.Constants;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly IFeatureService _features;

    public DashboardService(IAppDbContext db, IDateTimeProvider clock, IFeatureService features)
    {
        _db = db;
        _clock = clock;
        _features = features;
    }

    public async Task<CashierDashboardDto> GetCashierDashboardAsync(int cashRegisterId, CancellationToken ct = default)
    {
        var todayStart = _clock.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);

        var active = await _db.CashSessions.AsNoTracking()
            .Include(s => s.CashRegister)
            .Where(s => s.CashRegisterId == cashRegisterId && s.Status == CashSessionStatus.OPEN)
            .FirstOrDefaultAsync(ct);

        CashSessionWidgetDto? widget = null;
        if (active is not null)
        {
            var counts = await _db.CashOperations.AsNoTracking()
                .Where(o => o.CashSessionId == active.Id)
                .Select(o => new { o.Direction, o.Amount })
                .ToListAsync(ct);
            var theoretical = active.OpeningBalance
                + counts.Where(c => c.Direction == OperationDirection.IN).Sum(c => c.Amount)
                - counts.Where(c => c.Direction == OperationDirection.OUT).Sum(c => c.Amount);
            widget = new CashSessionWidgetDto(active.Id, active.CashRegisterId, active.CashRegister.Name,
                active.OpenedAt, active.OpeningBalance, theoretical, counts.Count);
        }

        var todayOps = await _db.CashOperations.AsNoTracking()
            .Where(o => o.CashRegisterId == cashRegisterId
                        && o.OperationDate >= todayStart && o.OperationDate < todayEnd)
            .Select(o => new { o.Direction, o.Amount })
            .ToListAsync(ct);

        var totalIn = todayOps.Where(o => o.Direction == OperationDirection.IN).Sum(o => o.Amount);
        var totalOut = todayOps.Where(o => o.Direction == OperationDirection.OUT).Sum(o => o.Amount);

        var recent = await _db.CashOperations.AsNoTracking()
            .Include(o => o.Category)
            .Where(o => o.CashRegisterId == cashRegisterId)
            .OrderByDescending(o => o.OperationDate).ThenByDescending(o => o.Id)
            .Take(10)
            .Select(o => new OperationWidgetDto(o.Id, o.OperationRef, o.OperationDate,
                o.Direction.ToString(), o.Category.Label, o.Amount, o.Label))
            .ToListAsync(ct);

        // Tendance sur 7 jours (pour la caisse sélectionnée) — courbe/barres évolution.
        var trendStart = todayStart.AddDays(-6);
        var trendOps = await _db.CashOperations.AsNoTracking()
            .Where(o => o.CashRegisterId == cashRegisterId && o.OperationDate >= trendStart && o.OperationDate < todayEnd)
            .Select(o => new { o.OperationDate, o.Direction, o.Amount })
            .ToListAsync(ct);
        var trend7 = BuildDailyTrend(trendOps.Select(o => (o.OperationDate, o.Direction, o.Amount)), trendStart, todayStart);

        // Répartition par catégorie du jour — camembert.
        // NB : agrégation (Sum) faite côté client — SQLite/EF Core ne sait pas traduire Sum(decimal) en SQL.
        var categoryRows = await _db.CashOperations.AsNoTracking()
            .Include(o => o.Category)
            .Where(o => o.CashRegisterId == cashRegisterId && o.OperationDate >= todayStart && o.OperationDate < todayEnd)
            .Select(o => new { o.Category.Label, o.Amount })
            .ToListAsync(ct);
        var categoryBreakdown = categoryRows.GroupBy(o => o.Label)
            .Select(g => new CategoryBreakdownDto(g.Key, g.Sum(x => x.Amount), g.Count()))
            .OrderByDescending(c => c.Amount)
            .Take(6)
            .ToList();

        return new CashierDashboardDto(widget, totalIn, totalOut, todayOps.Count, recent, trend7, categoryBreakdown);
    }

    public async Task<SupervisorDashboardDto> GetSupervisorDashboardAsync(CancellationToken ct = default)
    {
        var todayStart = _clock.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);

        var openSessions = await _db.CashSessions.AsNoTracking()
            .Include(s => s.CashRegister)
            .Where(s => s.Status == CashSessionStatus.OPEN)
            .ToListAsync(ct);

        var openWidgets = new List<CashSessionWidgetDto>();
        foreach (var s in openSessions)
        {
            var ops = await _db.CashOperations.AsNoTracking()
                .Where(o => o.CashSessionId == s.Id)
                .Select(o => new { o.Direction, o.Amount })
                .ToListAsync(ct);
            var theoretical = s.OpeningBalance
                + ops.Where(o => o.Direction == OperationDirection.IN).Sum(o => o.Amount)
                - ops.Where(o => o.Direction == OperationDirection.OUT).Sum(o => o.Amount);
            openWidgets.Add(new CashSessionWidgetDto(s.Id, s.CashRegisterId, s.CashRegister.Name,
                s.OpenedAt, s.OpeningBalance, theoretical, ops.Count));
        }

        var todayClosed = await _db.CashSessions.AsNoTracking()
            .CountAsync(s => s.Status == CashSessionStatus.CLOSED
                             && s.ClosedAt >= todayStart && s.ClosedAt < todayEnd, ct);

        var todayOps = await _db.CashOperations.AsNoTracking()
            .Where(o => o.OperationDate >= todayStart && o.OperationDate < todayEnd)
            .Select(o => new { o.Direction, o.Amount })
            .ToListAsync(ct);
        var totalIn = todayOps.Where(o => o.Direction == OperationDirection.IN).Sum(o => o.Amount);
        var totalOut = todayOps.Where(o => o.Direction == OperationDirection.OUT).Sum(o => o.Amount);

        var sessionsWithVariance = await _db.CashSessions.AsNoTracking()
            .CountAsync(s => s.Status == CashSessionStatus.CLOSED
                             && s.VarianceAmount != null && s.VarianceAmount != 0, ct);

        var kpis = new List<SupervisorKpiDto>
        {
            new("Sessions ouvertes", openSessions.Count, null),
            new("Sessions clôturées aujourd'hui", todayClosed, null),
            new("Entrées du jour", totalIn, null),
            new("Sorties du jour", totalOut, null),
            new("Sessions en écart", sessionsWithVariance, null)
        };

        // Tendance sur 14 jours (toutes caisses confondues) — courbe évolution entrées/sorties/net.
        var trendStart = todayStart.AddDays(-13);
        var trendOps = await _db.CashOperations.AsNoTracking()
            .Where(o => o.OperationDate >= trendStart && o.OperationDate < todayEnd)
            .Select(o => new { o.OperationDate, o.Direction, o.Amount })
            .ToListAsync(ct);
        var trend14 = BuildDailyTrend(trendOps.Select(o => (o.OperationDate, o.Direction, o.Amount)), trendStart, todayStart);

        // Top catégories sur 30 jours — camembert/barres.
        // NB : agrégation (Sum) faite côté client — SQLite/EF Core ne sait pas traduire Sum(decimal) en SQL.
        var catStart = todayStart.AddDays(-29);
        var catRows = await _db.CashOperations.AsNoTracking()
            .Include(o => o.Category)
            .Where(o => o.OperationDate >= catStart && o.OperationDate < todayEnd)
            .Select(o => new { o.Category.Label, o.Amount })
            .ToListAsync(ct);
        var topCategories = catRows.GroupBy(o => o.Label)
            .Select(g => new CategoryBreakdownDto(g.Key, g.Sum(x => x.Amount), g.Count()))
            .OrderByDescending(c => c.Amount)
            .Take(8)
            .ToList();

        // Répartition du jour par caisse — comparaison entre caisses.
        var registerRows = await _db.CashOperations.AsNoTracking()
            .Include(o => o.CashRegister)
            .Where(o => o.OperationDate >= todayStart && o.OperationDate < todayEnd)
            .Select(o => new { o.CashRegisterId, o.CashRegister.Code, o.CashRegister.Name, o.Direction, o.Amount })
            .ToListAsync(ct);
        var registerBreakdown = registerRows
            .GroupBy(o => new { o.CashRegisterId, o.Code, o.Name })
            .Select(g => new RegisterBreakdownDto(
                g.Key.CashRegisterId, g.Key.Code, g.Key.Name,
                g.Where(x => x.Direction == OperationDirection.IN).Sum(x => x.Amount)
                    - g.Where(x => x.Direction == OperationDirection.OUT).Sum(x => x.Amount),
                g.Count()))
            .OrderByDescending(r => Math.Abs(r.Net))
            .ToList();

        // Widgets "à traiter" — gated par feature flag, pour ne pas afficher un module désactivé.
        var approvalsEnabled = await _features.IsEnabledAsync(FeatureCodes.AdvValidation, ct);
        var pendingApprovals = approvalsEnabled
            ? await _db.ApprovalRequests.AsNoTracking().CountAsync(r => r.Status == ApprovalStatus.PENDING, ct)
            : 0;

        var varianceEnabled = await _features.IsEnabledAsync(FeatureCodes.AdvVarianceManagement, ct);
        var openVarianceCases = varianceEnabled
            ? await _db.VarianceCases.AsNoTracking().CountAsync(v => v.Status == VarianceStatus.OPEN, ct)
            : 0;

        var anomaliesEnabled = await _features.IsEnabledAsync(FeatureCodes.AdvAnomalies, ct);
        var openAnomalies = anomaliesEnabled
            ? await _db.AnomalyCases.AsNoTracking().CountAsync(a => a.Status == AnomalyStatus.OPEN, ct)
            : 0;

        return new SupervisorDashboardDto(
            openSessions.Count, todayClosed, totalIn, totalOut, totalIn - totalOut,
            sessionsWithVariance, openWidgets, kpis,
            trend14, topCategories, registerBreakdown,
            pendingApprovals, approvalsEnabled,
            openVarianceCases, varianceEnabled,
            openAnomalies, anomaliesEnabled);
    }

    // Construit une série continue de points quotidiens (jours sans opération = zéros),
    // pour que les graphiques d'évolution affichent des dates régulières sans trous.
    private static List<DailyTrendPointDto> BuildDailyTrend(
        IEnumerable<(DateTime OperationDate, OperationDirection Direction, decimal Amount)> ops,
        DateTime start, DateTime end)
    {
        var byDay = ops.GroupBy(o => o.OperationDate.Date)
            .ToDictionary(g => g.Key, g => (
                In: g.Where(x => x.Direction == OperationDirection.IN).Sum(x => x.Amount),
                Out: g.Where(x => x.Direction == OperationDirection.OUT).Sum(x => x.Amount)));

        var result = new List<DailyTrendPointDto>();
        for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
        {
            var (i, o) = byDay.TryGetValue(d, out var v) ? v : (0m, 0m);
            result.Add(new DailyTrendPointDto(d, i, o, i - o));
        }
        return result;
    }
}
