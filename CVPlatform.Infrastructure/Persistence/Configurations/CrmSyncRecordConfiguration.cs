using CVPlatform.Domain.Entities;
using CVPlatform.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CVPlatform.Infrastructure.Persistence.Configurations;

public class CrmSyncRecordConfiguration : IEntityTypeConfiguration<CrmSyncRecord>
{
    public void Configure(EntityTypeBuilder<CrmSyncRecord> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.UserId)
            .IsRequired();

        builder.Property(r => r.SalesforceAccountId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.SalesforceContactId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Company)
            .HasMaxLength(200);

        builder.Property(r => r.JobTitle)
            .HasMaxLength(200);

        builder.Property(r => r.Phone)
            .HasMaxLength(50);

        builder.HasIndex(r => r.UserId);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
