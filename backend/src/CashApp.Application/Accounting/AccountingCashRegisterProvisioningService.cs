using CashApp.Application.Common.Interfaces;
using CashApp.Domain.Constants;
using CashApp.Domain.Entities;
using CashApp.Domain.Entities.V2;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.Accounting;

public interface IAccountingCashRegisterProvisioningService
{
    // Attribue automatiquement un compte comptable (Nature=CASH) et un journal à la caisse,
    // à partir de la numérotation automatique configurée dans les Paramètres Comptables.
    // Ne fait rien si le module Comptabilité n'est pas activé ou si la numérotation n'est pas configurée.
    Task ProvisionAsync(CashRegister cashRegister, CancellationToken ct = default);
}

public class AccountingCashRegisterProvisioningService : IAccountingCashRegisterProvisioningService
{
    private readonly IAppDbContext _db;
    private readonly IFeatureService _features;
    private readonly IAuditLogger _audit;

    public AccountingCashRegisterProvisioningService(IAppDbContext db, IFeatureService features, IAuditLogger audit)
    {
        _db = db;
        _features = features;
        _audit = audit;
    }

    public async Task ProvisionAsync(CashRegister cashRegister, CancellationToken ct = default)
    {
        if (!await _features.IsEnabledAsync(FeatureCodes.AdvAccounting, ct)) return;

        var settings = await _db.AccountingSettings.FirstOrDefaultAsync(ct);
        if (settings is null) return;
        if (string.IsNullOrWhiteSpace(settings.CashAccountRootNumber) || string.IsNullOrWhiteSpace(settings.CashJournalRootCode))
            return;

        var accountNumber = await NextAccountNumberAsync(settings, ct);
        var account = await _db.AccountingAccounts.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber, ct);
        if (account is null)
        {
            account = new AccountingAccount
            {
                AccountNumber = accountNumber,
                Name = cashRegister.Name,
                Nature = AccountingAccountNature.CASH,
                IsActive = true
            };
            _db.AccountingAccounts.Add(account);
        }

        var journalCode = await NextJournalCodeAsync(settings, ct);
        var journal = await _db.AccountingJournals.FirstOrDefaultAsync(j => j.Code == journalCode, ct);
        if (journal is null)
        {
            journal = new AccountingJournal
            {
                Code = journalCode,
                Name = $"{journalCode} - {cashRegister.Name}",
                IsActive = true
            };
            _db.AccountingJournals.Add(journal);
        }

        await _db.SaveChangesAsync(ct);

        cashRegister.AccountingAccountId = account.Id;
        cashRegister.AccountingJournalId = journal.Id;

        await _audit.LogAsync(AuditAction.CREATE, nameof(CashRegister), cashRegister.Id,
            $"Numérotation comptable automatique : compte {account.AccountNumber}, journal {journal.Code}", ct: ct);
    }

    private async Task<string> NextAccountNumberAsync(Domain.Entities.V2.AccountingSettings settings, CancellationToken ct)
    {
        var root = settings.CashAccountRootNumber!.Trim();
        var totalLength = settings.CashAccountNumberLength ?? (root.Length + 2);
        var suffixWidth = Math.Max(1, totalLength - root.Length);

        string candidate;
        do
        {
            settings.LastCashAccountSequence++;
            candidate = root + settings.LastCashAccountSequence.ToString("D" + suffixWidth);
        } while (await _db.AccountingAccounts.AnyAsync(a => a.AccountNumber == candidate, ct));

        return candidate;
    }

    private async Task<string> NextJournalCodeAsync(Domain.Entities.V2.AccountingSettings settings, CancellationToken ct)
    {
        var root = settings.CashJournalRootCode!.Trim().ToUpperInvariant();

        string candidate;
        do
        {
            settings.LastCashJournalSequence++;
            candidate = $"{root}{settings.LastCashJournalSequence:D3}";
        } while (await _db.AccountingJournals.AnyAsync(j => j.Code == candidate, ct));

        return candidate;
    }
}
