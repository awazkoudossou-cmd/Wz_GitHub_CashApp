using CashApp.Domain.Common;

namespace CashApp.Domain.Entities.V2;

public class AccountingEntry : BaseEntity
{
    public int GenerationId { get; set; }
    public AccountingGeneration Generation { get; set; } = null!;

    // Nullable : en mode CENTRALIZED, une écriture peut regrouper plusieurs opérations.
    public int? CashOperationId { get; set; }
    public CashOperation? CashOperation { get; set; }

    public int JournalId { get; set; }
    public AccountingJournal Journal { get; set; } = null!;

    public int AccountId { get; set; }
    public AccountingAccount Account { get; set; } = null!;

    public DateTime EntryDate { get; set; }
    public DateTime OperationDate { get; set; }

    public string Reference { get; set; } = string.Empty;
    public string? PieceNumber { get; set; }
    public string Description { get; set; } = string.Empty;

    // Seule modification autorisée en plus du libellé (Description) une fois l'écriture générée.
    public string? Comment { get; set; }

    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}
