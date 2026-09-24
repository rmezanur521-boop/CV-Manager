using CVPlatform.Domain.Entities;
using CVPlatform.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CVPlatform.Infrastructure.Persistence.Configurations;

public class AttributeUsageConfiguration : IEntityTypeConfiguration<AttributeUsage>
{
    public void Configure(EntityTypeBuilder<AttributeUsage> builder)
    {
        builder.HasIndex(u => new { u.UserId, u.AttributeId }).IsUnique();

        builder.HasOne(u => u.Attribute)
            .WithMany()
            .HasForeignKey(u => u.AttributeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(u => u.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}