using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using CashApp.Application.Accounting.Dtos;
using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Domain.Entities;
using CashApp.Domain.Entities.V2;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.Accounting;

// Moteur de génération des écritures comptables. Lit les CashOperation, ne les modifie jamais,
// produit des AccountingEntry. C'est le SEUL endroit qui écrit dans AccountingEntry — aucun
// controller n'appelle directement les DbSet.
public class AccountingGenerationEngineService : IAccountingGenerationEngineService
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _audit;
    private readonly IAccountingGenerationService _generationReader;

    public AccountingGenerationEngineService(
        IAppDbContext db, IDateTimeProvider clock, ICurrentUserService currentUser,
        IAuditLogger audit, IAccountingGenerationService generationReader)
    {
        _db = db;
        _clock = clock;
        _currentUser = currentUser;
        _audit = audit;
        _generationReader = generationReader;
    }

    public async Task<AccountingGenerationPreviewDto> PreviewAsync(GenerateAccountingEntriesDto dto, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var result = await BuildAsync(dto.StartDate, dto.EndDate, dto.CashRegisterIds, ct);

        // Classification supplémentaire pour l'aperçu (lecture seule — BuildAsync ne persiste jamais).
        // IgnoreQueryFilters() : le filtre global exclut IsDeleted par défaut, mais on doit justement
        // voir les opérations annulées ici pour les compter dans "ignorées".
        var rangeQuery = _db.CashOperations.AsNoTracking().IgnoreQueryFilters()
            .Where(o => o.OperationDate >= dto.StartDate && o.OperationDate <= dto.EndDate);
        if (dto.CashRegisterIds is { Count: > 0 })
            rangeQuery = rangeQuery.Where(o => dto.CashRegisterIds.Contains(o.CashRegisterId));

        var allOpsInRange = await rangeQuery
            .Select(o => new { o.Id, o.IsDeleted, o.IsPendingApproval, SessionStatus = o.CashSession.Status })
            .ToListAsync(ct);
        var allIds = allOpsInRange.Select(o => o.Id).ToList();
        var alreadyAccountedIds = allIds.Count == 0
            ? new HashSet<int>()
            : (await _db.AccountingEntries.AsNoTracking()
                .Where(e => e.CashOperationId != null && allIds.Contains(e.CashOperationId!.Value))
                .Select(e => e.CashOperationId!.Value).Distinct().ToListAsync(ct)).ToHashSet();

        var alreadyAccountedCount = allOpsInRange.Count(o => alreadyAccountedIds.Contains(o.Id));
        // Ignorées : annulées/rejetées (IsDeleted), en attente de validation (IsPendingApproval),
        // ou dont la session de caisse n'est pas encore clôturée (seules les opérations d'une
        // session clôturée peuvent être comptabilisées) — et pas déjà comptabilisées.
        var ignoredCount = allOpsInRange.Count(o => !alreadyAccountedIds.Contains(o.Id) &&
            (o.IsDeleted || o.IsPendingApproval || o.SessionStatus != CashSessionStatus.CLOSED));

        sw.Stop();
        return new AccountingGenerationPreviewDto(
            result.OperationCount, result.Entries.Count, result.Pendings.Count, (int)sw.ElapsedMilliseconds,
            alreadyAccountedCount, ignoredCount);
    }

    public async Task<AccountingGenerationDetailDto> GenerateAsync(GenerateAccountingEntriesDto dto, CancellationToken ct = default, int? actingUserId = null)
    {
        var sw = Stopwatch.StartNew();
        var result = await BuildAsync(dto.StartDate, dto.EndDate, dto.CashRegisterIds, ct);
        var userId = actingUserId ?? _currentUser.UserId ?? throw new ForbiddenException("Non authentifié.");
        var now = _clock.UtcNow;

        var useTransaction = _db.Database.IsRelational();
        var tx = useTransaction ? await _db.Database.BeginTransactionAsync(ct) : null;
        try
        {
            var batch = new AccountingGeneration
            {
                Reference = await NextBatchNumberAsync(now, ct),
                GenerationType = result.GenerationType,
                GenerationMode = result.GenerationMode,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Status = AccountingGenerationStatus.GENERATED,
                GeneratedBy = userId,
                GeneratedAt = now,
                TotalOperations = result.OperationCount,
                TotalEntries = result.Entries.Count,
                Remarks = result.Pendings.Count > 0 ? $"{result.Pendings.Count} opération(s) placée(s) en attente." : null
            };
            _db.AccountingGenerations.Add(batch);
            await _db.SaveChangesAsync(ct);

            foreach (var entry in result.Entries) entry.GenerationId = batch.Id;
            _db.AccountingEntries.AddRange(result.Entries);
            _db.AccountingPendings.AddRange(result.Pendings);
            await _db.SaveChangesAsync(ct);

            sw.Stop();
            _db.AccountingGenerationLogs.Add(new AccountingGenerationLog
            {
                GenerationId = batch.Id,
                PerformedBy = userId,
                PerformedAt = now,
                OperationCount = result.OperationCount,
                EntryCount = result.Entries.Count,
                ProcessingTimeMs = (int)sw.ElapsedMilliseconds,
                ErrorsJson = result.Pendings.Count > 0
                    ? JsonSerializer.Serialize(result.Pendings.Select(p => p.Reason))
                    : null
            });

            await _audit.LogAsync(AuditAction.CREATE, nameof(AccountingGeneration), batch.Id,
                $"Génération {batch.Reference} : {result.Entries.Count} écriture(s) pour {result.OperationCount} opération(s), {result.Pendings.Count} en attente",
                ct: ct);
            await _db.SaveChangesAsync(ct);

            if (tx is not null) await tx.CommitAsync(ct);
            return await _generationReader.GetAsync(batch.Id, ct);
        }
        catch
        {
            if (tx is not null) await tx.RollbackAsync(ct);
            throw;
        }
        finally
        {
            if (tx is not null) await tx.DisposeAsync();
        }
    }

    public async Task<AccountingGenerationDetailDto> GeneratePendingAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var settings = await _db.AccountingSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        if (settings is null || !settings.IsConfigured)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["accounting"] = new[] { "Le moteur comptable n'est pas configuré (voir Paramètres comptables)." }
            });

        var pendings = await _db.AccountingPendings
            .Where(p => !p.Resolved)
            .Include(p => p.CashOperation).ThenInclude(o => o.CashRegister).ThenInclude(r => r.AccountingJournal)
            .Include(p => p.CashOperation).ThenInclude(o => o.CashRegister).ThenInclude(r => r.AccountingAccount)
            .Include(p => p.CashOperation).ThenInclude(o => o.Category).ThenInclude(c => c.AccountingAccount)
            .ToListAsync(ct);

        if (pendings.Count == 0)
            throw new BusinessRuleException("NO_PENDING", "Aucune opération comptable en attente à retraiter.");

        var alreadyEntered = (await _db.AccountingEntries.AsNoTracking()
                .Where(e => e.CashOperationId != null)
                .Select(e => e.CashOperationId!.Value)
                .Distinct().ToListAsync(ct))
            .ToHashSet();

        var userNames = await LoadUserNamesAsync(pendings.Select(p => p.CashOperation.CreatedBy), ct);
        var now = _clock.UtcNow;
        var narrationTemplate = settings.NarrationTemplate ?? string.Empty;

        var newEntries = new List<AccountingEntry>();
        var centralGroups = new Dictionary<(int JournalId, int AccountId, OperationDirection Direction), (decimal Amount, int Count)>();
        var resolvedCount = 0;
        DateTime? minDate = null, maxDate = null;

        foreach (var pending in pendings)
        {
            var op = pending.CashOperation;

            if (op.IsDeleted)
            {
                pending.Resolved = true;
                pending.ResolvedDate = now;
                pending.Reason = "Opération annulée entre-temps — ignorée.";
                resolvedCount++;
                continue;
            }
            if (op.IsPendingApproval) continue; // toujours en attente d'approbation métier, on ne touche à rien
            if (alreadyEntered.Contains(op.Id))
            {
                pending.Resolved = true;
                pending.ResolvedDate = now;
                pending.Reason = "Déjà comptabilisée entre-temps.";
                resolvedCount++;
                continue;
            }

            var processed = ProcessOperation(op, settings.GenerationType, narrationTemplate, userNames, now);
            if (!processed.Success)
            {
                pending.Reason = processed.FailureReason!;
                continue;
            }

            newEntries.AddRange(processed.DetailEntries);
            if (processed.CentralContribution is { } c)
            {
                var key = (c.JournalId, c.AccountId, c.Direction);
                centralGroups[key] = centralGroups.TryGetValue(key, out var acc)
                    ? (acc.Amount + c.Amount, acc.Count + 1)
                    : (c.Amount, 1);
            }

            pending.Resolved = true;
            pending.ResolvedDate = now;
            resolvedCount++;
            minDate = minDate is null || op.OperationDate < minDate ? op.OperationDate : minDate;
            maxDate = maxDate is null || op.OperationDate > maxDate ? op.OperationDate : maxDate;
        }

        if (settings.GenerationType == AccountingGenerationType.CENTRALIZED)
        {
            foreach (var (key, agg) in centralGroups)
                newEntries.Add(MakeCentralizedEntry(key.JournalId, key.AccountId, key.Direction, agg.Amount, agg.Count, now, now));
        }

        if (resolvedCount == 0)
            throw new BusinessRuleException("NO_PENDING_RESOLVED", "Aucune opération en attente n'a pu être retraitée avec la configuration actuelle.");

        var userId = _currentUser.UserId ?? throw new ForbiddenException("Non authentifié.");
        var useTransaction = _db.Database.IsRelational();
        var tx = useTransaction ? await _db.Database.BeginTransactionAsync(ct) : null;
        try
        {
            var batch = new AccountingGeneration
            {
                Reference = await NextBatchNumberAsync(now, ct),
                GenerationType = settings.GenerationType,
                GenerationMode = settings.GenerationMode,
                StartDate = minDate ?? now,
                EndDate = maxDate ?? now,
                Status = AccountingGenerationStatus.GENERATED,
                GeneratedBy = userId,
                GeneratedAt = now,
                TotalOperations = resolvedCount,
                TotalEntries = newEntries.Count,
                Remarks = $"Relance : {resolvedCount}/{pendings.Count} opération(s) en attente retraitée(s)."
            };
            _db.AccountingGenerations.Add(batch);
            await _db.SaveChangesAsync(ct);

            foreach (var entry in newEntries) entry.GenerationId = batch.Id;
            _db.AccountingEntries.AddRange(newEntries);
            await _db.SaveChangesAsync(ct);

            sw.Stop();
            _db.AccountingGenerationLogs.Add(new AccountingGenerationLog
            {
                GenerationId = batch.Id,
                PerformedBy = userId,
                PerformedAt = now,
                OperationCount = resolvedCount,
                EntryCount = newEntries.Count,
                ProcessingTimeMs = (int)sw.ElapsedMilliseconds,
                ErrorsJson = null
            });

            await _audit.LogAsync(AuditAction.CREATE, nameof(AccountingGeneration), batch.Id,
                $"Relance {batch.Reference} : {resolvedCount}/{pendings.Count} en attente retraitée(s)", ct: ct);
            await _db.SaveChangesAsync(ct);

            if (tx is not null) await tx.CommitAsync(ct);
            return await _generationReader.GetAsync(batch.Id, ct);
        }
        catch
        {
            if (tx is not null) await tx.RollbackAsync(ct);
            throw;
        }
        finally
        {
            if (tx is not null) await tx.DisposeAsync();
        }
    }

    public async Task<AccountingGenerationDetailDto> RegenerateAsync(int batchId, CancellationToken ct = default)
    {
        var batch = await _db.AccountingGenerations.FirstOrDefaultAsync(g => g.Id == batchId, ct)
            ?? throw new NotFoundException(nameof(AccountingGeneration), batchId);
        if (batch.Exported)
            throw new BusinessRuleException("BATCH_EXPORTED", "Ce batch a été exporté et ne peut plus être régénéré.");

        var sw = Stopwatch.StartNew();
        var useTransaction = _db.Database.IsRelational();
        var tx = useTransaction ? await _db.Database.BeginTransactionAsync(ct) : null;
        try
        {
            // Supprime les écritures (et logs) du batch — jamais les CashOperation.
            var oldEntries = await _db.AccountingEntries.Where(e => e.GenerationId == batchId).ToListAsync(ct);
            _db.AccountingEntries.RemoveRange(oldEntries);
            var oldLogs = await _db.AccountingGenerationLogs.Where(l => l.GenerationId == batchId).ToListAsync(ct);
            _db.AccountingGenerationLogs.RemoveRange(oldLogs);
            await _db.SaveChangesAsync(ct);

            var result = await BuildAsync(batch.StartDate, batch.EndDate, null, ct);
            var userId = _currentUser.UserId ?? throw new ForbiddenException("Non authentifié.");
            var now = _clock.UtcNow;

            foreach (var entry in result.Entries) entry.GenerationId = batch.Id;
            _db.AccountingEntries.AddRange(result.Entries);
            _db.AccountingPendings.AddRange(result.Pendings);

            batch.GenerationType = result.GenerationType;
            batch.GenerationMode = result.GenerationMode;
            batch.Status = AccountingGenerationStatus.GENERATED;
            batch.GeneratedAt = now;
            batch.TotalOperations = result.OperationCount;
            batch.TotalEntries = result.Entries.Count;
            batch.Remarks = result.Pendings.Count > 0 ? $"Régénéré : {result.Pendings.Count} opération(s) en attente." : "Régénéré.";
            await _db.SaveChangesAsync(ct);

            sw.Stop();
            _db.AccountingGenerationLogs.Add(new AccountingGenerationLog
            {
                GenerationId = batch.Id,
                PerformedBy = userId,
                PerformedAt = now,
                OperationCount = result.OperationCount,
                EntryCount = result.Entries.Count,
                ProcessingTimeMs = (int)sw.ElapsedMilliseconds,
                ErrorsJson = result.Pendings.Count > 0 ? JsonSerializer.Serialize(result.Pendings.Select(p => p.Reason)) : null
            });

            await _audit.LogAsync(AuditAction.UPDATE, nameof(AccountingGeneration), batch.Id,
                $"Batch {batch.Reference} régénéré : {result.Entries.Count} écriture(s), {result.Pendings.Count} en attente", ct: ct);
            await _db.SaveChangesAsync(ct);

            if (tx is not null) await tx.CommitAsync(ct);
            return await _generationReader.GetAsync(batch.Id, ct);
        }
        catch
        {
            if (tx is not null) await tx.RollbackAsync(ct);
            throw;
        }
        finally
        {
            if (tx is not null) await tx.DisposeAsync();
        }
    }

    // === Cœur du moteur : lecture + calcul, sans écriture DB (réutilisé par Preview et Generate) ===

    private async Task<GenerationResult> BuildAsync(DateTime start, DateTime end, IReadOnlyList<int>? registerIds, CancellationToken ct)
    {
        var preflightErrors = new List<string>();
        if (start > end) preflightErrors.Add("La date de début doit être antérieure ou égale à la date de fin.");

        var settings = await _db.AccountingSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        if (settings is null || !settings.IsConfigured)
            preflightErrors.Add("Le moteur comptable n'est pas configuré (voir Paramètres comptables).");

        if (preflightErrors.Count > 0)
            throw new ValidationException(new Dictionary<string, string[]> { ["accounting"] = preflightErrors.ToArray() });

        var alreadyEntered = (await _db.AccountingEntries.AsNoTracking()
                .Where(e => e.CashOperationId != null)
                .Select(e => e.CashOperationId!.Value)
                .Distinct().ToListAsync(ct))
            .ToHashSet();
        var alreadyPending = (await _db.AccountingPendings.AsNoTracking()
                .Where(p => !p.Resolved)
                .Select(p => p.CashOperationId)
                .ToListAsync(ct))
            .ToHashSet();

        var q = _db.CashOperations.AsNoTracking()
            .Include(o => o.CashRegister).ThenInclude(r => r.AccountingJournal)
            .Include(o => o.CashRegister).ThenInclude(r => r.AccountingAccount)
            .Include(o => o.Category).ThenInclude(c => c.AccountingAccount)
            // "POSTED" : ni annulée/rejetée (soft delete), ni en attente d'approbation, et la session
            // de caisse d'origine doit être clôturée (une opération d'une session encore ouverte n'est
            // jamais éligible — elle deviendra candidate dès la clôture de sa session).
            .Where(o => !o.IsDeleted && !o.IsPendingApproval)
            .Where(o => o.CashSession.Status == CashSessionStatus.CLOSED)
            .Where(o => o.OperationDate >= start && o.OperationDate <= end);
        if (registerIds is { Count: > 0 })
            q = q.Where(o => registerIds.Contains(o.CashRegisterId));

        var candidates = await q
            .Where(o => !alreadyEntered.Contains(o.Id))
            .OrderBy(o => o.OperationDate).ThenBy(o => o.Id)
            .ToListAsync(ct);

        var userNames = await LoadUserNamesAsync(candidates.Select(o => o.CreatedBy), ct);
        var narrationTemplate = settings!.NarrationTemplate ?? string.Empty;
        var now = _clock.UtcNow;

        var entries = new List<AccountingEntry>();
        var pendings = new List<AccountingPending>();
        var centralGroups = new Dictionary<(int JournalId, int AccountId, OperationDirection Direction), (decimal Amount, int Count)>();

        foreach (var op in candidates)
        {
            if (alreadyPending.Contains(op.Id)) continue; // déjà en attente : sera traité par la relance, pas ici.

            var processed = ProcessOperation(op, settings.GenerationType, narrationTemplate, userNames, now);
            if (!processed.Success)
            {
                pendings.Add(new AccountingPending
                {
                    CashOperationId = op.Id,
                    Reason = processed.FailureReason!,
                    CreatedDate = now,
                    Resolved = false
                });
                continue;
            }

            entries.AddRange(processed.DetailEntries);
            if (processed.CentralContribution is { } c)
            {
                var key = (c.JournalId, c.AccountId, c.Direction);
                centralGroups[key] = centralGroups.TryGetValue(key, out var acc)
                    ? (acc.Amount + c.Amount, acc.Count + 1)
                    : (c.Amount, 1);
            }
        }

        if (settings.GenerationType == AccountingGenerationType.CENTRALIZED)
        {
            foreach (var (key, agg) in centralGroups)
                entries.Add(MakeCentralizedEntry(key.JournalId, key.AccountId, key.Direction, agg.Amount, agg.Count, now, end));
        }

        return new GenerationResult(candidates.Count, entries, pendings, settings.GenerationType, settings.GenerationMode);
    }

    // Traite une opération individuelle : vérifie la config, construit les lignes de détail
    // (toujours créées, y compris en mode centralisé — traçabilité par opération) et, en mode
    // centralisé, retourne en plus la contribution à agréger côté caisse.
    private static OpProcessResult ProcessOperation(
        CashOperation op, AccountingGenerationType genType, string narrationTemplate,
        IReadOnlyDictionary<int, string> userNames, DateTime now)
    {
        var register = op.CashRegister;
        var reasons = new List<string>();
        if (register.AccountingJournalId is null || register.AccountingJournal is null) reasons.Add("Journal absent sur la caisse");
        if (register.AccountingAccountId is null || register.AccountingAccount is null) reasons.Add("Compte caisse absent");
        if (op.Category.AccountingAccountId is null || op.Category.AccountingAccount is null) reasons.Add("Compte catégorie absent");

        if (reasons.Count > 0)
            return new OpProcessResult(false, string.Join(" ; ", reasons), new List<AccountingEntry>(), null);

        var journal = register.AccountingJournal!;
        var cashAccount = register.AccountingAccount!;
        var categoryAccount = op.Category.AccountingAccount!;
        var cashierName = op.CreatedBy.HasValue && userNames.TryGetValue(op.CreatedBy.Value, out var n) ? n : string.Empty;
        var description = RenderNarration(narrationTemplate, op, journal, cashierName);
        var isIn = op.Direction == OperationDirection.IN;

        var lines = new List<AccountingEntry>();
        (int JournalId, int AccountId, OperationDirection Direction, decimal Amount)? central = null;

        if (genType == AccountingGenerationType.DETAILED)
        {
            if (isIn)
            {
                lines.Add(MakeEntry(op, journal, cashAccount, description, op.Amount, 0, now));
                lines.Add(MakeEntry(op, journal, categoryAccount, description, 0, op.Amount, now));
            }
            else
            {
                lines.Add(MakeEntry(op, journal, categoryAccount, description, op.Amount, 0, now));
                lines.Add(MakeEntry(op, journal, cashAccount, description, 0, op.Amount, now));
            }
        }
        else
        {
            // Ligne individuelle côté catégorie (garde la traçabilité par opération) ; la
            // contrepartie caisse est agrégée par (journal, compte caisse, sens) par l'appelant.
            lines.Add(isIn
                ? MakeEntry(op, journal, categoryAccount, description, 0, op.Amount, now)
                : MakeEntry(op, journal, categoryAccount, description, op.Amount, 0, now));
            central = (journal.Id, cashAccount.Id, op.Direction, op.Amount);
        }

        return new OpProcessResult(true, null, lines, central);
    }

    private static AccountingEntry MakeEntry(CashOperation op, AccountingJournal journal, AccountingAccount account,
        string description, decimal debit, decimal credit, DateTime now) => new()
    {
        CashOperationId = op.Id,
        JournalId = journal.Id,
        AccountId = account.Id,
        EntryDate = now,
        OperationDate = op.OperationDate,
        Reference = op.OperationRef,
        PieceNumber = op.OperationRef,
        Description = description,
        Debit = debit,
        Credit = credit
    };

    private static AccountingEntry MakeCentralizedEntry(int journalId, int accountId, OperationDirection direction,
        decimal amount, int count, DateTime entryDate, DateTime operationDate)
    {
        // IN  : la caisse encaisse -> ligne caisse au Débit.
        // OUT : la caisse décaisse -> ligne caisse au Crédit.
        var isDebit = direction == OperationDirection.IN;
        return new AccountingEntry
        {
            CashOperationId = null,
            JournalId = journalId,
            AccountId = accountId,
            EntryDate = entryDate,
            OperationDate = operationDate,
            Reference = "SYNTHESE",
            PieceNumber = null,
            Description = $"Synthèse centralisée — {count} opération(s)",
            Debit = isDebit ? amount : 0,
            Credit = isDebit ? 0 : amount
        };
    }

    private static string RenderNarration(string template, CashOperation op, AccountingJournal journal, string cashierName)
    {
        if (string.IsNullOrEmpty(template)) return string.Empty;
        var map = new Dictionary<string, string>
        {
            ["OperationNumber"] = op.OperationRef,
            ["Category"] = op.Category.Label,
            ["CashRegister"] = op.CashRegister.Code,
            ["Amount"] = op.Amount.ToString("N2"),
            ["OperationDate"] = op.OperationDate.ToString("yyyy-MM-dd"),
            ["Reference"] = op.ExternalReference ?? op.OperationRef,
            ["Cashier"] = cashierName,
            ["SessionNumber"] = $"#{op.CashSessionId}",
            ["Journal"] = journal.Code
        };
        return Regex.Replace(template, @"\{(\w+)\}", m => map.TryGetValue(m.Groups[1].Value, out var v) ? v : m.Value);
    }

    private async Task<Dictionary<int, string>> LoadUserNamesAsync(IEnumerable<int?> createdByIds, CancellationToken ct)
    {
        var ids = createdByIds.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, string>();
        return await _db.Users.AsNoTracking().Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
    }

    private async Task<string> NextBatchNumberAsync(DateTime now, CancellationToken ct)
    {
        var datePart = now.ToString("yyyyMMdd");
        var prefix = $"ACC-{datePart}-";
        var existing = await _db.AccountingGenerations.AsNoTracking()
            .Where(g => g.Reference.StartsWith(prefix))
            .Select(g => g.Reference)
            .ToListAsync(ct);
        var next = existing
            .Select(r => int.TryParse(r[prefix.Length..], out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;
        return $"{prefix}{next:D5}";
    }

    private sealed record GenerationResult(
        int OperationCount,
        List<AccountingEntry> Entries,
        List<AccountingPending> Pendings,
        AccountingGenerationType GenerationType,
        AccountingGenerationMode GenerationMode);

    private sealed record OpProcessResult(
        bool Success,
        string? FailureReason,
        List<AccountingEntry> DetailEntries,
        (int JournalId, int AccountId, OperationDirection Direction, decimal Amount)? CentralContribution);
}
