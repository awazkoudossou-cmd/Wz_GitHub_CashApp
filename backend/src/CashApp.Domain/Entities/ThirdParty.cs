using CashApp.Domain.Common;

namespace CashApp.Domain.Entities;

// Tiers pré-enregistré (client/fournisseur/bénéficiaire…) — alimenté automatiquement à chaque
// saisie d'un nouveau nom de tiers sur une opération, pour être ensuite sélectionnable.
public class ThirdParty : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
