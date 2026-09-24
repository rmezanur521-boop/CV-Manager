using CVPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CVPlatform.Infrastructure.Persistence.Configurations;

public class PositionProjectFilterConfiguration : IEntityTypeConfiguration<PositionProjectFilter>
{
    public void Configure(EntityTypeBuilder<PositionProjectFilter> builder)
    {
        builder.HasKey(f => f.PositionId);

        builder.HasOne(f => f.Position)
            .WithOne(p => p.ProjectFilter)
            .HasForeignKey<PositionProjectFilter>(f => f.PositionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}