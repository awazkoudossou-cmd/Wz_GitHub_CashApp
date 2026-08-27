using CashApp.Domain.Common;
using CashApp.Domain.Entities.V2;
using CashApp.Domain.Enums;

namespace CashApp.Domain.Entities;

public class Category : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public OperationDirection Direction { get; set; }
    public bool IsActive { get; set; } = true;

    // Groupe d'organisation (ex. « Ventes & Encaissements »). Nullable pour compat schéma
    // (colonne ajoutée après coup) mais toujours renseigné en pratique après le seed/backfill.
    public int? GroupId { get; set; }
    public CategoryGroup? Group { get; set; }

    // Module Comptabilité (V3) : rattachement catégorie -> compte comptable de contrepartie. Toujours par FK.
    public int? AccountingAccountId { get; set; }
    public AccountingAccount? AccountingAccount { get; set; }
}
