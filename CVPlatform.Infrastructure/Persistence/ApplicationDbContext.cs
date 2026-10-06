using CVPlatform.Domain.Entities;
using CVPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CVPlatform.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<AttributeCategory> AttributeCategories => Set<AttributeCategory>();
    public DbSet<AttributeDefinition> Attributes => Set<AttributeDefinition>();
    public DbSet<AttributeOption> AttributeOptions => Set<AttributeOption>();
    public DbSet<CandidateAttributeValue> CandidateAttributeValues => Set<CandidateAttributeValue>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TechnologyTag> TechnologyTags => Set<TechnologyTag>();
    public DbSet<ProjectTag> ProjectTags => Set<ProjectTag>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<PositionAccessRule> PositionAccessRules => Set<PositionAccessRule>();
    public DbSet<PositionProjectFilter> PositionProjectFilters => Set<PositionProjectFilter>();
    public DbSet<CvLike> CvLikes => Set<CvLike>();
    public DbSet<Cv> Cvs => Set<Cv>();
    public DbSet<DiscussionPost> DiscussionPosts => Set<DiscussionPost>();
    public DbSet<PositionAttribute> PositionAttributes => Set<PositionAttribute>();
    public DbSet<AttributeUsage> AttributeUsages => Set<AttributeUsage>();
    public DbSet<FileAsset> FileAssets => Set<FileAsset>();
    public DbSet<CrmSyncRecord> CrmSyncRecords => Set<CrmSyncRecord>();
    public DbSet<PositionApiToken> PositionApiTokens => Set<PositionApiToken>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}