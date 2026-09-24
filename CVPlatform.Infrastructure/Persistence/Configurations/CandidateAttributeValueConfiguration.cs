using CVPlatform.Domain.Entities;
using CVPlatform.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CVPlatform.Infrastructure.Persistence.Configurations;

public class CandidateAttributeValueConfiguration : IEntityTypeConfiguration<CandidateAttributeValue>
{
    public void Configure(EntityTypeBuilder<CandidateAttributeValue> builder)
    {
        builder.HasIndex(v => new { v.CandidateId, v.AttributeId }).IsUnique();
        builder.Property(v => v.Version).IsConcurrencyToken();

        builder.HasOne(v => v.Attribute)
            .WithMany()
            .HasForeignKey(v => v.AttributeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.SelectedOption)
            .WithMany()
            .HasForeignKey(v => v.SelectedOptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(v => v.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}