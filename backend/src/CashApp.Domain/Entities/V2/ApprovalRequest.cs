using CashApp.Domain.Common;
using CashApp.Domain.Enums;

namespace CashApp.Domain.Entities.V2;

// Une demande d'approbation concrète sur une entité métier.
public class ApprovalRequest : BaseEntity
{
    public string RequestRef { get; set; } = string.Empty;

    public int ApprovalRuleId { get; set; }
    public ApprovalRule ApprovalRule { get; set; } = null!;

    public ApprovalTargetType TargetType { get; set; }
    public string TargetEntityType { get; set; } = string.Empty;
    public int TargetEntityId { get; set; }

    public int? CashRegisterId { get; set; }
    public CashRegister? CashRegister { get; set; }

    public decimal? Amount { get; set; }
    public string? CurrencyCode { get; set; }

    public string Reason { get; set; } = string.Empty;

    public ApprovalStatus Status { get; set; } = ApprovalStatus.PENDING;

    public int RequestedBy { get; set; }
    public User RequestedByUser { get; set; } = null!;
    public DateTime RequestedAt { get; set; }

    public int? DecidedBy { get; set; }
    public User? DecidedByUser { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecisionComment { get; set; }

    public ICollection<ApprovalAction> Actions { get; set; } = new List<ApprovalAction>();
}
