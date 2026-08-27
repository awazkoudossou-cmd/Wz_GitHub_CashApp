using CashApp.Domain.Common;
using CashApp.Domain.Enums;

namespace CashApp.Domain.Entities.V2;

public class AccountingAccount : BaseEntity
{
    public string AccountNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AccountingAccountNature Nature { get; set; }
    public bool IsActive { get; set; } = true;
}
