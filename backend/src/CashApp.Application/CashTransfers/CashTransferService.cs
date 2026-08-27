using CashApp.Application.Approvals;
using CashApp.Application.CashTransfers.Dtos;
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

namespace CashApp.Application.CashTransfers;

public class CashTransferService : ICashTransferService
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

    public CashTransferService(IAppDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock,
        ICashRegisterAccessService access, IApprovalService approval, ISettingsService settings,
        IAuditLogger audit, IReferenceGeneratorService refGen, ICategoryGroupService categoryGroups)
    {
        _db = db; _currentUser = currentUser; _clock = clock; _access = access;
        _approval = approval; _settings = settings; _audit = audit; _refGen = refGen;
        _categoryGroups = categoryGroups;
    }

    public async Task<PagedResponse<CashTransferListItemDto>> ListAsync(CashTransferFilterDto filter, CancellationToken ct = default)
    {
        var page = Math.Max(1, filter.Page);
        var size = Math.Clamp(filter.PageSize, 1, 200);

        var accessible = await _access.GetAccessibleRegisterIdsAsync(ct);

        var q = _db.CashTransfers.AsNoTracking()
            .Include(t => t.SourceCashRegister)
            .Include(t => t.DestinationCashRegister)
            .Include(t => t.RequestedByUser)
            .Where(t => accessible.Contains(t.SourceCashRegisterId) || accessible.Contains(t.DestinationCashRegisterId));

        if (filter.Status.HasValue) q = q.Where(t => t.Status == filter.Status.Value);
        if (filter.SourceCashRegisterId.HasValue) q = q.Where(t => t.SourceCashRegisterId == filter.SourceCashRegisterId.Value);
        if (filter.DestinationCashRegisterId.HasValue) q = q.Where(t => t.DestinationCashRegisterId == filter.DestinationCashRegisterId.Value);
        if (filter.From.HasValue) q = q.Where(t => t.TransferDate >= filter.From.Value);
        if (filter.To.HasValue) q = q.Where(t => t.TransferDate <= filter.To.Value);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(t => t.TransferDate)
            .Skip((page - 1) * size).Take(size)
            .Select(t => new CashTransferListItemDto(
                t.Id, t.TransferRef,
                t.SourceCashRegisterId, t.SourceCashRegister.Code,
                t.DestinationCashRegisterId, t.DestinationCashRegister.Code,
                t.Amount, t.CurrencyCode, t.TransferDate, t.Status,
                t.RequestedBy, t.RequestedByUser.FullName, t.CreatedAt))
            .ToListAsync(ct);

        return new PagedResponse<CashTransferListItemDto> { Items = items, Page = page, PageSize = size, TotalCount = total };
    }

    public async Task<CashTransferDetailDto> GetAsync(int id, CancellationToken ct = default)
    {
        var t = await LoadAsync(id, asNoTracking: true, ct) ?? throw new NotFoundException(nameof(CashTransfer), id);
        await _access.EnsureCanAccessAsync(t.SourceCashRegisterId, ct);
        return Map(t);
    }

    public async Task<CashTransferDetailDto> CreateAsync(CreateCashTransferDto dto, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException("Non authentifié.");

        if (dto.SourceCashRegisterId == dto.DestinationCashRegisterId)
            throw new BusinessRuleException("INVALID_TRANSFER", "Source et destination doivent être différentes.");
        if (dto.Amount <= 0)
            throw new BusinessRuleException("AMOUNT_INVALID", "Montant > 0 obligatoire.");

        await _access.EnsureCanAccessAsync(dto.SourceCashRegisterId, ct);

        var source = await _db.CashRegisters.FirstOrDefaultAsync(r => r.Id == dto.SourceCashRegisterId, ct)
            ?? throw new NotFoundException(nameof(CashRegister), dto.SourceCashRegisterId);
        var dest = await _db.CashRegisters.FirstOrDefaultAsync(r => r.Id == dto.DestinationCashRegisterId, ct)
            ?? throw new NotFoundException(nameof(CashRegister), dto.DestinationCashRegisterId);
        if (!source.IsActive) throw new BusinessRuleException("CASH_REGISTER_INACTIVE", "Caisse source inactive.");
        if (!dest.IsActive) throw new BusinessRuleException("CASH_REGISTER_INACTIVE", "Caisse destination inactive.");

        // Les deux caisses doivent avoir une session ouverte avant de pouvoir initier un transfert.
        var srcHasOpen = await _db.CashSessions.AnyAsync(s => s.CashRegisterId == source.Id && s.Status == CashSessionStatus.OPEN, ct);
        if (!srcHasOpen)
            throw new BusinessRuleException("SOURCE_SESSION_NOT_OPEN",
                $"La caisse source « {source.Code} — {source.Name} » n'a aucune session ouverte. Ouvrez une session avant d'effectuer le transfert.");
        var dstHasOpen = await _db.CashSessions.AnyAsync(s => s.CashRegisterId == dest.Id && s.Status == CashSessionStatus.OPEN, ct);
        if (!dstHasOpen)
            throw new BusinessRuleException("DEST_SESSION_NOT_OPEN",
                $"La caisse destination « {dest.Code} — {dest.Name} » n'a aucune session ouverte. Ouvrez une session avant d'effectuer le transfert.");

        var now = _clock.UtcNow;
        var stamp = now.ToString("yyyyMMddHHmmss");
        var entity = new CashTransfer
        {
            TransferRef = $"TR-{stamp}-{Random.Shared.Next(1000, 9999)}",
            SourceCashRegisterId = dto.SourceCashRegisterId,
            DestinationCashRegisterId = dto.DestinationCashRegisterId,
            Amount = dto.Amount,
            CurrencyCode = dto.CurrencyCode.Trim().ToUpperInvariant(),
            TransferDate = dto.TransferDate,
            Reason = dto.Reason.Trim(),
            RequestedBy = userId,
            Status = CashTransferStatus.DRAFT
        };
        _db.CashTransfers.Add(entity);
        await _db.SaveChangesAsync(ct);

        // Validation requise ?
        var transfersRequireApproval = bool.TryParse(
            await _settings.GetRawAsync(SettingKeys.ApprovalRequiredForTransfer, ct), out var b) && b;
        if (transfersRequireApproval)
        {
            var match = await _approval.FindMatchingRuleAsync(ApprovalTargetType.CASH_TRANSFER, dto.Amount, entity.CurrencyCode, ct);
            if (match is not null)
            {
                var request = await _approval.CreateRequestAsync(
                    match.ApprovalRuleId, ApprovalTargetType.CASH_TRANSFER,
                    nameof(CashTransfer), entity.Id,
                    entity.SourceCashRegisterId, entity.Amount, entity.CurrencyCode,
                    $"Transfert {entity.TransferRef} {entity.Amount} {entity.CurrencyCode}", ct);
                entity.Status = CashTransferStatus.PENDING_APPROVAL;
                // 1) On persiste la demande pour qu'elle reçoive son Id auto-généré.
                await _db.SaveChangesAsync(ct);
                // 2) On peut maintenant lier la demande au transfert (FK valide).
                entity.ApprovalRequestId = request.Id;
                await _db.SaveChangesAsync(ct);
            }
        }

        await _audit.LogAsync(AuditAction.CREATE, nameof(CashTransfer), entity.Id, $"Transfert {entity.TransferRef}", newValues: dto, ct: ct);
        await _db.SaveChangesAsync(ct);
        return await GetAsync(entity.Id, ct);
    }

    public async Task<CashTransferDetailDto> CompleteAsync(int id, CancellationToken ct = default)
    {
        var t = await LoadAsync(id, asNoTracking: false, ct) ?? throw new NotFoundException(nameof(CashTransfer), id);
        await _access.EnsureCanAccessAsync(t.SourceCashRegisterId, ct);

        // Doit être DRAFT (pas de validation requise) ou APPROVED
        if (t.Status == CashTransferStatus.PENDING_APPROVAL)
            throw new BusinessRuleException("TRANSFER_PENDING_APPROVAL", "En attente d'approbation.");
        if (t.Status is CashTransferStatus.COMPLETED or CashTransferStatus.REJECTED or CashTransferStatus.CANCELLED)
            throw new BusinessRuleException("TRANSFER_INVALID_STATUS", $"Statut '{t.Status}' incompatible.");

        // Synchroniser avec l'approbation si elle existe
        if (t.ApprovalRequestId.HasValue)
        {
            var req = await _db.ApprovalRequests.FirstOrDefaultAsync(r => r.Id == t.ApprovalRequestId.Value, ct);
            if (req is null || req.Status != ApprovalStatus.APPROVED)
                throw new BusinessRuleException("TRANSFER_NOT_APPROVED", "Approbation non encore validée.");
            if (t.Status == CashTransferStatus.PENDING_APPROVAL) t.Status = CashTransferStatus.APPROVED;
        }

        // Sessions ouvertes sur source et destination
        var srcSession = await _db.CashSessions.FirstOrDefaultAsync(s => s.CashRegisterId == t.SourceCashRegisterId && s.Status == CashSessionStatus.OPEN, ct)
            ?? throw new BusinessRuleException("SOURCE_SESSION_NOT_OPEN", "Aucune session ouverte sur la caisse source.");
        var dstSession = await _db.CashSessions.FirstOrDefaultAsync(s => s.CashRegisterId == t.DestinationCashRegisterId && s.Status == CashSessionStatus.OPEN, ct)
            ?? throw new BusinessRuleException("DEST_SESSION_NOT_OPEN", "Aucune session ouverte sur la caisse destination.");

        var now = _clock.UtcNow;
        var userId = _currentUser.UserId ?? throw new ForbiddenException("Non authentifié.");

        // Créer les opérations miroir (catégorie technique réutilisée si possible — sinon laissée libre pour l'utilisateur de mapper)
        var transferCategory = await _db.Categories.FirstOrDefaultAsync(c => c.Code == "TRANSFER", ct);
        if (transferCategory is null)
        {
            var group = await _categoryGroups.FindOrCreateAsync("Transferts internes", ct);
            transferCategory = new Category { Code = "TRANSFER", Label = "Transfert inter-caisses", Direction = OperationDirection.OUT, IsActive = true, GroupId = group.Id };
            _db.Categories.Add(transferCategory);
            await _db.SaveChangesAsync(ct);
        }
        var transferCategoryIn = await _db.Categories.FirstOrDefaultAsync(c => c.Code == "TRANSFER_IN", ct);
        if (transferCategoryIn is null)
        {
            var group = await _categoryGroups.FindOrCreateAsync("Transferts internes", ct);
            transferCategoryIn = new Category { Code = "TRANSFER_IN", Label = "Transfert reçu", Direction = OperationDirection.IN, IsActive = true, GroupId = group.Id };
            _db.Categories.Add(transferCategoryIn);
            await _db.SaveChangesAsync(ct);
        }

        // NB : on persiste l'opération source avant de générer la référence de la destination,
        // sinon le générateur (recherche globale) renvoie 2 fois la même référence (collision UNIQUE).
        var srcOp = new CashOperation
        {
            OperationRef = await _refGen.NextOperationRefAsync(t.SourceCashRegisterId, t.TransferDate, ct),
            CashRegisterId = t.SourceCashRegisterId,
            CashSessionId = srcSession.Id,
            OperationDate = t.TransferDate,
            Direction = OperationDirection.OUT,
            CategoryId = transferCategory.Id,
            Amount = t.Amount,
            CurrencyCode = t.CurrencyCode,
            PaymentMethod = PaymentMethod.CASH,
            Label = $"Transfert vers caisse #{t.DestinationCashRegisterId} ({t.TransferRef})",
            ExternalReference = t.TransferRef,
            CreatedBy = userId
        };
        _db.CashOperations.Add(srcOp);
        await _db.SaveChangesAsync(ct);

        var dstOp = new CashOperation
        {
            OperationRef = await _refGen.NextOperationRefAsync(t.DestinationCashRegisterId, t.TransferDate, ct),
            CashRegisterId = t.DestinationCashRegisterId,
            CashSessionId = dstSession.Id,
            OperationDate = t.TransferDate,
            Direction = OperationDirection.IN,
            CategoryId = transferCategoryIn.Id,
            Amount = t.Amount,
            CurrencyCode = t.CurrencyCode,
            PaymentMethod = PaymentMethod.CASH,
            Label = $"Transfert reçu depuis caisse #{t.SourceCashRegisterId} ({t.TransferRef})",
            ExternalReference = t.TransferRef,
            CreatedBy = userId
        };
        _db.CashOperations.Add(dstOp);
        await _db.SaveChangesAsync(ct);

        t.SourceOperationId = srcOp.Id;
        t.DestinationOperationId = dstOp.Id;
        t.SourceSessionId = srcSession.Id;
        t.DestinationSessionId = dstSession.Id;
        t.Status = CashTransferStatus.COMPLETED;
        t.CompletedAt = now;
        // Recalcule des soldes théoriques
        await RecomputeSessionAsync(srcSession, ct);
        await RecomputeSessionAsync(dstSession, ct);

        await _audit.LogAsync(AuditAction.COMPLETE, nameof(CashTransfer), t.Id, $"Transfert {t.TransferRef} finalisé",
            metadata: new { src = srcOp.OperationRef, dst = dstOp.OperationRef }, ct: ct);
        await _db.SaveChangesAsync(ct);
        return await GetAsync(t.Id, ct);
    }

    public async Task<CashTransferDetailDto> CancelAsync(int id, CancelCashTransferDto dto, CancellationToken ct = default)
    {
        var t = await LoadAsync(id, asNoTracking: false, ct) ?? throw new NotFoundException(nameof(CashTransfer), id);
        await _access.EnsureCanAccessAsync(t.SourceCashRegisterId, ct);
        if (t.Status is CashTransferStatus.COMPLETED or CashTransferStatus.CANCELLED)
            throw new BusinessRuleException("TRANSFER_INVALID_STATUS", $"Statut '{t.Status}' non annulable.");
        t.Status = CashTransferStatus.CANCELLED;
        t.CancelledAt = _clock.UtcNow;

        // Si une demande d'approbation PENDING est liée, on la passe en CANCELLED pour qu'elle
        // sorte de la file d'attente des Demandes d'approbation.
        var userId = _currentUser.UserId;
        var pendingReqs = await _db.ApprovalRequests
            .Where(r => r.TargetType == ApprovalTargetType.CASH_TRANSFER
                        && r.TargetEntityId == t.Id
                        && r.Status == ApprovalStatus.PENDING)
            .ToListAsync(ct);
        foreach (var r in pendingReqs)
        {
            r.Status = ApprovalStatus.CANCELLED;
            r.DecidedBy = userId;
            r.DecidedAt = _clock.UtcNow;
            r.DecisionComment = $"Transfert annulé : {dto.Reason}";
        }

        await _audit.LogAsync(AuditAction.CANCEL, nameof(CashTransfer), t.Id, dto.Reason, ct: ct);
        await _db.SaveChangesAsync(ct);
        return await GetAsync(t.Id, ct);
    }

    private async Task<CashTransfer?> LoadAsync(int id, bool asNoTracking, CancellationToken ct)
    {
        var q = _db.CashTransfers
            .Include(t => t.SourceCashRegister)
            .Include(t => t.DestinationCashRegister)
            .Include(t => t.RequestedByUser)
            .Include(t => t.ApprovedByUser)
            .Include(t => t.SourceOperation)
            .Include(t => t.DestinationOperation)
            .AsQueryable();
        if (asNoTracking) q = q.AsNoTracking();
        return await q.FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    private async Task RecomputeSessionAsync(CashSession session, CancellationToken ct)
    {
        // Exclure les ops en attente d'approbation, sinon le théorique est gonflé.
        var ops = await _db.CashOperations.AsNoTracking()
            .Where(o => o.CashSessionId == session.Id && !o.IsPendingApproval)
            .Select(o => new { o.Direction, o.Amount })
            .ToListAsync(ct);
        var totalIn = ops.Where(o => o.Direction == OperationDirection.IN).Sum(o => o.Amount);
        var totalOut = ops.Where(o => o.Direction == OperationDirection.OUT).Sum(o => o.Amount);
        session.TheoreticalBalance = session.OpeningBalance + totalIn - totalOut;
    }

    private static CashTransferDetailDto Map(CashTransfer t) =>
        new(t.Id, t.TransferRef,
            t.SourceCashRegisterId, t.SourceCashRegister.Code, t.SourceCashRegister.Name, t.SourceSessionId,
            t.DestinationCashRegisterId, t.DestinationCashRegister.Code, t.DestinationCashRegister.Name, t.DestinationSessionId,
            t.Amount, t.CurrencyCode, t.TransferDate, t.Reason, t.Status,
            t.RequestedBy, t.RequestedByUser.FullName,
            t.ApprovedBy, t.ApprovedByUser?.FullName, t.ApprovedAt,
            t.CompletedAt, t.CancelledAt,
            t.SourceOperationId, t.SourceOperation?.OperationRef,
            t.DestinationOperationId, t.DestinationOperation?.OperationRef,
            t.ApprovalRequestId, t.CreatedAt, t.UpdatedAt);
}
