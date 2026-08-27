namespace CashApp.Domain.Common;

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    int? DeletedBy { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeleteReason { get; set; }
}
