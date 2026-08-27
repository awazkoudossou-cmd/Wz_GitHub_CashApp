using CashApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashApp.Infrastructure.Persistence.Configurations;

public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> b)
    {
        b.ToTable("app_settings");
        b.HasKey(x => x.Id);

        b.Property(x => x.SettingKey).HasMaxLength(80).IsRequired();
        b.HasIndex(x => x.SettingKey).IsUnique();

        b.Property(x => x.SettingValue).HasMaxLength(1000);
    }
}
