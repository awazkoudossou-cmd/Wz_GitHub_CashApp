using CashApp.Domain.Common;

namespace CashApp.Domain.Entities.V2;

// Opération comptable en attente : le moteur n'a pas pu la traiter (compte manquant, journal manquant...).
public class AccountingPending : BaseEntity
{
    public int CashOperationId { get; set; }
    public CashOperation CashOperation { get; set; } = null!;

    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }

    public bool Resolved { get; set; }
    public DateTime? ResolvedDate { get; set; }
}
