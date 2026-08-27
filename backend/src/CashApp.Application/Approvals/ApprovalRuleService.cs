using CashApp.Application.Approvals.Dtos;
using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Domain.Entities.V2;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.Approvals;

public class ApprovalRuleService : IApprovalRuleService
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;

    public ApprovalRuleService(IAppDbContext db, IAuditLogger audit, IDateTimeProvider clock)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
    }

    public async Task<IReadOnlyList<ApprovalRuleDto>> ListAsync(CancellationToken ct = default)
    {
        // Tri côté client : SQLite ne sait pas ORDER BY un decimal.
        var rules = await _db.ApprovalRules.AsNoTracking()
            .OrderBy(r => r.TargetType)
            .ToListAsync(ct);
        return rules
            .OrderBy(r => r.TargetType)
            .ThenBy(r => r.AmountThreshold ?? 0m)
            .Select(r => new ApprovalRuleDto(r.Id, r.Code, r.Name, r.Description, r.TargetType,
                r.AmountThreshold, r.CurrencyCode, r.RequiredApproverRole, r.IsBlocking, r.IsActive,
                r.CreatedAt, r.UpdatedAt))
            .ToList();
    }

    public async Task<ApprovalRuleDto> CreateAsync(CreateApprovalRuleDto dto, CancellationToken ct = default)
    {
        var code = dto.Code.Trim().ToUpperInvariant();
        if (await _db.ApprovalRules.AnyAsync(r => r.Code == code, ct))
            throw new BusinessRuleException("APPROVAL_RULE_CODE_EXISTS", $"Code '{code}' déjà utilisé.");

        var entity = new ApprovalRule
        {
            Code = code,
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            TargetType = dto.TargetType,
            AmountThreshold = dto.AmountThreshold,
            CurrencyCode = dto.CurrencyCode?.Trim().ToUpperInvariant(),
            RequiredApproverRole = dto.RequiredApproverRole,
            IsBlocking = dto.IsBlocking,
            IsActive = true
        };
        _db.ApprovalRules.Add(entity);
        await _audit.LogAsync(AuditAction.CREATE, nameof(ApprovalRule), 0, $"ApprovalRule {code}", newValues: entity, ct: ct);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<ApprovalRuleDto> UpdateAsync(int id, UpdateApprovalRuleDto dto, CancellationToken ct = default)
    {
        var entity = await _db.ApprovalRules.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(ApprovalRule), id);

        entity.Name = dto.Name.Trim();
        entity.Description = dto.Description?.Trim();
        entity.AmountThreshold = dto.AmountThreshold;
        entity.CurrencyCode = dto.CurrencyCode?.Trim().ToUpperInvariant();
        entity.RequiredApproverRole = dto.RequiredApproverRole;
        entity.IsBlocking = dto.IsBlocking;
        entity.UpdatedAt = _clock.UtcNow;

        await _audit.LogAsync(AuditAction.UPDATE, nameof(ApprovalRule), entity.Id, $"ApprovalRule {entity.Code}", newValues: entity, ct: ct);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task UpdateStatusAsync(int id, UpdateApprovalRuleStatusDto dto, CancellationToken ct = default)
    {
        var entity = await _db.ApprovalRules.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(ApprovalRule), id);
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = _clock.UtcNow;
        await _audit.LogAsync(AuditAction.UPDATE, nameof(ApprovalRule), entity.Id,
            $"ApprovalRule {entity.Code} {(dto.IsActive ? "activated" : "deactivated")}", ct: ct);
        await _db.SaveChangesAsync(ct);
    }

    private static ApprovalRuleDto Map(ApprovalRule r) =>
        new(r.Id, r.Code, r.Name, r.Description, r.TargetType, r.AmountThreshold, r.CurrencyCode,
            r.RequiredApproverRole, r.IsBlocking, r.IsActive, r.CreatedAt, r.UpdatedAt);
}
