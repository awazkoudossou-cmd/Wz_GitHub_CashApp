using CashApp.Application.Accounting;
using CashApp.Application.Anomalies;
using CashApp.Application.Approvals;
using CashApp.Application.CashSessions.Dtos;
using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Settings;
using CashApp.Application.Variances;
using CashApp.Domain.Constants;
using CashApp.Domain.Entities;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.CashSessions;

public class CashSessionService : ICashSessionService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ISettingsService _settings;
    private readonly IFeatureService _features;
    private readonly IVarianceService _variance;
    private readonly IAnomalyService _anomaly;
    private readonly IApprovalService _approval;
    private readonly IAuditLogger _audit;
    private readonly ICashRegisterAccessService _access;
    private readonly IAccountingQueueService _accountingQueue;

    public CashSessionService(IAppDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock,
        ISettingsService settings, IFeatureService features, IVarianceService variance,
        IAnomalyService anomaly, IApprovalService approval, IAuditLogger audit,
        ICashRegisterAccessService access, IAccountingQueueService accountingQueue)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _settings = settings;
        _features = features;
        _variance = variance;
        _anomaly = anomaly;
        _approval = approval;
        _audit = audit;
        _access = access;
        _accountingQueue = accountingQueue;
    }

    public async Task<IReadOnlyList<CashSessionListItemDto>> ListAsync(int? cashRegisterId, CancellationToken ct = default)
    {
        // Filtrage strict par caisses accessibles à l'utilisateur courant.
        var accessible = await _access.GetAccessibleRegisterIdsAsync(ct);

        var q = _db.CashSessions.AsNoTracking()
            .Include(s => s.CashRegister)
            .Include(s => s.OpenedByUser)
            .Where(s => accessible.Contains(s.CashRegisterId))
            .AsQueryable();

        if (cashRegisterId.HasValue)
            q = q.Where(s => s.CashRegisterId == cashRegisterId.Value);

        return await q
            .OrderByDescending(s => s.OpenedAt)
            .Select(s => new CashSessionListItemDto(
                s.Id, s.CashRegisterId, s.CashRegister.Code, s.CashRegister.Name,
                s.OpenedBy, s.OpenedByUser.FullName,
                s.OpenedAt, s.OpeningBalance,
                s.ClosedBy, s.ClosedAt,
                s.Status, s.TheoreticalBalance, s.PhysicalBalance, s.VarianceAmount))
            .ToListAsync(ct);
    }

    public async Task<CashSessionDetailDto> GetAsync(int id, CancellationToken ct = default)
    {
        var session = await _db.CashSessions.AsNoTracking()
            .Include(s => s.CashRegister)
            .Include(s => s.OpenedByUser)
            .Include(s => s.ClosedByUser)
            .FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException(nameof(CashSession), id);

        await _access.EnsureCanAccessAsync(session.CashRegisterId, ct);

        var summary = await BuildSummaryAsync(session.Id, ct);
        return Map(session, summary);
    }

    public async Task<CashSessionDetailDto> OpenAsync(OpenCashSessionDto dto, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException("Utilisateur non authentifié.");

        var register = await _db.CashRegisters.FirstOrDefaultAsync(r => r.Id == dto.CashRegisterId, ct)
            ?? throw new NotFoundException(nameof(CashRegister), dto.CashRegisterId);

        if (!register.IsActive)
            throw new BusinessRuleException("CASH_REGISTER_INACTIVE", "La caisse est inactive.");

        var isAdminOrSupervisor = _currentUser.IsInRole(RoleCodes.Admin) || _currentUser.IsInRole(RoleCodes.Supervisor);
        if (!isAdminOrSupervisor)
        {
            var assigned = await _db.UserCashRegisters
                .AnyAsync(uc => uc.UserId == userId && uc.CashRegisterId == dto.CashRegisterId, ct);
            if (!assigned)
                throw new ForbiddenException("Vous n'êtes pas affecté à cette caisse.");
        }

        var alreadyOpen = await _db.CashSessions
            .AnyAsync(s => s.CashRegisterId == dto.CashRegisterId && s.Status == CashSessionStatus.OPEN, ct);
        if (alreadyOpen)
            throw new BusinessRuleException("CASH_SESSION_ALREADY_OPEN",
                "Une session est déjà ouverte sur cette caisse.");

        if (dto.OpeningBalance < 0)
            throw new BusinessRuleException("OPENING_BALANCE_NEGATIVE", "Le solde d'ouverture doit être >= 0.");

        var session = new CashSession
        {
            CashRegisterId = dto.CashRegisterId,
            OpenedBy = userId,
            OpenedAt = _clock.UtcNow,
            OpeningBalance = dto.OpeningBalance,
            OpenComment = dto.OpenComment?.Trim(),
            Status = CashSessionStatus.OPEN,
            TheoreticalBalance = dto.OpeningBalance
        };
        _db.CashSessions.Add(session);
        await _db.SaveChangesAsync(ct);
        return await GetAsync(session.Id, ct);
    }

    public async Task<CashSessionDetailDto> CloseAsync(int id, CloseCashSessionDto dto, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException("Utilisateur non authentifié.");

        var session = await _db.CashSessions.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException(nameof(CashSession), id);

        if (session.Status != CashSessionStatus.OPEN)
            throw new BusinessRuleException("CASH_SESSION_NOT_OPEN", "La session n'est pas ouverte.");

        // Règle : le caissier ne peut fermer que sa propre session ; ADMIN/SUPERVISOR peut fermer selon setting.
        var isAdmin = _currentUser.IsInRole(RoleCodes.Admin);
        var isSupervisor = _currentUser.IsInRole(RoleCodes.Supervisor);
        var allowSupervisorAny = bool.TryParse(
            await _settings.GetRawAsync(SettingKeys.AllowSupervisorCloseAnySession, ct),
            out var b) && b;

        if (!isAdmin && !(isSupervisor && allowSupervisorAny) && session.OpenedBy != userId)
            throw new ForbiddenException("Vous ne pouvez fermer que les sessions que vous avez ouvertes.");

        // Vérifie les éléments encore en attente d'approbation (ops, transferts, dépôts).
        // Si l'utilisateur n'a pas confirmé leur suppression, on bloque la clôture.
        var pending = await GetPendingItemsAsync(session.Id, ct);
        if (pending.HasAny)
        {
            if (!dto.ConfirmDeletePending)
            {
                throw new BusinessRuleException("PENDING_ITEMS_BLOCK_CLOSE",
                    $"Éléments en attente d'approbation : {pending.PendingOperationCount} opération(s), " +
                    $"{pending.PendingTransferCount} transfert(s), {pending.PendingDepositCount} dépôt(s). " +
                    "Confirmez la suppression pour clôturer.");
            }
            await DeletePendingItemsAsync(session.Id, userId, ct);
        }

        var theoretical = await ComputeTheoreticalBalanceAsync(session, ct);
        var computedVariance = dto.PhysicalBalance - theoretical;
        var justificationProvided = !string.IsNullOrWhiteSpace(dto.VarianceJustification);

        // Règle : tout écart impose une justification, sauf en dessous d'un seuil de tolérance
        // configuré via VARIANCE_JUSTIFICATION_THRESHOLD (lu en culture invariante).
        // VARIANCE_FORCE_JUSTIFICATION_BELOW_THRESHOLD=true force la justification même en dessous.
        if (computedVariance != 0 && !justificationProvided)
        {
            var ciInv = System.Globalization.CultureInfo.InvariantCulture;
            var thrRaw = await _settings.GetRawAsync(SettingKeys.VarianceJustificationThreshold, ct);
            decimal.TryParse(thrRaw, System.Globalization.NumberStyles.Number, ciInv, out var justifyThreshold);
            var forceBelow = bool.TryParse(
                await _settings.GetRawAsync(SettingKeys.VarianceForceJustificationBelowThreshold, ct),
                out var fb) && fb;

            var smallVariance = justifyThreshold > 0 && Math.Abs(computedVariance) < justifyThreshold;
            if (!smallVariance || forceBelow)
            {
                throw new BusinessRuleException("VARIANCE_JUSTIFICATION_REQUIRED",
                    $"Écart de {computedVariance:N2} détecté. Une justification écrite est obligatoire pour clôturer la session.");
            }
        }

        session.TheoreticalBalance = theoretical;
        session.PhysicalBalance = dto.PhysicalBalance;
        session.VarianceAmount = computedVariance;

        // Concatène la justification au commentaire de clôture pour la rendre visible même
        // si la feature ADV_VARIANCE_MANAGEMENT est désactivée (pas de VarianceCase créé).
        var closeComment = dto.CloseComment?.Trim();
        if (justificationProvided)
        {
            var justification = $"Justification écart ({computedVariance:N2}) : {dto.VarianceJustification!.Trim()}";
            closeComment = string.IsNullOrWhiteSpace(closeComment)
                ? justification
                : $"{closeComment}\n{justification}";
        }
        session.CloseComment = closeComment;
        session.ClosedAt = _clock.UtcNow;
        session.ClosedBy = userId;
        session.Status = CashSessionStatus.CLOSED;

        // Création d'un VarianceCase :
        //   - Si setting VARIANCE_TRACK_ALL_NON_ZERO=true : tout écart non nul produit un dossier (mode "trace systématique").
        //   - Sinon (défaut) : comportement V2-A historique → uniquement si feature AdvVarianceManagement + écart >= seuil de justification.
        // Les blocs ApprovalRequest / AnomalyCase restent gated sur le feature flag + seuils.
        var variance = session.VarianceAmount.GetValueOrDefault();
        var absVariance = Math.Abs(variance);
        if (absVariance > 0)
        {
            var trackAll = bool.TryParse(await _settings.GetRawAsync(SettingKeys.VarianceTrackAllNonZero, ct), out var ta) && ta;
            var advOn = await _features.IsEnabledAsync(FeatureCodes.AdvVarianceManagement, ct);

            var ci = System.Globalization.CultureInfo.InvariantCulture;
            decimal.TryParse(await _settings.GetRawAsync(SettingKeys.VarianceJustificationThreshold, ct),
                System.Globalization.NumberStyles.Number, ci, out var justifyThr);
            decimal.TryParse(await _settings.GetRawAsync(SettingKeys.VarianceApprovalThreshold, ct),
                System.Globalization.NumberStyles.Number, ci, out var approveThr);

            var shouldCreate = trackAll || (advOn && justifyThr > 0 && absVariance >= justifyThr);
            if (shouldCreate)
            {
                var register = await _db.CashRegisters.AsNoTracking().FirstAsync(r => r.Id == session.CashRegisterId, ct);
                var varianceCase = await _variance.CreateForSessionAsync(session.Id, variance, register.CurrencyCode, ct);
                await _db.SaveChangesAsync(ct); // persister le case pour avoir son Id

                if (justificationProvided)
                {
                    _db.VarianceJustifications.Add(new Domain.Entities.V2.VarianceJustification
                    {
                        VarianceCaseId = varianceCase.Id,
                        ProvidedBy = userId,
                        ProvidedAt = _clock.UtcNow,
                        Comment = dto.VarianceJustification!.Trim()
                    });
                    _db.VarianceActions.Add(new Domain.Entities.V2.VarianceAction
                    {
                        VarianceCaseId = varianceCase.Id,
                        ActionType = "JUSTIFY",
                        PerformedBy = userId,
                        PerformedAt = _clock.UtcNow,
                        Comment = dto.VarianceJustification!.Trim()
                    });
                    if (varianceCase.Status == VarianceStatus.OPEN) varianceCase.Status = VarianceStatus.JUSTIFIED;
                }

                if (advOn)
                {
                    if (approveThr > 0 && absVariance >= approveThr &&
                        await _features.IsEnabledAsync(FeatureCodes.AdvValidation, ct))
                    {
                        var match = await _approval.FindMatchingRuleAsync(ApprovalTargetType.VARIANCE_CASE, absVariance, register.CurrencyCode, ct);
                        if (match is not null)
                        {
                            var req = await _approval.CreateRequestAsync(match.ApprovalRuleId, ApprovalTargetType.VARIANCE_CASE,
                                nameof(Domain.Entities.V2.VarianceCase), varianceCase.Id, session.CashRegisterId,
                                absVariance, register.CurrencyCode,
                                $"Écart de session #{session.Id} : {variance} {register.CurrencyCode}", ct);
                            await _db.SaveChangesAsync(ct);
                            varianceCase.ApprovalRequestId = req.Id;
                        }
                    }

                    var autoAnomaly = bool.TryParse(await _settings.GetRawAsync(SettingKeys.AutoCreateAnomalyOnLargeVariance, ct), out var aa) && aa;
                    if (autoAnomaly && await _features.IsEnabledAsync(FeatureCodes.AdvAnomalies, ct))
                    {
                        await _db.SaveChangesAsync(ct);
                        var anomaly = await _anomaly.CreateAutoAsync(
                            $"Écart de caisse session #{session.Id} : {variance:N2}",
                            $"Détecté à la clôture. Théorique={theoretical:N2}, Physique={dto.PhysicalBalance:N2}.",
                            absVariance >= approveThr ? AnomalySeverity.HIGH : AnomalySeverity.MEDIUM,
                            nameof(CashSession), session.Id, session.CashRegisterId, session.Id, ct);
                        await _db.SaveChangesAsync(ct);
                        varianceCase.AnomalyCaseId = anomaly.Id;
                    }
                }
            }
        }

        await _audit.LogAsync(AuditAction.CLOSE, nameof(CashSession), session.Id,
            $"Session fermée — théo={theoretical:N2} phy={dto.PhysicalBalance:N2} écart={session.VarianceAmount:N2}",
            metadata: new { session.OpeningBalance, theoretical, session.PhysicalBalance, session.VarianceAmount }, ct: ct);
        await _db.SaveChangesAsync(ct);

        // Mode automatique : met la génération comptable en file d'attente, jamais en direct.
        // No-op silencieux si le module est désactivé ou le mode ON_CASH_CLOSING non configuré —
        // la clôture de caisse ne doit jamais échouer à cause de la comptabilité.
        if (await _features.IsEnabledAsync(FeatureCodes.AdvAccounting, ct))
        {
            await _accountingQueue.EnqueueAutomaticAsync(session.CashRegisterId, session.OpenedAt, session.ClosedAt!.Value, userId, ct);
        }

        return await GetAsync(id, ct);
    }

    public async Task<OpeningDefaultDto> GetOpeningDefaultAsync(int cashRegisterId, CancellationToken ct = default)
    {
        var mode = (await _settings.GetRawAsync(SettingKeys.OpeningBalanceDefaultMode, ct)) ?? "ZERO";
        if (!string.Equals(mode, "LAST_CLOSING_PHYSICAL", StringComparison.OrdinalIgnoreCase))
            return new OpeningDefaultDto(cashRegisterId, 0m, "ZERO");

        var lastClosed = await _db.CashSessions.AsNoTracking()
            .Where(s => s.CashRegisterId == cashRegisterId
                        && s.Status == CashSessionStatus.CLOSED
                        && s.PhysicalBalance != null)
            .OrderByDescending(s => s.ClosedAt)
            .Select(s => (decimal?)s.PhysicalBalance)
            .FirstOrDefaultAsync(ct);

        if (!lastClosed.HasValue)
            return new OpeningDefaultDto(cashRegisterId, 0m, "NO_PREVIOUS_SESSION");

        return new OpeningDefaultDto(cashRegisterId, lastClosed.Value, "LAST_CLOSING_PHYSICAL");
    }

    public async Task<SessionPendingItemsDto> GetPendingItemsAsync(int sessionId, CancellationToken ct = default)
    {
        var pendingOps = await _db.CashOperations.AsNoTracking()
            .Where(o => o.CashSessionId == sessionId && o.IsPendingApproval && !o.IsDeleted)
            .Select(o => o.OperationRef).ToListAsync(ct);

        var pendingTransfers = await _db.CashTransfers.AsNoTracking()
            .Where(t => (t.SourceSessionId == sessionId || t.DestinationSessionId == sessionId)
                        && (t.Status == CashTransferStatus.PENDING_APPROVAL || t.Status == CashTransferStatus.APPROVED))
            .Select(t => t.TransferRef).ToListAsync(ct);

        var pendingDeposits = await _db.BankDeposits.AsNoTracking()
            .Where(d => d.CashSessionId == sessionId
                        && (d.Status == BankDepositStatus.PENDING_APPROVAL || d.Status == BankDepositStatus.APPROVED))
            .Select(d => d.DepositRef).ToListAsync(ct);

        return new SessionPendingItemsDto(
            pendingOps.Count, pendingTransfers.Count, pendingDeposits.Count,
            pendingOps, pendingTransfers, pendingDeposits);
    }

    // Supprime les opérations PENDING_APPROVAL de la session + leurs ApprovalRequests associées.
    // Pour les transferts et dépôts pending, on les passe en CANCELLED + nettoyage des ApprovalRequests.
    private async Task DeletePendingItemsAsync(int sessionId, int userId, CancellationToken ct)
    {
        // 1) CashOperations PENDING_APPROVAL → soft delete + suppression des ApprovalRequests CASH_OPERATION / CASH_OPERATION_CANCEL.
        var pendingOps = await _db.CashOperations
            .Where(o => o.CashSessionId == sessionId && o.IsPendingApproval && !o.IsDeleted)
            .ToListAsync(ct);
        if (pendingOps.Count > 0)
        {
            var opIds = pendingOps.Select(o => o.Id).ToList();
            var opRequests = await _db.ApprovalRequests
                .Where(r => opIds.Contains(r.TargetEntityId)
                            && (r.TargetType == ApprovalTargetType.CASH_OPERATION
                                || r.TargetType == ApprovalTargetType.CASH_OPERATION_CANCEL))
                .ToListAsync(ct);
            _db.ApprovalRequests.RemoveRange(opRequests);
            foreach (var op in pendingOps)
            {
                op.IsPendingApproval = false;
                op.IsPendingCancellation = false;
                op.IsDeleted = true;
                op.DeletedBy = userId;
                op.DeletedAt = _clock.UtcNow;
                op.DeleteReason = "Supprimée à la clôture de la session (était en attente d'approbation).";
            }
        }

        // 2) CashTransfers pending → CANCELLED + suppression des ApprovalRequests CASH_TRANSFER.
        var pendingTransfers = await _db.CashTransfers
            .Where(t => (t.SourceSessionId == sessionId || t.DestinationSessionId == sessionId)
                        && (t.Status == CashTransferStatus.PENDING_APPROVAL || t.Status == CashTransferStatus.APPROVED))
            .ToListAsync(ct);
        if (pendingTransfers.Count > 0)
        {
            var ids = pendingTransfers.Select(t => t.Id).ToList();
            var reqs = await _db.ApprovalRequests
                .Where(r => r.TargetType == ApprovalTargetType.CASH_TRANSFER && ids.Contains(r.TargetEntityId))
                .ToListAsync(ct);
            _db.ApprovalRequests.RemoveRange(reqs);
            foreach (var t in pendingTransfers)
            {
                t.Status = CashTransferStatus.CANCELLED;
                t.CancelledAt = _clock.UtcNow;
            }
        }

        // 3) BankDeposits pending → CANCELLED + suppression des ApprovalRequests BANK_DEPOSIT.
        var pendingDeposits = await _db.BankDeposits
            .Where(d => d.CashSessionId == sessionId
                        && (d.Status == BankDepositStatus.PENDING_APPROVAL || d.Status == BankDepositStatus.APPROVED))
            .ToListAsync(ct);
        if (pendingDeposits.Count > 0)
        {
            var ids = pendingDeposits.Select(d => d.Id).ToList();
            var reqs = await _db.ApprovalRequests
                .Where(r => r.TargetType == ApprovalTargetType.BANK_DEPOSIT && ids.Contains(r.TargetEntityId))
                .ToListAsync(ct);
            _db.ApprovalRequests.RemoveRange(reqs);
            foreach (var d in pendingDeposits)
            {
                d.Status = BankDepositStatus.CANCELLED;
                d.CancelledAt = _clock.UtcNow;
            }
        }

        await _audit.LogAsync(AuditAction.UPDATE, nameof(CashSession), sessionId,
            $"Clôture : nettoyage des éléments pending — ops={pendingOps.Count}, transferts={pendingTransfers.Count}, dépôts={pendingDeposits.Count}.",
            ct: ct);
    }

    public async Task<decimal> RecomputeTheoreticalBalanceAsync(int sessionId, CancellationToken ct = default)
    {
        var session = await _db.CashSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct)
            ?? throw new NotFoundException(nameof(CashSession), sessionId);

        var theoretical = await ComputeTheoreticalBalanceAsync(session, ct);
        session.TheoreticalBalance = theoretical;
        await _db.SaveChangesAsync(ct);
        return theoretical;
    }

    private async Task<decimal> ComputeTheoreticalBalanceAsync(CashSession session, CancellationToken ct)
    {
        // Exclut les opérations en attente d'approbation : tant qu'une demande est en cours,
        // l'opération n'impacte pas le solde théorique. Une fois approuvée → IsPendingApproval=false → réintégrée.
        var ops = await _db.CashOperations
            .AsNoTracking()
            .Where(o => o.CashSessionId == session.Id && !o.IsPendingApproval)
            .Select(o => new { o.Direction, o.Amount })
            .ToListAsync(ct);

        var totalIn = ops.Where(o => o.Direction == OperationDirection.IN).Sum(o => o.Amount);
        var totalOut = ops.Where(o => o.Direction == OperationDirection.OUT).Sum(o => o.Amount);
        return session.OpeningBalance + totalIn - totalOut;
    }

    private async Task<CashSessionSummaryDto> BuildSummaryAsync(int sessionId, CancellationToken ct)
    {
        var ops = await _db.CashOperations.AsNoTracking()
            .Where(o => o.CashSessionId == sessionId)
            .Select(o => new { o.Direction, o.Amount })
            .ToListAsync(ct);

        var totalIn = ops.Where(o => o.Direction == OperationDirection.IN).Sum(o => o.Amount);
        var totalOut = ops.Where(o => o.Direction == OperationDirection.OUT).Sum(o => o.Amount);
        return new CashSessionSummaryDto(ops.Count, totalIn, totalOut, totalIn - totalOut);
    }

    private static CashSessionDetailDto Map(CashSession s, CashSessionSummaryDto summary) =>
        new(s.Id, s.CashRegisterId, s.CashRegister.Code, s.CashRegister.Name,
            s.OpenedBy, s.OpenedByUser.FullName, s.OpenedAt, s.OpeningBalance,
            s.ClosedBy, s.ClosedByUser?.FullName, s.ClosedAt,
            s.TheoreticalBalance, s.PhysicalBalance, s.VarianceAmount,
            s.Status, s.OpenComment, s.CloseComment, s.CreatedAt, s.UpdatedAt,
            summary);
}
