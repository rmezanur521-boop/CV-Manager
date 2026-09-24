using CVPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CVPlatform.Infrastructure.Persistence.Configurations;

public class PositionAccessRuleConfiguration : IEntityTypeConfiguration<PositionAccessRule>
{
    public void Configure(EntityTypeBuilder<PositionAccessRule> builder)
    {
        builder.Property(r => r.ComparisonValue).HasMaxLength(500);

        builder.HasOne(r => r.Position)
            .WithMany(p => p.AccessRules)
            .HasForeignKey(r => r.PositionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Attribute)
            .WithMany()
            .HasForeignKey(r => r.AttributeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}