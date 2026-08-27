using CashApp.Application.Approvals.Dtos;
using CashApp.Application.BankDeposits;
using CashApp.Application.CashTransfers;
using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Common.Models;
using CashApp.Domain.Entities.V2;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CashApp.Application.Approvals;

public class ApprovalService : IApprovalService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditLogger _audit;
    private readonly IServiceProvider _sp; // résolution paresseuse pour éviter le cycle ApprovalService <-> CashTransferService/BankDepositService

    public ApprovalService(IAppDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock, IAuditLogger audit, IServiceProvider sp)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _audit = audit;
        _sp = sp;
    }

    public async Task<PagedResponse<ApprovalRequestListItemDto>> ListAsync(ApprovalRequestFilterDto filter, CancellationToken ct = default)
    {
        var page = Math.Max(1, filter.Page);
        var size = Math.Clamp(filter.PageSize, 1, 200);

        var q = _db.ApprovalRequests.AsNoTracking()
            .Include(r => r.RequestedByUser)
            .Include(r => r.DecidedByUser)
            .Include(r => r.CashRegister)
            .AsQueryable();

        if (filter.Status.HasValue) q = q.Where(r => r.Status == filter.Status.Value);
        if (filter.TargetType.HasValue) q = q.Where(r => r.TargetType == filter.TargetType.Value);
        if (filter.CashRegisterId.HasValue) q = q.Where(r => r.CashRegisterId == filter.CashRegisterId.Value);
        if (filter.From.HasValue) q = q.Where(r => r.RequestedAt >= filter.From.Value);
        if (filter.To.HasValue) q = q.Where(r => r.RequestedAt <= filter.To.Value);

        var total = await q.CountAsync(ct);

        var sortBy = (filter.SortBy ?? "date").Trim().ToLowerInvariant();
        var asc = string.Equals(filter.SortDir, "asc", StringComparison.OrdinalIgnoreCase);
        q = (sortBy, asc) switch
        {
            ("amount", true)  => q.OrderBy(r => r.Amount).ThenBy(r => r.Id),
            ("amount", false) => q.OrderByDescending(r => r.Amount).ThenByDescending(r => r.Id),
            (_,       true)   => q.OrderBy(r => r.RequestedAt).ThenBy(r => r.Id),
            _                 => q.OrderByDescending(r => r.RequestedAt).ThenByDescending(r => r.Id),
        };

        var items = await q
            .Skip((page - 1) * size).Take(size)
            .Select(r => new ApprovalRequestListItemDto(
                r.Id, r.RequestRef, r.TargetType, r.TargetEntityType, r.TargetEntityId,
                r.CashRegisterId, r.CashRegister != null ? r.CashRegister.Code : null,
                r.Amount, r.CurrencyCode, r.Status,
                r.RequestedBy, r.RequestedByUser.FullName, r.RequestedAt,
                r.DecidedBy, r.DecidedByUser != null ? r.DecidedByUser.FullName : null, r.DecidedAt,
                r.Reason,
                // Résolution de la session de caisse selon la cible.
                r.TargetType == ApprovalTargetType.CASH_OPERATION || r.TargetType == ApprovalTargetType.CASH_OPERATION_CANCEL
                    ? _db.CashOperations.IgnoreQueryFilters().Where(o => o.Id == r.TargetEntityId).Select(o => (int?)o.CashSessionId).FirstOrDefault()
                : r.TargetType == ApprovalTargetType.CASH_TRANSFER
                    ? _db.CashTransfers.Where(t => t.Id == r.TargetEntityId).Select(t => t.SourceSessionId).FirstOrDefault()
                : r.TargetType == ApprovalTargetType.BANK_DEPOSIT
                    ? _db.BankDeposits.Where(d => d.Id == r.TargetEntityId).Select(d => d.CashSessionId).FirstOrDefault()
                : r.TargetType == ApprovalTargetType.CASH_SESSION_CLOSE
                    ? (int?)r.TargetEntityId
                    : null))
            .ToListAsync(ct);

        return new PagedResponse<ApprovalRequestListItemDto>
        {
            Items = items, Page = page, PageSize = size, TotalCount = total
        };
    }

    public async Task<ApprovalRequestDetailDto> GetAsync(int id, CancellationToken ct = default)
    {
        var req = await _db.ApprovalRequests.AsNoTracking()
            .Include(r => r.ApprovalRule)
            .Include(r => r.CashRegister)
            .Include(r => r.RequestedByUser)
            .Include(r => r.DecidedByUser)
            .Include(r => r.Actions).ThenInclude(a => a.PerformedByUser)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(ApprovalRequest), id);
        return MapDetail(req);
    }

    public async Task<ApprovalRequestDetailDto> ApproveAsync(int id, ApproveRequestDto dto, CancellationToken ct = default)
    {
        var req = await _db.ApprovalRequests
            .Include(r => r.ApprovalRule)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(ApprovalRequest), id);

        EnsurePending(req);
        EnsureCanDecide(req);

        var userId = _currentUser.UserId!.Value;
        req.Status = ApprovalStatus.APPROVED;
        req.DecidedBy = userId;
        req.DecidedAt = _clock.UtcNow;
        req.DecisionComment = dto.Comment?.Trim();

        _db.ApprovalActions.Add(new ApprovalAction
        {
            ApprovalRequestId = req.Id,
            ActionType = AuditAction.APPROVE,
            PerformedBy = userId,
            PerformedAt = _clock.UtcNow,
            Comment = dto.Comment?.Trim()
        });

        // Synchronisation auto du statut de l'entité cible : si elle était PENDING_APPROVAL,
        // elle passe à APPROVED. Sinon l'utilisateur ne pourrait jamais finaliser un transfert/dépôt.
        await SyncTargetEntityStatusAsync(req, ApprovalStatus.APPROVED, ct);

        await _audit.LogAsync(AuditAction.APPROVE, nameof(ApprovalRequest), req.Id,
            $"Approved {req.TargetEntityType}#{req.TargetEntityId}",
            metadata: new { req.RequestRef, req.TargetType }, ct: ct);

        await _db.SaveChangesAsync(ct);

        // Auto-finalisation : créer immédiatement les opérations miroir (transferts) ou OUT (dépôts).
        // Si la finalisation échoue (ex. pas de session ouverte), on n'annule PAS l'approbation ;
        // l'utilisateur cliquera "Finaliser" manuellement plus tard.
        await TryAutoCompleteAsync(req, ct);

        return await GetAsync(req.Id, ct);
    }

    public async Task<ApprovalRequestDetailDto> RejectAsync(int id, RejectRequestDto dto, CancellationToken ct = default)
    {
        var req = await _db.ApprovalRequests
            .Include(r => r.ApprovalRule)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(ApprovalRequest), id);

        EnsurePending(req);
        EnsureCanDecide(req);

        var userId = _currentUser.UserId!.Value;
        req.Status = ApprovalStatus.REJECTED;
        req.DecidedBy = userId;
        req.DecidedAt = _clock.UtcNow;
        req.DecisionComment = dto.Comment.Trim();

        _db.ApprovalActions.Add(new ApprovalAction
        {
            ApprovalRequestId = req.Id,
            ActionType = AuditAction.REJECT,
            PerformedBy = userId,
            PerformedAt = _clock.UtcNow,
            Comment = dto.Comment.Trim()
        });

        // Synchronisation : entité cible passe en REJECTED → elle ne pourra plus être finalisée.
        await SyncTargetEntityStatusAsync(req, ApprovalStatus.REJECTED, ct);

        await _audit.LogAsync(AuditAction.REJECT, nameof(ApprovalRequest), req.Id,
            $"Rejected {req.TargetEntityType}#{req.TargetEntityId}",
            metadata: new { req.RequestRef, req.TargetType, reason = dto.Comment }, ct: ct);

        await _db.SaveChangesAsync(ct);
        return await GetAsync(req.Id, ct);
    }

    // Met à jour le statut de l'entité cible (CashTransfer, BankDeposit) selon la décision.
    private async Task SyncTargetEntityStatusAsync(ApprovalRequest req, ApprovalStatus decision, CancellationToken ct)
    {
        switch (req.TargetType)
        {
            case ApprovalTargetType.CASH_TRANSFER:
                var transfer = await _db.CashTransfers.FirstOrDefaultAsync(t => t.Id == req.TargetEntityId, ct);
                if (transfer is not null && transfer.Status == CashTransferStatus.PENDING_APPROVAL)
                {
                    transfer.Status = decision == ApprovalStatus.APPROVED
                        ? CashTransferStatus.APPROVED
                        : CashTransferStatus.REJECTED;
                    if (decision == ApprovalStatus.APPROVED)
                    {
                        transfer.ApprovedBy = _currentUser.UserId;
                        transfer.ApprovedAt = _clock.UtcNow;
                    }
                }
                break;

            case ApprovalTargetType.BANK_DEPOSIT:
                var deposit = await _db.BankDeposits.FirstOrDefaultAsync(d => d.Id == req.TargetEntityId, ct);
                if (deposit is not null && deposit.Status == BankDepositStatus.PENDING_APPROVAL)
                {
                    deposit.Status = decision == ApprovalStatus.APPROVED
                        ? BankDepositStatus.APPROVED
                        : BankDepositStatus.REJECTED;
                    if (decision == ApprovalStatus.APPROVED)
                    {
                        deposit.ApprovedBy = _currentUser.UserId;
                        deposit.ApprovedAt = _clock.UtcNow;
                    }
                }
                break;

            case ApprovalTargetType.VARIANCE_CASE:
                var variance = await _db.VarianceCases.FirstOrDefaultAsync(v => v.Id == req.TargetEntityId, ct);
                if (variance is not null)
                {
                    variance.Status = decision == ApprovalStatus.APPROVED
                        ? VarianceStatus.APPROVED
                        : VarianceStatus.REJECTED;
                }
                break;

            case ApprovalTargetType.CASH_OPERATION:
                var op = await _db.CashOperations.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(o => o.Id == req.TargetEntityId, ct);
                if (op is not null)
                {
                    if (decision == ApprovalStatus.APPROVED)
                    {
                        op.IsPendingApproval = false;
                    }
                    else
                    {
                        // Rejetée → soft delete, le solde théorique sera recalculé.
                        op.IsPendingApproval = false;
                        op.IsDeleted = true;
                        op.DeletedBy = _currentUser.UserId;
                        op.DeletedAt = _clock.UtcNow;
                        op.DeleteReason = $"Rejetée par approbation : {req.DecisionComment}";
                    }
                }
                break;

            case ApprovalTargetType.CASH_OPERATION_CANCEL:
                var opC = await _db.CashOperations.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(o => o.Id == req.TargetEntityId, ct);
                if (opC is not null)
                {
                    if (decision == ApprovalStatus.APPROVED)
                    {
                        // Si l'opération a été validée par le workflow d'approbation, on NE la
                        // supprime PAS : on la garde et on crée une écriture inverse (montant
                        // miroir, direction opposée) pour conserver la traçabilité.
                        var wasWorkflowApproved = await _db.ApprovalRequests.AsNoTracking().AnyAsync(r =>
                            r.TargetType == ApprovalTargetType.CASH_OPERATION
                            && r.TargetEntityId == opC.Id
                            && r.Status == ApprovalStatus.APPROVED, ct);

                        opC.IsPendingCancellation = false;

                        if (wasWorkflowApproved)
                        {
                            await CreateCancellationReversalAsync(opC, req, ct);
                        }
                        else
                        {
                            // Annulation classique : soft delete effectif.
                            opC.IsDeleted = true;
                            opC.DeletedBy = _currentUser.UserId;
                            opC.DeletedAt = _clock.UtcNow;
                            // DeleteReason a été pré-rempli au moment de la demande.
                        }
                    }
                    else
                    {
                        // Annulation rejetée : l'opération redevient APPROVED (active).
                        // Réinitialise explicitement tous les champs de suppression au cas où un
                        // état résiduel les aurait positionnés.
                        opC.IsPendingCancellation = false;
                        opC.IsDeleted = false;
                        opC.DeletedBy = null;
                        opC.DeletedAt = null;
                        opC.DeleteReason = null;
                    }
                }
                break;

            // CASH_SESSION_CLOSE : pas d'entité miroir à synchroniser.
        }
    }

    // Tente de finaliser le transfert/dépôt automatiquement après approbation.
    // En cas d'échec (sessions non ouvertes, etc.), trace dans l'audit et laisse l'utilisateur conclure.
    private async Task TryAutoCompleteAsync(ApprovalRequest req, CancellationToken ct)
    {
        try
        {
            switch (req.TargetType)
            {
                case ApprovalTargetType.CASH_TRANSFER:
                    var transferSvc = _sp.GetRequiredService<ICashTransferService>();
                    await transferSvc.CompleteAsync(req.TargetEntityId, ct);
                    await _audit.LogAsync(AuditAction.COMPLETE, "CashTransfer", req.TargetEntityId,
                        "Auto-finalisation après approbation", ct: ct);
                    await _db.SaveChangesAsync(ct);
                    break;

                case ApprovalTargetType.BANK_DEPOSIT:
                    var depositSvc = _sp.GetRequiredService<IBankDepositService>();
                    await depositSvc.CompleteAsync(req.TargetEntityId, ct);
                    await _audit.LogAsync(AuditAction.COMPLETE, "BankDeposit", req.TargetEntityId,
                        "Auto-finalisation après approbation", ct: ct);
                    await _db.SaveChangesAsync(ct);
                    break;
            }
        }
        catch (Exception ex)
        {
            // On NE remonte PAS l'erreur — l'approbation reste valide.
            await _audit.LogAsync(AuditAction.UPDATE, "ApprovalRequest", req.Id,
                $"Auto-finalisation échouée : {ex.Message}",
                metadata: new { req.TargetType, req.TargetEntityId, error = ex.GetType().Name },
                ct: ct);
            await _db.SaveChangesAsync(ct);
        }
    }

    // Crée une opération inverse (MÊME direction, montant NÉGATIF) pour matérialiser
    // l'annulation d'une opération qui avait été validée par le workflow d'approbation.
    // L'opération d'origine reste visible dans le journal et conserve son montant initial.
    private async Task CreateCancellationReversalAsync(
        CashApp.Domain.Entities.CashOperation original, ApprovalRequest req, CancellationToken ct)
    {
        var refGen = _sp.GetRequiredService<IReferenceGeneratorService>();
        var now = _clock.UtcNow;
        var reason = original.DeleteReason ?? req.DecisionComment ?? "annulation approuvée";

        var reversal = new CashApp.Domain.Entities.CashOperation
        {
            OperationRef = await refGen.NextOperationRefAsync(original.CashRegisterId, now, ct),
            CashRegisterId = original.CashRegisterId,
            CashSessionId = original.CashSessionId,
            OperationDate = now,
            Direction = original.Direction,
            CategoryId = original.CategoryId,
            Amount = -original.Amount,
            CurrencyCode = original.CurrencyCode,
            PaymentMethod = original.PaymentMethod,
            Label = $"ANNULATION — {original.Label}",
            Description = $"Annulation de l'opération {original.OperationRef} (validée par approbation). Motif : {reason}",
            ExternalReference = original.OperationRef,
            ThirdPartyName = original.ThirdPartyName,
            CreatedBy = _currentUser.UserId
        };
        _db.CashOperations.Add(reversal);

        await _audit.LogAsync(AuditAction.CANCEL, nameof(CashApp.Domain.Entities.CashOperation), original.Id,
            $"Annulation par écriture inverse {reversal.OperationRef} (opération validée par workflow).",
            metadata: new { originalRef = original.OperationRef, direction = original.Direction, reversalAmount = reversal.Amount },
            ct: ct);
    }

    public async Task<ApprovalRequest?> FindMatchingRuleAsync(ApprovalTargetType targetType, decimal? amount, string? currency, CancellationToken ct = default)
    {
        // Trouve la règle ACTIVE la plus permissive : seuil <= amount, devise compatible.
        var rules = await _db.ApprovalRules.AsNoTracking()
            .Where(r => r.IsActive && r.TargetType == targetType)
            .ToListAsync(ct);

        var match = rules
            .Where(r =>
                (r.AmountThreshold == null || (amount.HasValue && amount.Value >= r.AmountThreshold.Value))
                && (string.IsNullOrEmpty(r.CurrencyCode) || string.Equals(r.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(r => r.AmountThreshold ?? 0)
            .FirstOrDefault();

        return match is null ? null : new ApprovalRequest { ApprovalRuleId = match.Id };
    }

    public async Task<ApprovalRequest> CreateRequestAsync(int approvalRuleId, ApprovalTargetType targetType,
        string targetEntityType, int targetEntityId, int? cashRegisterId, decimal? amount, string? currency,
        string reason, CancellationToken ct = default)
    {
        var rule = await _db.ApprovalRules.FirstOrDefaultAsync(r => r.Id == approvalRuleId, ct)
            ?? throw new NotFoundException(nameof(ApprovalRule), approvalRuleId);

        var userId = _currentUser.UserId ?? throw new ForbiddenException("Non authentifié.");
        var now = _clock.UtcNow;
        var req = new ApprovalRequest
        {
            RequestRef = $"AR-{now:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}",
            ApprovalRuleId = rule.Id,
            TargetType = targetType,
            TargetEntityType = targetEntityType,
            TargetEntityId = targetEntityId,
            CashRegisterId = cashRegisterId,
            Amount = amount,
            CurrencyCode = currency,
            Reason = reason,
            Status = ApprovalStatus.PENDING,
            RequestedBy = userId,
            RequestedAt = now
        };
        _db.ApprovalRequests.Add(req);

        await _audit.LogAsync(AuditAction.CREATE, nameof(ApprovalRequest), 0,
            $"Pending approval on {targetEntityType}#{targetEntityId}",
            metadata: new { req.RequestRef, targetType, amount }, ct: ct);

        return req;
    }

    private static void EnsurePending(ApprovalRequest r)
    {
        if (r.Status != ApprovalStatus.PENDING)
            throw new BusinessRuleException("APPROVAL_REQUEST_NOT_PENDING",
                $"La demande est déjà {r.Status}.");
    }

    private void EnsureCanDecide(ApprovalRequest r)
    {
        if (!_currentUser.UserId.HasValue) throw new ForbiddenException("Non authentifié.");
        var required = r.ApprovalRule.RequiredApproverRole;
        var isAdmin = _currentUser.IsInRole(Domain.Constants.RoleCodes.Admin);
        if (isAdmin) return;
        if (!_currentUser.IsInRole(required))
            throw new ForbiddenException($"Décision réservée au rôle {required}.");
    }

    private static ApprovalRequestDetailDto MapDetail(ApprovalRequest r) =>
        new(r.Id, r.RequestRef, r.ApprovalRuleId, r.ApprovalRule.Code, r.TargetType,
            r.TargetEntityType, r.TargetEntityId,
            r.CashRegisterId, r.CashRegister?.Code,
            r.Amount, r.CurrencyCode, r.Reason, r.Status,
            r.RequestedBy, r.RequestedByUser.FullName, r.RequestedAt,
            r.DecidedBy, r.DecidedByUser?.FullName, r.DecidedAt, r.DecisionComment,
            r.CreatedAt,
            r.Actions.OrderBy(a => a.PerformedAt)
                .Select(a => new ApprovalActionDto(a.Id, a.ActionType, a.PerformedBy, a.PerformedByUser.FullName, a.PerformedAt, a.Comment))
                .ToList());
}
