using CashApp.Application.Accounting.Dtos;
using CashApp.Application.Common.Interfaces;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.Accounting;

public class AccountingDashboardService : IAccountingDashboardService
{
    private const int TrendDays = 14;
    private const int TopAccountCount = 8;

    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public AccountingDashboardService(IAppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<AccountingDashboardDto> GetAsync(CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var todayStart = today.ToDateTime(TimeOnly.MinValue);
        var todayEnd = today.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var since = todayStart.AddDays(-(TrendDays - 1));

        var accountCount = await _db.AccountingAccounts.AsNoTracking().CountAsync(ct);
        var journalCount = await _db.AccountingJournals.AsNoTracking().CountAsync(ct);
        var configuredCategoryCount = await _db.Categories.AsNoTracking().CountAsync(c => c.AccountingAccountId != null, ct);
        var configuredCashRegisterCount = await _db.CashRegisters.AsNoTracking()
            .CountAsync(r => r.AccountingJournalId != null && r.AccountingAccountId != null, ct);
        var batchCount = await _db.AccountingGenerations.AsNoTracking().CountAsync(ct);
        var entryCount = await _db.AccountingEntries.AsNoTracking().CountAsync(ct);
        var pendingCount = await _db.AccountingPendings.AsNoTracking().CountAsync(p => !p.Resolved, ct);

        var lastGeneration = await _db.AccountingGenerations.AsNoTracking()
            .OrderByDescending(g => g.GeneratedAt)
            .Select(g => new { g.Reference, g.GeneratedAt })
            .FirstOrDefaultAsync(ct);
        var lastExport = await _db.AccountingExportLogs.AsNoTracking()
            .OrderByDescending(l => l.ExportedAt)
            .Select(l => new { l.FileName, l.ExportedAt })
            .FirstOrDefaultAsync(ct);

        var batchesToday = await _db.AccountingGenerations.AsNoTracking()
            .CountAsync(g => g.GeneratedAt >= todayStart && g.GeneratedAt < todayEnd, ct);
        var entriesToday = await _db.AccountingEntries.AsNoTracking()
            .CountAsync(e => e.EntryDate >= todayStart && e.EntryDate < todayEnd, ct);
        var exportsToday = await _db.AccountingExportLogs.AsNoTracking()
            .CountAsync(l => l.ExportedAt >= todayStart && l.ExportedAt < todayEnd, ct);
        var errorsCount = await _db.AccountingGenerationQueues.AsNoTracking()
            .CountAsync(q => q.Status == QueueStatus.FAILED, ct);

        var recentEntryDates = await _db.AccountingEntries.AsNoTracking()
            .Where(e => e.EntryDate >= since)
            .Select(e => e.EntryDate)
            .ToListAsync(ct);
        var entriesByDay = BuildDailySeries(recentEntryDates, since, today);

        var recentGenerationDates = await _db.AccountingGenerations.AsNoTracking()
            .Where(g => g.GeneratedAt >= since)
            .Select(g => g.GeneratedAt)
            .ToListAsync(ct);
        var generationsByDay = BuildDailySeries(recentGenerationDates, since, today);

        // GroupBy + projection DTO non traduisible par le provider In-Memory (utilisé en tests) —
        // on matérialise les colonnes brutes puis on agrège côté client (volumes faibles, sans risque).
        var journalCodes = await _db.AccountingEntries.AsNoTracking().Select(e => e.Journal.Code).ToListAsync(ct);
        var journalDistribution = journalCodes
            .GroupBy(c => c)
            .Select(g => new AccountingNamedCountDto(g.Key, g.Count()))
            .OrderByDescending(g => g.Count)
            .ToList();

        var accountLabels = await _db.AccountingEntries.AsNoTracking()
            .Select(e => e.Account.AccountNumber + " " + e.Account.Name)
            .ToListAsync(ct);
        var accountDistributionRaw = accountLabels
            .GroupBy(a => a)
            .Select(g => new AccountingNamedCountDto(g.Key, g.Count()))
            .OrderByDescending(g => g.Count)
            .ToList();
        var accountDistribution = CollapseTopN(accountDistributionRaw, TopAccountCount);

        return new AccountingDashboardDto(
            accountCount, journalCount, configuredCategoryCount, configuredCashRegisterCount,
            batchCount, entryCount, pendingCount,
            lastGeneration?.Reference, lastGeneration?.GeneratedAt,
            lastExport?.FileName, lastExport?.ExportedAt,
            batchesToday, entriesToday, exportsToday, errorsCount,
            entriesByDay, generationsByDay, journalDistribution, accountDistribution);
    }

    private static List<AccountingDailyCountDto> BuildDailySeries(List<DateTime> timestamps, DateTime since, DateOnly today)
    {
        var byDay = timestamps.GroupBy(DateOnly.FromDateTime).ToDictionary(g => g.Key, g => g.Count());
        var result = new List<AccountingDailyCountDto>();
        for (var d = DateOnly.FromDateTime(since); d <= today; d = d.AddDays(1))
            result.Add(new AccountingDailyCountDto(d, byDay.GetValueOrDefault(d)));
        return result;
    }

    private static List<AccountingNamedCountDto> CollapseTopN(IReadOnlyList<AccountingNamedCountDto> items, int topN)
    {
        if (items.Count <= topN) return items.ToList();
        var top = items.Take(topN).ToList();
        var othersCount = items.Skip(topN).Sum(x => x.Count);
        if (othersCount > 0) top.Add(new AccountingNamedCountDto("Autres", othersCount));
        return top;
    }
}
