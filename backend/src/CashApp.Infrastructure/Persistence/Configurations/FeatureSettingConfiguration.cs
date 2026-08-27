using CashApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashApp.Infrastructure.Persistence.Configurations;

public class FeatureSettingConfiguration : IEntityTypeConfiguration<FeatureSetting>
{
    public void Configure(EntityTypeBuilder<FeatureSetting> b)
    {
        b.ToTable("feature_settings");
        b.HasKey(x => x.Id);

        b.Property(x => x.FeatureCode).HasMaxLength(60).IsRequired();
        b.HasIndex(x => x.FeatureCode).IsUnique();

        b.Property(x => x.FeatureName).HasMaxLength(150).IsRequired();
        b.Property(x => x.IsEnabled).IsRequired();
    }
}
