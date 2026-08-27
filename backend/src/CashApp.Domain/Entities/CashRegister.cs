using CashApp.Domain.Common;
using CashApp.Domain.Entities.V2;
using CashApp.Domain.Enums;

namespace CashApp.Domain.Entities;

public class CashRegister : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CurrencyCode { get; set; } = "XOF";
    public bool IsActive { get; set; } = true;

    // Valeurs pré-remplies à la création d'une nouvelle opération sur cette caisse.
    public OperationDirection DefaultDirection { get; set; } = OperationDirection.IN;
    public PaymentMethod DefaultPaymentMethod { get; set; } = PaymentMethod.CASH;

    // Module Comptabilité (V3) : rattachement caisse -> journal / compte comptable. Toujours par FK.
    public int? AccountingJournalId { get; set; }
    public AccountingJournal? AccountingJournal { get; set; }
    public int? AccountingAccountId { get; set; }
    public AccountingAccount? AccountingAccount { get; set; }

    public ICollection<UserCashRegister> UserCashRegisters { get; set; } = new List<UserCashRegister>();
    public ICollection<CashSession> CashSessions { get; set; } = new List<CashSession>();
    public ICollection<CashOperation> CashOperations { get; set; } = new List<CashOperation>();
}
