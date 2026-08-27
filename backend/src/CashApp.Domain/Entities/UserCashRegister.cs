namespace CashApp.Domain.Entities;

public class UserCashRegister
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int CashRegisterId { get; set; }
    public CashRegister CashRegister { get; set; } = null!;

    public DateTime AssignedAt { get; set; }
}
