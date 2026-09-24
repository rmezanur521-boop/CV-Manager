using CVPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CVPlatform.Infrastructure.Persistence.Configurations;

public class PositionProjectFilterTagConfiguration : IEntityTypeConfiguration<PositionProjectFilterTag>
{
    public void Configure(EntityTypeBuilder<PositionProjectFilterTag> builder)
    {
        builder.HasKey(t => new { t.PositionId, t.TagId });

        builder.HasOne(t => t.Filter)
            .WithMany(f => f.RequiredTags)
            .HasForeignKey(t => t.PositionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Tag)
            .WithMany()
            .HasForeignKey(t => t.TagId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}