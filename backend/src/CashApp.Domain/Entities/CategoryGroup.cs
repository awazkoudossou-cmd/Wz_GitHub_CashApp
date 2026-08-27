using CashApp.Domain.Common;

namespace CashApp.Domain.Entities;

// Groupe d'organisation des catégories (ex. « Ventes & Encaissements », « Achats & Dépenses »).
public class CategoryGroup : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
