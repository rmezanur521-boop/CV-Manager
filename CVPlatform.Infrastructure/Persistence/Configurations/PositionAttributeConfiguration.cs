using CVPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CVPlatform.Infrastructure.Persistence.Configurations;

public class PositionAttributeConfiguration : IEntityTypeConfiguration<PositionAttribute>
{
    public void Configure(EntityTypeBuilder<PositionAttribute> builder)
    {
        builder.HasKey(pa => new { pa.PositionId, pa.AttributeId });

        builder.HasOne(pa => pa.Position)
            .WithMany(p => p.PositionAttributes)
            .HasForeignKey(pa => pa.PositionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pa => pa.Attribute)
            .WithMany()
            .HasForeignKey(pa => pa.AttributeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}