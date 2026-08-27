using CashApp.Application.Approvals;
using CashApp.Application.BankDeposits.Dtos;
using CashApp.Application.CategoryGroups;
using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Common.Models;
using CashApp.Application.Settings;
using CashApp.Domain.Constants;
using CashApp.Domain.Entities;
using CashApp.Domain.Entities.V2;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.BankDeposits;

public class BankDepositService : IBankDepositService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ICashRegisterAccessService _access;
    private readonly IApprovalService _approval;
    private readonly ISettingsService _settings;
    private readonly IAuditLogger _audit;
    private readonly IReferenceGeneratorService _refGen;
    private readonly ICategoryGroupService _categoryGroups;

    public BankDepositService(IAppDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock,
        ICashRegisterAccessService access, IApprovalService approval, ISettingsService settings,
        IAuditLogger audit, IReferenceGeneratorService refGen, ICategoryGroupService categoryGroups)
    {
        _db = db; _currentUser = currentUser; _clock = clock; _access = access;
        _approval = approval; _settings = settings; _audit = audit; _refGen = refGen;
        _categoryGroups = categoryGroups;
    }

    public async Task<PagedResponse<BankDepositListItemDto>> ListAsync(BankDepositFilterDto filter, CancellationToken ct = default)
    {
        var page = Math.Max(1, filter.Page);
        var size = Math.Clamp(filter.PageSize, 1, 200);
        var accessible = await _access.GetAccessibleRegisterIdsAsync(ct);

        var q = _db.BankDeposits.AsNoTracking()
            .Include(d => d.CashRegister)
            .Include(d => d.RequestedByUser)
            .Where(d => accessible.Contains(d.CashRegisterId));

        if (filter.Status.HasValue) q = q.Where(d => d.Status == filter.Status.Value);
        if (filter.CashRegisterId.HasValue) q = q.Where(d => d.CashRegisterId == filter.CashRegisterId.Value);
        if (filter.From.HasValue) q = q.Where(d => d.DepositDate >= filter.From.Value);
        if (filter.To.HasValue) q = q.Where(d => d.DepositDate <= filter.To.Value);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(d => d.DepositDate)
            .Skip((page - 1) * size).Take(size)
            .Select(d => new BankDepositListItemDto(
                d.Id, d.DepositRef, d.CashRegisterId, d.CashRegister.Code,
                d.DepositDate, d.Amount, d.CurrencyCode, d.BankName, d.Status,
                d.RequestedBy, d.RequestedByUser.FullName, d.CreatedAt))
            .ToListAsync(ct);

        return new PagedResponse<BankDepositListItemDto> { Items = items, Page = page, PageSize = size, TotalCount = total };
    }

    public async Task<BankDepositDetailDto> GetAsync(int id, CancellationToken ct = default)
    {
        var d = await LoadAsync(id, true, ct) ?? throw new NotFoundException(nameof(BankDeposit), id);
        await _access.EnsureCanAccessAsync(d.CashRegisterId, ct);
        return Map(d);
    }

    public async Task<BankDepositDetailDto> CreateAsync(CreateBankDepositDto dto, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException("Non authentifié.");
        await _access.EnsureCanAccessAsync(dto.CashRegisterId, ct);

        var register = await _db.CashRegisters.FirstOrDefaultAsync(r => r.Id == dto.CashRegisterId, ct)
            ?? throw new NotFoundException(nameof(CashRegister), dto.CashRegisterId);
        if (!register.IsActive) throw new BusinessRuleException("CASH_REGISTER_INACTIVE", "Caisse inactive.");
        if (dto.Amount <= 0) throw new BusinessRuleException("AMOUNT_INVALID", "Montant > 0 obligatoire.");

        var now = _clock.UtcNow;
        var entity = new BankDeposit
        {
            DepositRef = $"BD-{now:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}",
            CashRegisterId = dto.CashRegisterId,
            DepositDate = dto.DepositDate,
            Amount = dto.Amount,
            CurrencyCode = dto.CurrencyCode.Trim().ToUpperInvariant(),
            BankName = dto.BankName.Trim(),
            AccountReference = dto.AccountReference?.Trim(),
            DepositSlipReference = dto.DepositSlipReference?.Trim(),
            Description = dto.Description?.Trim(),
            RequestedBy = userId,
            Status = BankDepositStatus.DRAFT
        };
        _db.BankDeposits.Add(entity);
        await _db.SaveChangesAsync(ct);

        var requireApproval = bool.TryParse(
            await _settings.GetRawAsync(SettingKeys.ApprovalRequiredForBankDeposit, ct), out var b) && b;
        if (requireApproval)
        {
            var match = await _approval.FindMatchingRuleAsync(ApprovalTargetType.BANK_DEPOSIT, dto.Amount, entity.CurrencyCode, ct);
            if (match is not null)
            {
                var request = await _approval.CreateRequestAsync(
                    match.ApprovalRuleId, ApprovalTargetType.BANK_DEPOSIT,
                    nameof(BankDeposit), entity.Id,
                    entity.CashRegisterId, entity.Amount, entity.CurrencyCode,
                    $"Dépôt banque {entity.DepositRef} {entity.Amount} {entity.CurrencyCode}", ct);
                entity.Status = BankDepositStatus.PENDING_APPROVAL;
                // 1) On persiste la demande pour qu'elle reçoive son Id auto-généré.
                await _db.SaveChangesAsync(ct);
                // 2) On peut maintenant lier la demande au dépôt (FK valide).
                entity.ApprovalRequestId = request.Id;
                await _db.SaveChangesAsync(ct);
            }
        }

        await _audit.LogAsync(AuditAction.CREATE, nameof(BankDeposit), entity.Id, $"Dépôt {entity.DepositRef}", newValues: dto, ct: ct);
        await _db.SaveChangesAsync(ct);
        return await GetAsync(entity.Id, ct);
    }

    public async Task<BankDepositDetailDto> CompleteAsync(int id, CancellationToken ct = default)
    {
        var d = await LoadAsync(id, false, ct) ?? throw new NotFoundException(nameof(BankDeposit), id);
        await _access.EnsureCanAccessAsync(d.CashRegisterId, ct);

        if (d.Status == BankDepositStatus.PENDING_APPROVAL)
            throw new BusinessRuleException("DEPOSIT_PENDING_APPROVAL", "En attente d'approbation.");
        if (d.Status is BankDepositStatus.COMPLETED or BankDepositStatus.REJECTED or BankDepositStatus.CANCELLED)
            throw new BusinessRuleException("DEPOSIT_INVALID_STATUS", $"Statut '{d.Status}' incompatible.");

        if (d.ApprovalRequestId.HasValue)
        {
            var req = await _db.ApprovalRequests.FirstOrDefaultAsync(r => r.Id == d.ApprovalRequestId.Value, ct);
            if (req is null || req.Status != ApprovalStatus.APPROVED)
                throw new BusinessRuleException("DEPOSIT_NOT_APPROVED", "Approbation non encore validée.");
            if (d.Status == BankDepositStatus.PENDING_APPROVAL) d.Status = BankDepositStatus.APPROVED;
        }

        var session = await _db.CashSessions.FirstOrDefaultAsync(s => s.CashRegisterId == d.CashRegisterId && s.Status == CashSessionStatus.OPEN, ct)
            ?? throw new BusinessRuleException("SESSION_NOT_OPEN", "Aucune session ouverte sur cette caisse.");

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Code == "BANK_DEPOSIT", ct);
        if (category is null)
        {
            var group = await _categoryGroups.FindOrCreateAsync("Banque", ct);
            category = new Category { Code = "BANK_DEPOSIT", Label = "Dépôt banque", Direction = OperationDirection.OUT, IsActive = true, GroupId = group.Id };
            _db.Categories.Add(category);
            await _db.SaveChangesAsync(ct);
        }

        var userId = _currentUser.UserId ?? throw new ForbiddenException("Non authentifié.");
        var op = new CashOperation
        {
            OperationRef = await _refGen.NextOperationRefAsync(d.CashRegisterId, d.DepositDate, ct),
            CashRegisterId = d.CashRegisterId,
            CashSessionId = session.Id,
            OperationDate = d.DepositDate,
            Direction = OperationDirection.OUT,
            CategoryId = category.Id,
            Amount = d.Amount,
            CurrencyCode = d.CurrencyCode,
            PaymentMethod = PaymentMethod.BANK_TRANSFER,
            Label = $"Dépôt banque {d.BankName} ({d.DepositRef})",
            ExternalReference = d.DepositRef,
            CreatedBy = userId
        };
        _db.CashOperations.Add(op);
        await _db.SaveChangesAsync(ct);

        d.LinkedOperationId = op.Id;
        d.CashSessionId = session.Id;
        d.Status = BankDepositStatus.COMPLETED;
        d.CompletedAt = _clock.UtcNow;

        // Recalcul solde — exclure les ops en attente d'approbation, sinon le théorique est gonflé.
        var ops = await _db.CashOperations.AsNoTracking()
            .Where(o => o.CashSessionId == session.Id && !o.IsPendingApproval)
            .Select(o => new { o.Direction, o.Amount }).ToListAsync(ct);
        var totalIn = ops.Where(o => o.Direction == OperationDirection.IN).Sum(o => o.Amount);
        var totalOut = ops.Where(o => o.Direction == OperationDirection.OUT).Sum(o => o.Amount);
        session.TheoreticalBalance = session.OpeningBalance + totalIn - totalOut;

        await _audit.LogAsync(AuditAction.COMPLETE, nameof(BankDeposit), d.Id, $"Dépôt {d.DepositRef} finalisé",
            metadata: new { opRef = op.OperationRef }, ct: ct);
        await _db.SaveChangesAsync(ct);
        return await GetAsync(d.Id, ct);
    }

    public async Task<BankDepositDetailDto> CancelAsync(int id, CancelBankDepositDto dto, CancellationToken ct = default)
    {
        var d = await LoadAsync(id, false, ct) ?? throw new NotFoundException(nameof(BankDeposit), id);
        await _access.EnsureCanAccessAsync(d.CashRegisterId, ct);
        if (d.Status is BankDepositStatus.COMPLETED or BankDepositStatus.CANCELLED)
            throw new BusinessRuleException("DEPOSIT_INVALID_STATUS", $"Statut '{d.Status}' non annulable.");
        d.Status = BankDepositStatus.CANCELLED;
        d.CancelledAt = _clock.UtcNow;
        await _audit.LogAsync(AuditAction.CANCEL, nameof(BankDeposit), d.Id, dto.Reason, ct: ct);
        await _db.SaveChangesAsync(ct);
        return await GetAsync(d.Id, ct);
    }

    private async Task<BankDeposit?> LoadAsync(int id, bool asNoTracking, CancellationToken ct)
    {
        var q = _db.BankDeposits
            .Include(d => d.CashRegister)
            .Include(d => d.RequestedByUser)
            .Include(d => d.ApprovedByUser)
            .Include(d => d.LinkedOperation)
            .AsQueryable();
        if (asNoTracking) q = q.AsNoTracking();
        return await q.FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    private static BankDepositDetailDto Map(BankDeposit d) =>
        new(d.Id, d.DepositRef, d.CashRegisterId, d.CashRegister.Code, d.CashRegister.Name, d.CashSessionId,
            d.DepositDate, d.Amount, d.CurrencyCode, d.BankName, d.AccountReference, d.DepositSlipReference, d.Description,
            d.Status, d.RequestedBy, d.RequestedByUser.FullName,
            d.ApprovedBy, d.ApprovedByUser?.FullName, d.ApprovedAt, d.CompletedAt, d.CancelledAt,
            d.LinkedOperationId, d.LinkedOperation?.OperationRef,
            d.ApprovalRequestId, d.CreatedAt, d.UpdatedAt);
}
