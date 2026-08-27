using CashApp.Application.Accounting;
using CashApp.Application.CashRegisters.Dtos;
using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.CashRegisters;

public class CashRegisterService : ICashRegisterService
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly IAccountingCashRegisterProvisioningService _accountingProvisioning;

    public CashRegisterService(IAppDbContext db, IDateTimeProvider clock, IAccountingCashRegisterProvisioningService accountingProvisioning)
    {
        _db = db;
        _clock = clock;
        _accountingProvisioning = accountingProvisioning;
    }

    public async Task<IReadOnlyList<CashRegisterListItemDto>> ListAsync(CancellationToken ct = default)
    {
        return await _db.CashRegisters
            .AsNoTracking()
            .OrderBy(c => c.Code)
            .Select(c => new CashRegisterListItemDto(c.Id, c.Code, c.Name, c.CurrencyCode, c.IsActive, c.CreatedAt,
                c.DefaultDirection, c.DefaultPaymentMethod))
            .ToListAsync(ct);
    }

    public async Task<CashRegisterDetailDto> GetAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.CashRegisters.AsNoTracking()
            .Include(c => c.AccountingAccount)
            .Include(c => c.AccountingJournal)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException(nameof(CashRegister), id);
        return Map(entity);
    }

    public async Task<CashRegisterDetailDto> CreateAsync(CreateCashRegisterDto dto, CancellationToken ct = default)
    {
        var code = dto.Code.Trim().ToUpperInvariant();
        if (await _db.CashRegisters.AnyAsync(c => c.Code == code, ct))
            throw new BusinessRuleException("CASH_REGISTER_CODE_EXISTS", $"Le code '{code}' est déjà utilisé.");

        var entity = new CashRegister
        {
            Code = code,
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            CurrencyCode = dto.CurrencyCode.Trim().ToUpperInvariant(),
            IsActive = true,
            DefaultDirection = dto.DefaultDirection,
            DefaultPaymentMethod = dto.DefaultPaymentMethod
        };

        // Transaction atomique : la caisse + le compte/journal comptable auto-générés (si configurés)
        // sont créés ensemble, ou pas du tout en cas d'erreur.
        var useTransaction = _db.Database.IsRelational();
        var tx = useTransaction ? await _db.Database.BeginTransactionAsync(ct) : null;
        try
        {
            _db.CashRegisters.Add(entity);
            await _db.SaveChangesAsync(ct);

            await _accountingProvisioning.ProvisionAsync(entity, ct);
            await _db.SaveChangesAsync(ct);

            if (tx is not null) await tx.CommitAsync(ct);
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

        return await GetAsync(entity.Id, ct);
    }

    public async Task<CashRegisterDetailDto> UpdateAsync(int id, UpdateCashRegisterDto dto, CancellationToken ct = default)
    {
        var entity = await _db.CashRegisters.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException(nameof(CashRegister), id);

        entity.Name = dto.Name.Trim();
        entity.Description = dto.Description?.Trim();
        entity.CurrencyCode = dto.CurrencyCode.Trim().ToUpperInvariant();
        entity.DefaultDirection = dto.DefaultDirection;
        entity.DefaultPaymentMethod = dto.DefaultPaymentMethod;
        entity.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task UpdateStatusAsync(int id, UpdateCashRegisterStatusDto dto, CancellationToken ct = default)
    {
        var entity = await _db.CashRegisters.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException(nameof(CashRegister), id);

        if (!dto.IsActive)
        {
            var openSession = await _db.CashSessions.AnyAsync(
                s => s.CashRegisterId == id && s.Status == Domain.Enums.CashSessionStatus.OPEN, ct);
            if (openSession)
                throw new BusinessRuleException("CASH_REGISTER_HAS_OPEN_SESSION",
                    "Impossible de désactiver une caisse ayant une session ouverte.");
        }

        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private static CashRegisterDetailDto Map(CashRegister c) =>
        new(c.Id, c.Code, c.Name, c.Description, c.CurrencyCode, c.IsActive, c.CreatedAt, c.UpdatedAt,
            c.DefaultDirection, c.DefaultPaymentMethod,
            c.AccountingAccountId, c.AccountingAccount?.AccountNumber,
            c.AccountingJournalId, c.AccountingJournal?.Code);
}
