using CashApp.Domain.Common;

namespace CashApp.Domain.Entities;

public class AppSetting : BaseEntity
{
    public string SettingKey { get; set; } = string.Empty;
    public string? SettingValue { get; set; }
    public int? UpdatedBy { get; set; }
}
