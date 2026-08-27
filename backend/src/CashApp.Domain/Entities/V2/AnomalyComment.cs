using CashApp.Domain.Common;

namespace CashApp.Domain.Entities.V2;

public class AnomalyComment : BaseEntity
{
    public int AnomalyCaseId { get; set; }
    public AnomalyCase AnomalyCase { get; set; } = null!;

    public int AuthorId { get; set; }
    public User AuthorUser { get; set; } = null!;

    public string Body { get; set; } = string.Empty;
}
