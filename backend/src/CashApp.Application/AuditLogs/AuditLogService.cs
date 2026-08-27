using System.Text.Json;
using CashApp.Application.AuditLogs.Dtos;
using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.AuditLogs;

public class AuditLogService : IAuditLogService
{
    private readonly IAppDbContext _db;
    public AuditLogService(IAppDbContext db) => _db = db;

    public async Task<PagedResponse<AuditLogListItemDto>> ListAsync(AuditLogFilterDto filter, CancellationToken ct = default)
    {
        var page = Math.Max(1, filter.Page);
        var size = Math.Clamp(filter.PageSize, 1, 500);

        var q = _db.AuditLogs.AsNoTracking()
            .Include(a => a.PerformedByUser)
            .AsQueryable();

        if (filter.ActionType.HasValue) q = q.Where(a => a.ActionType == filter.ActionType.Value);
        if (!string.IsNullOrWhiteSpace(filter.EntityType)) q = q.Where(a => a.EntityType == filter.EntityType);
        if (filter.EntityId.HasValue) q = q.Where(a => a.EntityId == filter.EntityId.Value);
        if (filter.PerformedBy.HasValue) q = q.Where(a => a.PerformedBy == filter.PerformedBy.Value);
        if (filter.From.HasValue) q = q.Where(a => a.PerformedAt >= filter.From.Value);
        if (filter.To.HasValue) q = q.Where(a => a.PerformedAt <= filter.To.Value);

        var total = await q.CountAsync(ct);

        // On charge les colonnes brutes nécessaires puis on parse les JSON côté client.
        var raw = await q.OrderByDescending(a => a.PerformedAt)
            .Skip((page - 1) * size).Take(size)
            .Select(a => new
            {
                a.Id, a.ActionType, a.EntityType, a.EntityId, a.PerformedBy,
                PerformedByName = a.PerformedByUser != null ? a.PerformedByUser.FullName : null,
                a.PerformedAt, a.Description, a.MetadataJson, a.NewValuesJson, a.OldValuesJson
            })
            .ToListAsync(ct);

        var items = raw.Select(a =>
        {
            var money = ExtractMoney(a.MetadataJson) ?? ExtractMoney(a.NewValuesJson) ?? ExtractMoney(a.OldValuesJson);
            return new AuditLogListItemDto(
                a.Id, a.ActionType, a.EntityType, a.EntityId,
                a.PerformedBy, a.PerformedByName,
                a.PerformedAt, a.Description,
                money?.Amount, money?.Currency);
        }).ToList();

        return new PagedResponse<AuditLogListItemDto> { Items = items, Page = page, PageSize = size, TotalCount = total };
    }

    public async Task<AuditLogDetailDto> GetAsync(int id, CancellationToken ct = default)
    {
        var a = await _db.AuditLogs.AsNoTracking()
            .Include(x => x.PerformedByUser)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.V2.AuditLog), id);
        return new AuditLogDetailDto(a.Id, a.ActionType, a.EntityType, a.EntityId,
            a.PerformedBy, a.PerformedByUser?.FullName, a.PerformedAt,
            a.Description, a.OldValuesJson, a.NewValuesJson, a.MetadataJson, a.IpAddress);
    }

    // Cherche un champ "amount" (et éventuellement currencyCode) dans un payload JSON arbitraire.
    private static (decimal Amount, string? Currency)? ExtractMoney(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;

            decimal? amount = TryDecimal(doc.RootElement, "amount")
                           ?? TryDecimal(doc.RootElement, "Amount")
                           ?? TryDecimal(doc.RootElement, "varianceAmount")
                           ?? TryDecimal(doc.RootElement, "VarianceAmount")
                           ?? TryDecimal(doc.RootElement, "openingBalance")
                           ?? TryDecimal(doc.RootElement, "OpeningBalance")
                           ?? TryDecimal(doc.RootElement, "physicalBalance")
                           ?? TryDecimal(doc.RootElement, "PhysicalBalance");
            if (amount is null) return null;

            string? currency = TryString(doc.RootElement, "currencyCode")
                           ?? TryString(doc.RootElement, "CurrencyCode")
                           ?? TryString(doc.RootElement, "currency");
            return (amount.Value, currency);
        }
        catch { return null; }
    }

    private static decimal? TryDecimal(JsonElement obj, string prop)
    {
        if (!obj.TryGetProperty(prop, out var el)) return null;
        return el.ValueKind switch
        {
            JsonValueKind.Number => el.TryGetDecimal(out var d) ? d : null,
            JsonValueKind.String when decimal.TryParse(el.GetString(), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var d2) => d2,
            _ => null
        };
    }

    private static string? TryString(JsonElement obj, string prop)
    {
        if (!obj.TryGetProperty(prop, out var el)) return null;
        return el.ValueKind == JsonValueKind.String ? el.GetString() : null;
    }
}
