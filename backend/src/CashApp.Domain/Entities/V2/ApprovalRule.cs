using CashApp.Domain.Common;
using CashApp.Domain.Enums;

namespace CashApp.Domain.Entities.V2;

// Règle qui déclenche un workflow de validation pour un type d'action.
public class ApprovalRule : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ApprovalTargetType TargetType { get; set; }

    // Seuil de montant ; null = toute la cible déclenche la règle.
    public decimal? AmountThreshold { get; set; }
    public string? CurrencyCode { get; set; }

    // Rôle requis pour approuver (ADMIN/SUPERVISOR…).
    public string RequiredApproverRole { get; set; } = string.Empty;

    // Si true, l'action est bloquée tant qu'elle n'est pas approuvée.
    public bool IsBlocking { get; set; } = true;

    public bool IsActive { get; set; } = true;
}
