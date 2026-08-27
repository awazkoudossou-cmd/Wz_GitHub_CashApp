using CashApp.Domain.Common;

namespace CashApp.Domain.Entities;

public class FeatureSetting : BaseEntity
{
    public string FeatureCode { get; set; } = string.Empty;
    public string FeatureName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public int? UpdatedBy { get; set; }
}
