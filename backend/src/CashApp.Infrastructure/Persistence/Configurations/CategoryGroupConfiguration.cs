using CashApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashApp.Infrastructure.Persistence.Configurations;

public class CategoryGroupConfiguration : IEntityTypeConfiguration<CategoryGroup>
{
    public void Configure(EntityTypeBuilder<CategoryGroup> b)
    {
        b.ToTable("category_groups");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).HasMaxLength(150).IsRequired().UseCollation("NOCASE");
        b.HasIndex(x => x.Name).IsUnique();
        b.Property(x => x.IsActive).IsRequired();
    }
}
