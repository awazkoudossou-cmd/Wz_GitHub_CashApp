using CashApp.Application.Anomalies.Dtos;
using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Common.Models;
using CashApp.Domain.Entities.V2;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.Anomalies;

public class AnomalyService : IAnomalyService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditLogger _audit;

    public AnomalyService(IAppDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock, IAuditLogger audit)
    {
        _db = db; _currentUser = currentUser; _clock = clock; _audit = audit;
    }

    public async Task<PagedResponse<AnomalyListItemDto>> ListAsync(AnomalyFilterDto filter, CancellationToken ct = default)
    {
        var page = Math.Max(1, filter.Page);
        var size = Math.Clamp(filter.PageSize, 1, 200);

        var q = _db.AnomalyCases.AsNoTracking()
            .Include(a => a.CashRegister)
            .Include(a => a.AssignedToUser)
            .AsQueryable();

        if (filter.Status.HasValue) q = q.Where(a => a.Status == filter.Status.Value);
        if (filter.Severity.HasValue) q = q.Where(a => a.Severity == filter.Severity.Value);
        if (filter.CashRegisterId.HasValue) q = q.Where(a => a.CashRegisterId == filter.CashRegisterId.Value);
        if (filter.From.HasValue) q = q.Where(a => a.DetectedAt >= filter.From.Value);
        if (filter.To.HasValue) q = q.Where(a => a.DetectedAt <= filter.To.Value);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(a => a.DetectedAt)
            .Skip((page - 1) * size).Take(size)
            .Select(a => new AnomalyListItemDto(
                a.Id, a.Reference, a.Severity, a.Status, a.Title,
                a.CashRegisterId, a.CashRegister != null ? a.CashRegister.Code : null,
                a.DetectedAt, a.AssignedTo, a.AssignedToUser != null ? a.AssignedToUser.FullName : null))
            .ToListAsync(ct);

        return new PagedResponse<AnomalyListItemDto> { Items = items, Page = page, PageSize = size, TotalCount = total };
    }

    public async Task<AnomalyDetailDto> GetAsync(int id, CancellationToken ct = default)
    {
        var a = await LoadAsync(id, true, ct) ?? throw new NotFoundException(nameof(AnomalyCase), id);
        return Map(a);
    }

    public async Task<AnomalyDetailDto> CreateAsync(CreateAnomalyDto dto, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException("Non authentifié.");
        var entity = new AnomalyCase
        {
            Reference = NewRef(_clock.UtcNow),
            Severity = dto.Severity,
            Status = AnomalyStatus.OPEN,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            RelatedEntityType = dto.RelatedEntityType,
            RelatedEntityId = dto.RelatedEntityId,
            CashRegisterId = dto.CashRegisterId,
            CashSessionId = dto.CashSessionId,
            DetectedAt = _clock.UtcNow,
            DetectedBy = userId
        };
        _db.AnomalyCases.Add(entity);
        await _audit.LogAsync(AuditAction.CREATE, nameof(AnomalyCase), 0, $"Anomaly {entity.Reference}: {entity.Title}", newValues: dto, ct: ct);
        await _db.SaveChangesAsync(ct);
        return await GetAsync(entity.Id, ct);
    }

    public async Task<AnomalyDetailDto> AssignAsync(int id, AssignAnomalyDto dto, CancellationToken ct = default)
    {
        var a = await _db.AnomalyCases.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(AnomalyCase), id);
        if (a.Status is AnomalyStatus.RESOLVED or AnomalyStatus.CLOSED)
            throw new BusinessRuleException("ANOMALY_INVALID_STATUS", $"Statut '{a.Status}' incompatible.");
        a.AssignedTo = dto.AssignToUserId;
        a.AssignedAt = _clock.UtcNow;
        if (a.Status == AnomalyStatus.OPEN) a.Status = AnomalyStatus.IN_REVIEW;
        await _audit.LogAsync(AuditAction.ASSIGN, nameof(AnomalyCase), a.Id, $"Assigned to user #{dto.AssignToUserId}", ct: ct);
        await _db.SaveChangesAsync(ct);
        return await GetAsync(a.Id, ct);
    }

    public async Task<AnomalyDetailDto> ResolveAsync(int id, ResolveAnomalyDto dto, CancellationToken ct = default)
    {
        var a = await _db.AnomalyCases.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(AnomalyCase), id);
        if (a.Status == AnomalyStatus.CLOSED)
            throw new BusinessRuleException("ANOMALY_INVALID_STATUS", "Déjà fermée.");
        a.Status = AnomalyStatus.RESOLVED;
        a.ResolvedAt = _clock.UtcNow;
        a.ResolvedBy = _currentUser.UserId;
        a.ResolutionComment = dto.ResolutionComment.Trim();
        await _audit.LogAsync(AuditAction.RESOLVE, nameof(AnomalyCase), a.Id, dto.ResolutionComment, ct: ct);
        await _db.SaveChangesAsync(ct);
        return await GetAsync(a.Id, ct);
    }

    public async Task<AnomalyDetailDto> AddCommentAsync(int id, AddAnomalyCommentDto dto, CancellationToken ct = default)
    {
        var a = await _db.AnomalyCases.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(AnomalyCase), id);
        var userId = _currentUser.UserId ?? throw new ForbiddenException("Non authentifié.");
        _db.AnomalyComments.Add(new AnomalyComment
        {
            AnomalyCaseId = a.Id,
            AuthorId = userId,
            Body = dto.Body.Trim()
        });
        await _db.SaveChangesAsync(ct);
        return await GetAsync(a.Id, ct);
    }

    public async Task<AnomalyCase> CreateAutoAsync(string title, string? description, AnomalySeverity severity,
        string? relatedEntityType, int? relatedEntityId, int? cashRegisterId, int? cashSessionId,
        CancellationToken ct = default)
    {
        var a = new AnomalyCase
        {
            Reference = NewRef(_clock.UtcNow),
            Severity = severity,
            Status = AnomalyStatus.OPEN,
            Title = title,
            Description = description,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
            CashRegisterId = cashRegisterId,
            CashSessionId = cashSessionId,
            DetectedAt = _clock.UtcNow,
            DetectedBy = null  // auto
        };
        _db.AnomalyCases.Add(a);
        await _audit.LogAsync(AuditAction.CREATE, nameof(AnomalyCase), 0, $"Auto-anomaly: {title}",
            metadata: new { auto = true, relatedEntityType, relatedEntityId }, ct: ct);
        return a;
    }

    private static string NewRef(DateTime now) => $"AN-{now:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";

    private async Task<AnomalyCase?> LoadAsync(int id, bool noTrack, CancellationToken ct)
    {
        var q = _db.AnomalyCases
            .Include(a => a.CashRegister)
            .Include(a => a.DetectedByUser)
            .Include(a => a.AssignedToUser)
            .Include(a => a.ResolvedByUser)
            .Include(a => a.Comments).ThenInclude(c => c.AuthorUser)
            .AsQueryable();
        if (noTrack) q = q.AsNoTracking();
        return await q.FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    private static AnomalyDetailDto Map(AnomalyCase a) =>
        new(a.Id, a.Reference, a.Severity, a.Status, a.Title, a.Description,
            a.RelatedEntityType, a.RelatedEntityId,
            a.CashRegisterId, a.CashRegister?.Code, a.CashSessionId,
            a.DetectedAt, a.DetectedBy, a.DetectedByUser?.FullName,
            a.AssignedTo, a.AssignedToUser?.FullName, a.AssignedAt,
            a.ResolvedAt, a.ResolvedBy, a.ResolvedByUser?.FullName, a.ResolutionComment,
            a.Comments.OrderBy(c => c.CreatedAt)
                .Select(c => new AnomalyCommentDto(c.Id, c.AuthorId, c.AuthorUser.FullName, c.Body, c.CreatedAt))
                .ToList());
}
