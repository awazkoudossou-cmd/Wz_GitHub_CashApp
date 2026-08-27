using CashApp.Application.Approvals;
using CashApp.Application.CashOperations.Dtos;
using CashApp.Application.CashSessions;
using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Common.Models;
using CashApp.Application.Settings;
using CashApp.Application.ThirdParties;
using CashApp.Domain.Constants;
using CashApp.Domain.Entities;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.CashOperations;

public class CashOperationService : ICashOperationService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IReferenceGeneratorService _refGen;
    private readonly ICashSessionService _sessions;
    private readonly ISettingsService _settings;
    private readonly IApprovalService _approval;
    private readonly IFeatureService _features;
    private readonly IAuditLogger _audit;
    private readonly ICashRegisterAccessService _access;
    private readonly IThirdPartyService _thirdParties;

    public CashOperationService(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IReferenceGeneratorService refGen,
        ICashSessionService sessions,
        ISettingsService settings,
        IApprovalService approval,
        IFeatureService features,
        IAuditLogger audit,
        ICashRegisterAccessService access,
        IThirdPartyService thirdParties)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _refGen = refGen;
        _sessions = sessions;
        _settings = settings;
        _approval = approval;
        _access = access;
        _features = features;
        _audit = audit;
        _thirdParties = thirdParties;
    }

    public async Task<PagedResponse<CashOperationListItemDto>> ListAsync(CashOperationFilterDto filter, CancellationToken ct = default)
    {
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 200);

        // Filtrage strict par caisses accessibles à l'utilisateur courant (ADMIN/SUPERVISOR voient tout).
        var accessible = await _access.GetAccessibleRegisterIdsAsync(ct);

        var baseQ = _db.CashOperations.AsNoTracking()
            .Include(o => o.CashRegister)
            .Include(o => o.CashSession)
            .Include(o => o.Category)
            .AsQueryable();
        // IncludeDeleted = true → on bypasse le query filter global pour exposer les ops soft-deleted
        // (annulées/rejetées). Ces ops restent exclues des calculs de solde, qui passent par leur propre requête.
        if (filter.IncludeDeleted) baseQ = baseQ.IgnoreQueryFilters();
        var q = baseQ.Where(o => accessible.Contains(o.CashRegisterId));

        if (filter.CashRegisterId.HasValue) q = q.Where(o => o.CashRegisterId == filter.CashRegisterId.Value);
        if (filter.CashSessionId.HasValue)  q = q.Where(o => o.CashSessionId == filter.CashSessionId.Value);
        if (filter.From.HasValue)           q = q.Where(o => o.OperationDate >= filter.From.Value);
        if (filter.To.HasValue)             q = q.Where(o => o.OperationDate <= filter.To.Value);
        if (filter.Direction.HasValue)      q = q.Where(o => o.Direction == filter.Direction.Value);
        if (filter.CategoryId.HasValue)     q = q.Where(o => o.CategoryId == filter.CategoryId.Value);

        var total = await q.CountAsync(ct);

        var sortBy = (filter.SortBy ?? "date").Trim().ToLowerInvariant();
        var asc = string.Equals(filter.SortDir, "asc", StringComparison.OrdinalIgnoreCase);
        q = (sortBy, asc) switch
        {
            ("ref",  true)  => q.OrderBy(o => o.OperationRef),
            ("ref",  false) => q.OrderByDescending(o => o.OperationRef),
            (_,      true)  => q.OrderBy(o => o.OperationDate).ThenBy(o => o.Id),
            _               => q.OrderByDescending(o => o.OperationDate).ThenByDescending(o => o.Id),
        };

        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new CashOperationListItemDto(
                o.Id, o.OperationRef, o.CashRegisterId, o.CashRegister.Code,
                o.CashSessionId, o.CashSession.Status, o.OperationDate, o.Direction,
                o.CategoryId, o.Category.Label, o.Amount, o.CurrencyCode,
                o.PaymentMethod, o.Label, o.ThirdPartyName, o.IsDeleted,
                _db.CashTransfers.Any(t => t.SourceOperationId == o.Id || t.DestinationOperationId == o.Id)
                  || _db.BankDeposits.Any(d => d.LinkedOperationId == o.Id),
                o.IsPendingApproval,
                o.IsPendingCancellation,
                _db.ApprovalRequests.Any(r => r.TargetType == ApprovalTargetType.CASH_OPERATION && r.TargetEntityId == o.Id)))
            .ToListAsync(ct);

        return new PagedResponse<CashOperationListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<CashOperationDetailDto> GetAsync(int id, CancellationToken ct = default)
    {
        var op = await _db.CashOperations.AsNoTracking()
            .IgnoreQueryFilters()
            .Include(o => o.CashRegister)
            .Include(o => o.CashSession)
            .Include(o => o.Category)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException(nameof(CashOperation), id);

        await _access.EnsureCanAccessAsync(op.CashRegisterId, ct);

        var transfer = await _db.CashTransfers.AsNoTracking()
            .Where(t => t.SourceOperationId == id || t.DestinationOperationId == id)
            .Select(t => new { t.Id, t.TransferRef })
            .FirstOrDefaultAsync(ct);
        var deposit = transfer is null
            ? await _db.BankDeposits.AsNoTracking()
                .Where(d => d.LinkedOperationId == id)
                .Select(d => new { d.Id, d.DepositRef })
                .FirstOrDefaultAsync(ct)
            : null;

        var hasWorkflow = await HasWorkflowRequestAsync(id, ct);

        return Map(op,
            transfer is not null ? ("CashTransfer", transfer.TransferRef, (int?)transfer.Id)
            : deposit is not null ? ("BankDeposit", deposit.DepositRef, (int?)deposit.Id)
            : (null, null, null),
            hasWorkflow);
    }

    public async Task<CashOperationDetailDto> CreateAsync(CreateCashOperationDto dto, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException("Utilisateur non authentifié.");

        var session = await _db.CashSessions
            .Include(s => s.CashRegister)
            .FirstOrDefaultAsync(s => s.Id == dto.CashSessionId, ct)
            ?? throw new NotFoundException(nameof(CashSession), dto.CashSessionId);

        // L'utilisateur doit avoir accès à la caisse de la session.
        await _access.EnsureCanAccessAsync(session.CashRegisterId, ct);

        if (session.Status != CashSessionStatus.OPEN)
            throw new BusinessRuleException("CASH_SESSION_NOT_OPEN",
                "Impossible de saisir une opération : la session n'est pas ouverte.");

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == dto.CategoryId, ct)
            ?? throw new NotFoundException(nameof(Category), dto.CategoryId);

        if (!category.IsActive)
            throw new BusinessRuleException("CATEGORY_INACTIVE", "Catégorie inactive.");

        if (category.Direction != dto.Direction)
            throw new BusinessRuleException("CATEGORY_DIRECTION_MISMATCH",
                $"La catégorie '{category.Code}' attend la direction '{category.Direction}'.");

        if (dto.Amount <= 0)
            throw new BusinessRuleException("AMOUNT_INVALID", "Le montant doit être strictement positif.");

        // Enregistre automatiquement le tiers saisi dans la liste des tiers pré-enregistrés,
        // pour qu'il soit ensuite proposé à la sélection (idempotent : find-or-create).
        var thirdPartyName = dto.ThirdPartyName?.Trim();
        if (!string.IsNullOrWhiteSpace(thirdPartyName))
            await _thirdParties.FindOrCreateAsync(thirdPartyName, ct);

        var op = new CashOperation
        {
            OperationRef = await _refGen.NextOperationRefAsync(session.CashRegisterId, dto.OperationDate, ct),
            CashRegisterId = session.CashRegisterId,
            CashSessionId = session.Id,
            OperationDate = dto.OperationDate,
            Direction = dto.Direction,
            CategoryId = dto.CategoryId,
            Amount = dto.Amount,
            CurrencyCode = session.CashRegister.CurrencyCode,
            PaymentMethod = dto.PaymentMethod,
            Label = dto.Label.Trim(),
            Description = dto.Description?.Trim(),
            ExternalReference = dto.ExternalReference?.Trim(),
            ThirdPartyName = thirdPartyName,
            CreatedBy = userId
        };

        _db.CashOperations.Add(op);
        await _db.SaveChangesAsync(ct);

        // V2-A — Workflow validation : si feature active ET une règle CASH_OPERATION matche,
        // l'opération passe en attente (IsPendingApproval=true). Elle est exclue du solde théorique
        // tant que non approuvée. Le setting global de seuil n'est plus requis : la règle suffit.
        if (await _features.IsEnabledAsync(FeatureCodes.AdvValidation, ct))
        {
            var match = await _approval.FindMatchingRuleAsync(ApprovalTargetType.CASH_OPERATION, op.Amount, op.CurrencyCode, ct);
            if (match is not null)
            {
                await _approval.CreateRequestAsync(match.ApprovalRuleId, ApprovalTargetType.CASH_OPERATION,
                    nameof(CashOperation), op.Id, op.CashRegisterId, op.Amount, op.CurrencyCode,
                    $"Opération {op.OperationRef} montant={op.Amount}", ct);
                op.IsPendingApproval = true;
                await _db.SaveChangesAsync(ct);
            }
        }

        await _audit.LogAsync(AuditAction.CREATE, nameof(CashOperation), op.Id,
            $"Opération {op.OperationRef} ({op.Direction} {op.Amount})", newValues: dto, ct: ct);
        await _db.SaveChangesAsync(ct);

        await _sessions.RecomputeTheoreticalBalanceAsync(session.Id, ct);
        return await GetAsync(op.Id, ct);
    }

    public async Task<CashOperationDetailDto> UpdateAsync(int id, UpdateCashOperationDto dto, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException("Utilisateur non authentifié.");

        var op = await _db.CashOperations
            .Include(o => o.CashSession)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException(nameof(CashOperation), id);

        if (op.IsDeleted)
            throw new BusinessRuleException("OPERATION_DELETED", "Opération annulée — non modifiable.");

        // Écriture de contrepassation (montant négatif) → figée.
        if (op.Amount < 0)
            throw new BusinessRuleException("OPERATION_IS_REVERSAL",
                "Écriture d'annulation (montant négatif) — non modifiable.");

        // Toute opération qui est passée par le workflow d'approbation (peu importe l'issue :
        // PENDING, APPROVED, REJECTED, CANCELLED) est figée : on ne peut plus la modifier.
        if (await HasWorkflowRequestAsync(op.Id, ct))
            throw new BusinessRuleException("OPERATION_WORKFLOW_LOCKED",
                "Opération passée par le workflow d'approbation — non modifiable.");

        await _access.EnsureCanAccessAsync(op.CashRegisterId, ct);
        await EnsureNotLinkedToSystemEntityAsync(op.Id, "modifier", ct);

        if (op.CashSession.Status != CashSessionStatus.OPEN)
        {
            var allowEdit = bool.TryParse(
                await _settings.GetRawAsync(SettingKeys.AllowOperationEditBeforeSessionClose, ct),
                out var b) && b;
            // Note : ce setting autorise l'édition AVANT clôture. Une fois fermé, refus.
            if (!allowEdit)
                throw new BusinessRuleException("OPERATION_EDIT_FORBIDDEN",
                    "Édition désactivée par configuration.");
            throw new BusinessRuleException("CASH_SESSION_CLOSED",
                "Impossible de modifier une opération dans une session fermée.");
        }

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == dto.CategoryId, ct)
            ?? throw new NotFoundException(nameof(Category), dto.CategoryId);

        if (category.Direction != op.Direction)
            throw new BusinessRuleException("CATEGORY_DIRECTION_MISMATCH",
                $"La catégorie '{category.Code}' attend la direction '{category.Direction}'.");

        if (dto.Amount <= 0)
            throw new BusinessRuleException("AMOUNT_INVALID", "Le montant doit être strictement positif.");

        var thirdPartyName = dto.ThirdPartyName?.Trim();
        if (!string.IsNullOrWhiteSpace(thirdPartyName))
            await _thirdParties.FindOrCreateAsync(thirdPartyName, ct);

        op.OperationDate = dto.OperationDate;
        op.CategoryId = dto.CategoryId;
        op.Amount = dto.Amount;
        op.PaymentMethod = dto.PaymentMethod;
        op.Label = dto.Label.Trim();
        op.Description = dto.Description?.Trim();
        op.ExternalReference = dto.ExternalReference?.Trim();
        op.ThirdPartyName = thirdPartyName;
        op.UpdatedBy = userId;
        op.UpdatedAt = _clock.UtcNow;

        // Re-évaluation du workflow d'approbation après modification du montant :
        // si une règle CASH_OPERATION matche désormais le nouveau montant ET que l'op n'est pas
        // déjà PENDING, on bascule en attente d'approbation et on crée la demande.
        // L'op ne peut atteindre cette branche que sans historique workflow (vérifié plus haut).
        if (!op.IsPendingApproval
            && await _features.IsEnabledAsync(FeatureCodes.AdvValidation, ct))
        {
            var match = await _approval.FindMatchingRuleAsync(
                ApprovalTargetType.CASH_OPERATION, op.Amount, op.CurrencyCode, ct);
            if (match is not null)
            {
                await _approval.CreateRequestAsync(match.ApprovalRuleId, ApprovalTargetType.CASH_OPERATION,
                    nameof(CashOperation), op.Id, op.CashRegisterId, op.Amount, op.CurrencyCode,
                    $"Opération {op.OperationRef} mise à jour — nouveau montant={op.Amount}", ct);
                op.IsPendingApproval = true;
                await _audit.LogAsync(AuditAction.UPDATE, nameof(CashOperation), op.Id,
                    $"Nouveau montant {op.Amount} déclenche une demande d'approbation.", ct: ct);
            }
        }

        await _db.SaveChangesAsync(ct);
        await _sessions.RecomputeTheoreticalBalanceAsync(op.CashSessionId, ct);

        return await GetAsync(op.Id, ct);
    }

    public async Task CancelAsync(int id, CancelCashOperationDto dto, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException("Utilisateur non authentifié.");

        var op = await _db.CashOperations
            .Include(o => o.CashSession)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException(nameof(CashOperation), id);

        if (op.IsDeleted)
            throw new BusinessRuleException("OPERATION_ALREADY_CANCELLED",
                "Opération déjà annulée — action impossible.");

        // Une écriture de contrepassation (montant négatif, créée par CreateCancellationReversalAsync)
        // ne peut pas être annulée : elle est figée pour garantir la traçabilité comptable.
        if (op.Amount < 0)
            throw new BusinessRuleException("OPERATION_IS_REVERSAL",
                "Écriture d'annulation (montant négatif) — non annulable.");

        await _access.EnsureCanAccessAsync(op.CashRegisterId, ct);
        await EnsureNotLinkedToSystemEntityAsync(op.Id, "annuler", ct);

        if (op.CashSession.Status != CashSessionStatus.OPEN)
            throw new BusinessRuleException("CASH_SESSION_CLOSED",
                "Impossible d'annuler une opération d'une session fermée.");

        // Une op encore PENDING_APPROVAL prime sur PendingCancellation (état hérité possible) :
        // on la laisse passer pour l'annulation directe gérée plus bas.
        if (op.IsPendingCancellation && !op.IsPendingApproval)
            throw new BusinessRuleException("OPERATION_CANCEL_PENDING",
                "Une demande d'annulation est déjà en cours d'approbation.");

        // Une opération validée par le workflow ne peut être annulée QUE via le workflow.
        // Pas de chemin direct, même si AdvValidation est désactivé ou qu'aucune règle ne matche.
        if (await WasWorkflowApprovedAsync(op.Id, ct))
        {
            if (!await _features.IsEnabledAsync(FeatureCodes.AdvValidation, ct))
                throw new BusinessRuleException("OPERATION_CANCEL_WORKFLOW_REQUIRED",
                    "Cette opération a été validée par le workflow : son annulation requiert le workflow d'approbation (actuellement désactivé).");

            var match = await _approval.FindMatchingRuleAsync(ApprovalTargetType.CASH_OPERATION_CANCEL, op.Amount, op.CurrencyCode, ct)
                ?? throw new BusinessRuleException("OPERATION_CANCEL_WORKFLOW_REQUIRED",
                    "Cette opération a été validée par le workflow : son annulation requiert une règle d'approbation CASH_OPERATION_CANCEL.");

            await _approval.CreateRequestAsync(match.ApprovalRuleId, ApprovalTargetType.CASH_OPERATION_CANCEL,
                nameof(CashOperation), op.Id, op.CashRegisterId, op.Amount, op.CurrencyCode,
                $"Annulation opération {op.OperationRef} : {dto.Reason}", ct);
            op.IsPendingCancellation = true;
            op.DeleteReason = dto.Reason.Trim();
            await _audit.LogAsync(AuditAction.UPDATE, nameof(CashOperation), op.Id,
                $"Annulation en attente d'approbation : {dto.Reason}", ct: ct);
            await _db.SaveChangesAsync(ct);
            return;
        }

        // Une opération encore en attente d'approbation (création non validée) peut être
        // annulée directement, sans nouveau workflow d'approbation. On supprime au passage
        // toutes les demandes d'approbation associées (création ET annulation éventuelle)
        // pour qu'elles ne traînent pas dans la liste des demandes à traiter.
        if (op.IsPendingApproval)
        {
            var relatedRequests = await _db.ApprovalRequests
                .Where(r => r.TargetEntityId == op.Id
                            && (r.TargetType == ApprovalTargetType.CASH_OPERATION
                                || r.TargetType == ApprovalTargetType.CASH_OPERATION_CANCEL))
                .ToListAsync(ct);
            _db.ApprovalRequests.RemoveRange(relatedRequests);
            op.IsPendingApproval = false;
            op.IsPendingCancellation = false;
        }
        // V2-A — Si feature AdvValidation active et une règle CASH_OPERATION_CANCEL matche,
        // on ne supprime PAS immédiatement : on crée une demande d'approbation.
        // L'opération reste active jusqu'à décision (IsPendingCancellation=true).
        // Exception : opération encore PENDING_APPROVAL → annulation directe (cf. bloc précédent).
        else if (await _features.IsEnabledAsync(FeatureCodes.AdvValidation, ct))
        {
            var match = await _approval.FindMatchingRuleAsync(ApprovalTargetType.CASH_OPERATION_CANCEL, op.Amount, op.CurrencyCode, ct);
            if (match is not null)
            {
                await _approval.CreateRequestAsync(match.ApprovalRuleId, ApprovalTargetType.CASH_OPERATION_CANCEL,
                    nameof(CashOperation), op.Id, op.CashRegisterId, op.Amount, op.CurrencyCode,
                    $"Annulation opération {op.OperationRef} : {dto.Reason}", ct);
                op.IsPendingCancellation = true;
                op.DeleteReason = dto.Reason.Trim();  // mémorise le motif demandé pour la décision
                await _audit.LogAsync(AuditAction.UPDATE, nameof(CashOperation), op.Id,
                    $"Annulation en attente d'approbation : {dto.Reason}", ct: ct);
                await _db.SaveChangesAsync(ct);
                return;
            }
        }

        op.IsDeleted = true;
        op.DeletedAt = _clock.UtcNow;
        op.DeletedBy = userId;
        op.DeleteReason = dto.Reason.Trim();

        await _audit.LogAsync(AuditAction.CANCEL, nameof(CashOperation), op.Id,
            $"Opération {op.OperationRef} annulée : {dto.Reason}", ct: ct);
        await _db.SaveChangesAsync(ct);
        await _sessions.RecomputeTheoreticalBalanceAsync(op.CashSessionId, ct);
    }

    // Détecte si l'opération a été validée par le workflow d'approbation (au moins une
    // ApprovalRequest CASH_OPERATION ciblant cette op et passée en APPROVED).
    private Task<bool> WasWorkflowApprovedAsync(int operationId, CancellationToken ct) =>
        _db.ApprovalRequests.AsNoTracking().AnyAsync(r =>
            r.TargetType == ApprovalTargetType.CASH_OPERATION
            && r.TargetEntityId == operationId
            && r.Status == ApprovalStatus.APPROVED, ct);

    // Détecte si l'opération est passée par le workflow d'approbation, quelle qu'en soit l'issue.
    // Utilisé pour figer la modification : une op qui a déclenché un workflow ne doit plus bouger.
    private Task<bool> HasWorkflowRequestAsync(int operationId, CancellationToken ct) =>
        _db.ApprovalRequests.AsNoTracking().AnyAsync(r =>
            r.TargetType == ApprovalTargetType.CASH_OPERATION
            && r.TargetEntityId == operationId, ct);

    private async Task EnsureNotLinkedToSystemEntityAsync(int operationId, string verb, CancellationToken ct)
    {
        var transferRef = await _db.CashTransfers.AsNoTracking()
            .Where(t => t.SourceOperationId == operationId || t.DestinationOperationId == operationId)
            .Select(t => t.TransferRef).FirstOrDefaultAsync(ct);
        if (!string.IsNullOrEmpty(transferRef))
            throw new BusinessRuleException("OPERATION_LOCKED_BY_TRANSFER",
                $"Impossible de {verb} : cette opération est liée au transfert {transferRef}.");

        var depositRef = await _db.BankDeposits.AsNoTracking()
            .Where(d => d.LinkedOperationId == operationId)
            .Select(d => d.DepositRef).FirstOrDefaultAsync(ct);
        if (!string.IsNullOrEmpty(depositRef))
            throw new BusinessRuleException("OPERATION_LOCKED_BY_DEPOSIT",
                $"Impossible de {verb} : cette opération est liée au dépôt banque {depositRef}.");
    }

    private static CashOperationDetailDto Map(CashOperation o,
        (string? Type, string? Reference, int? Id) lockedBy = default,
        bool hasWorkflowHistory = false) =>
        new(o.Id, o.OperationRef, o.CashRegisterId, o.CashRegister.Code, o.CashRegister.Name,
            o.CashSessionId, o.CashSession.Status, o.OperationDate, o.Direction,
            o.CategoryId, o.Category.Code, o.Category.Label,
            o.Amount, o.CurrencyCode, o.PaymentMethod,
            o.Label, o.Description, o.ExternalReference, o.ThirdPartyName,
            o.CreatedBy, o.CreatedAt, o.UpdatedBy, o.UpdatedAt,
            o.IsDeleted, o.DeletedBy, o.DeletedAt, o.DeleteReason,
            lockedBy.Type != null, lockedBy.Type, lockedBy.Reference, lockedBy.Id,
            o.IsPendingApproval, o.IsPendingCancellation, hasWorkflowHistory);
}
