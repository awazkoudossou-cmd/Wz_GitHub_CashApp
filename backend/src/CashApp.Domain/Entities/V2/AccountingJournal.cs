using CashApp.Domain.Common;

namespace CashApp.Domain.Entities.V2;

public class AccountingJournal : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
