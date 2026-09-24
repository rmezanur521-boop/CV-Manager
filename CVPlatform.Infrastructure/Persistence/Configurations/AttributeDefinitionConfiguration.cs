using CVPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CVPlatform.Infrastructure.Persistence.Configurations;

public class AttributeDefinitionConfiguration : IEntityTypeConfiguration<AttributeDefinition>
{
    public void Configure(EntityTypeBuilder<AttributeDefinition> builder)
    {
        builder.HasIndex(a => a.Name).IsUnique();
        builder.Property(a => a.Name).IsRequired().HasMaxLength(150);
        builder.Property(a => a.Description).HasMaxLength(1000);
        builder.Property(a => a.Version).IsConcurrencyToken();

        builder.HasOne(a => a.Category)
            .WithMany(c => c.Attributes)
            .HasForeignKey(a => a.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.Options)
            .WithOne(o => o.Attribute)
            .HasForeignKey(o => o.AttributeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}