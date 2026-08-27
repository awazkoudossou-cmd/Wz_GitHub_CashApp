using CashApp.Domain.Common;

namespace CashApp.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<UserCashRegister> UserCashRegisters { get; set; } = new List<UserCashRegister>();
}
