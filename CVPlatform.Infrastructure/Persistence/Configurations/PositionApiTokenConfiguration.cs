using CVPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CVPlatform.Infrastructure.Persistence.Configurations;

public class PositionApiTokenConfiguration : IEntityTypeConfiguration<PositionApiToken>
{
    public void Configure(EntityTypeBuilder<PositionApiToken> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.PositionId).IsRequired();
        builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(128);
        builder.Property(t => t.TokenPrefix).IsRequired().HasMaxLength(16);
        builder.Property(t => t.CreatedByUserId).IsRequired().HasMaxLength(450);

        builder.HasIndex(t => t.PositionId).IsUnique();

        builder.HasOne(t => t.Position)
            .WithMany()
            .HasForeignKey(t => t.PositionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
