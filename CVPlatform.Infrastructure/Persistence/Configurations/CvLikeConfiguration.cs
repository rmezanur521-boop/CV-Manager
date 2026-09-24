using CVPlatform.Domain.Entities;
using CVPlatform.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CVPlatform.Infrastructure.Persistence.Configurations;

public class CvLikeConfiguration : IEntityTypeConfiguration<CvLike>
{
    public void Configure(EntityTypeBuilder<CvLike> builder)
    {
        builder.HasIndex(l => new { l.CvId, l.RecruiterId }).IsUnique();

        builder.HasOne(l => l.Cv)
            .WithMany()
            .HasForeignKey(l => l.CvId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(l => l.RecruiterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}