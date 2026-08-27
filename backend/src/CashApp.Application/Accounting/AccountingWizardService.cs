using System.Text.RegularExpressions;
using CashApp.Application.Accounting.Dtos;
using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Domain.Entities;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.Accounting;

public class AccountingWizardService : IAccountingWizardService
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditLogger _audit;

    public AccountingWizardService(IAppDbContext db, IDateTimeProvider clock, IAuditLogger audit)
    {
        _db = db;
        _clock = clock;
        _audit = audit;
    }

    public async Task<IReadOnlyList<WizardCashRegisterDto>> ListCashRegistersAsync(CancellationToken ct = default)
    {
        return await _db.CashRegisters.AsNoTracking()
            .Include(r => r.AccountingJournal)
            .Include(r => r.AccountingAccount)
            .OrderBy(r => r.Code)
            .Select(r => new WizardCashRegisterDto(
                r.Id, r.Code, r.Name, r.IsActive,
                r.AccountingJournalId, r.AccountingJournal!.Code, r.AccountingJournal.Name,
                r.AccountingAccountId, r.AccountingAccount!.AccountNumber, r.AccountingAccount.Name))
            .ToListAsync(ct);
    }

    public async Task<WizardCashRegisterDto> AssignJournalAsync(int cashRegisterId, AssignCashRegisterJournalDto dto, CancellationToken ct = default)
    {
        var register = await _db.CashRegisters.FirstOrDefaultAsync(r => r.Id == cashRegisterId, ct)
            ?? throw new NotFoundException(nameof(CashRegister), cashRegisterId);
        var journal = await _db.AccountingJournals.FirstOrDefaultAsync(j => j.Id == dto.AccountingJournalId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.V2.AccountingJournal), dto.AccountingJournalId);

        register.AccountingJournalId = journal.Id;
        register.UpdatedAt = _clock.UtcNow;

        await _audit.LogAsync(AuditAction.CONFIG_CHANGE, nameof(CashRegister), register.Id,
            $"Journal '{journal.Code}' rattaché à la caisse {register.Code}", ct: ct);
        await _db.SaveChangesAsync(ct);
        return await GetCashRegisterDtoAsync(register.Id, ct);
    }

    public async Task<WizardCashRegisterDto> AssignAccountAsync(int cashRegisterId, AssignCashRegisterAccountDto dto, CancellationToken ct = default)
    {
        var register = await _db.CashRegisters.FirstOrDefaultAsync(r => r.Id == cashRegisterId, ct)
            ?? throw new NotFoundException(nameof(CashRegister), cashRegisterId);
        var account = await _db.AccountingAccounts.FirstOrDefaultAsync(a => a.Id == dto.AccountingAccountId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.V2.AccountingAccount), dto.AccountingAccountId);

        register.AccountingAccountId = account.Id;
        register.UpdatedAt = _clock.UtcNow;

        await _audit.LogAsync(AuditAction.CONFIG_CHANGE, nameof(CashRegister), register.Id,
            $"Compte '{account.AccountNumber}' rattaché à la caisse {register.Code}", ct: ct);
        await _db.SaveChangesAsync(ct);
        return await GetCashRegisterDtoAsync(register.Id, ct);
    }

    public async Task<IReadOnlyList<WizardCategoryDto>> ListCategoriesAsync(CancellationToken ct = default)
    {
        var usedCategoryIds = await _db.CashOperations.AsNoTracking()
            .Select(o => o.CategoryId).Distinct().ToListAsync(ct);
        var usedSet = usedCategoryIds.ToHashSet();

        return await _db.Categories.AsNoTracking()
            .Include(c => c.AccountingAccount)
            .OrderBy(c => c.Code)
            .Select(c => new WizardCategoryDto(
                c.Id, c.Code, c.Label, c.IsActive, usedSet.Contains(c.Id),
                c.AccountingAccountId, c.AccountingAccount!.AccountNumber, c.AccountingAccount.Name))
            .ToListAsync(ct);
    }

    public async Task<WizardCategoryDto> AssignCategoryAccountAsync(int categoryId, AssignCategoryAccountDto dto, CancellationToken ct = default)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId, ct)
            ?? throw new NotFoundException(nameof(Category), categoryId);
        var account = await _db.AccountingAccounts.FirstOrDefaultAsync(a => a.Id == dto.AccountingAccountId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.V2.AccountingAccount), dto.AccountingAccountId);

        category.AccountingAccountId = account.Id;
        category.UpdatedAt = _clock.UtcNow;

        await _audit.LogAsync(AuditAction.CONFIG_CHANGE, nameof(Category), category.Id,
            $"Compte '{account.AccountNumber}' rattaché à la catégorie {category.Code}", ct: ct);
        await _db.SaveChangesAsync(ct);

        var isUsed = await _db.CashOperations.AnyAsync(o => o.CategoryId == categoryId, ct);
        return new WizardCategoryDto(category.Id, category.Code, category.Label, category.IsActive, isUsed,
            category.AccountingAccountId, account.AccountNumber, account.Name);
    }

    public async Task<AccountingChecklistDto> GetChecklistAsync(CancellationToken ct = default)
    {
        var registers = await _db.CashRegisters.AsNoTracking().Where(r => r.IsActive).ToListAsync(ct);
        var missingJournal = registers.Count(r => r.AccountingJournalId is null);
        var missingAccount = registers.Count(r => r.AccountingAccountId is null);

        var usedCategoryIds = await _db.CashOperations.AsNoTracking().Select(o => o.CategoryId).Distinct().ToListAsync(ct);
        var usedCategories = await _db.Categories.AsNoTracking().Where(c => usedCategoryIds.Contains(c.Id)).ToListAsync(ct);
        var missingCategoryAccount = usedCategories.Count(c => c.AccountingAccountId is null);

        var settings = await _db.AccountingSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        var hasNarration = !string.IsNullOrWhiteSpace(settings?.NarrationTemplate);

        var items = new List<AccountingChecklistItemDto>
        {
            new("JOURNAL_CAISSE", "Journal caisse", missingJournal == 0,
                missingJournal == 0 ? null : $"{missingJournal} caisse(s) active(s) sans journal"),
            new("COMPTE_CAISSE", "Compte caisse", missingAccount == 0,
                missingAccount == 0 ? null : $"{missingAccount} caisse(s) active(s) sans compte"),
            new("COMPTE_CATEGORIE", "Compte catégorie", missingCategoryAccount == 0,
                missingCategoryAccount == 0 ? null : $"{missingCategoryAccount} catégorie(s) utilisée(s) sans compte"),
            new("TYPE_GENERATION", "Type génération", settings is not null, settings is not null ? null : "Non défini"),
            new("MODE_GENERATION", "Mode génération", settings is not null, settings is not null ? null : "Non défini"),
            new("LIBELLE", "Libellé", hasNarration, hasNarration ? null : "Modèle de libellé non défini")
        };

        return new AccountingChecklistDto(items, items.All(i => i.Ok));
    }

    public Task<AccountingPreviewResultDto> PreviewAsync(AccountingPreviewRequestDto dto, CancellationToken ct = default)
    {
        var sample = new Dictionary<string, string>
        {
            ["OperationNumber"] = "OP-20260101-0001",
            ["Category"] = "Vente",
            ["CashRegister"] = "CAISSE-01",
            ["Amount"] = "25 000",
            ["OperationDate"] = _clock.UtcNow.ToString("yyyy-MM-dd"),
            ["Reference"] = "REF-0001",
            ["Cashier"] = "Jean Dupont",
            ["SessionNumber"] = "#1",
            ["Journal"] = "VE"
        };

        var rendered = Regex.Replace(dto.Template ?? string.Empty, @"\{(\w+)\}",
            m => sample.TryGetValue(m.Groups[1].Value, out var v) ? v : m.Value);

        return Task.FromResult(new AccountingPreviewResultDto(rendered));
    }

    private async Task<WizardCashRegisterDto> GetCashRegisterDtoAsync(int id, CancellationToken ct)
    {
        return await _db.CashRegisters.AsNoTracking()
            .Include(r => r.AccountingJournal)
            .Include(r => r.AccountingAccount)
            .Where(r => r.Id == id)
            .Select(r => new WizardCashRegisterDto(
                r.Id, r.Code, r.Name, r.IsActive,
                r.AccountingJournalId, r.AccountingJournal!.Code, r.AccountingJournal.Name,
                r.AccountingAccountId, r.AccountingAccount!.AccountNumber, r.AccountingAccount.Name))
            .FirstAsync(ct);
    }
}
