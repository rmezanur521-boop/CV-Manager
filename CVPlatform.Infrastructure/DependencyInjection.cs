using CVPlatform.Application.Attributes;
using CVPlatform.Application.Common;
using CVPlatform.Application.Crm;
using CVPlatform.Application.Cvs;
using CVPlatform.Application.Dashboard;
using CVPlatform.Application.Discussions;
using CVPlatform.Application.Positions;
using CVPlatform.Application.Profile;
using CVPlatform.Application.Projects;
using CVPlatform.Application.PublicSite;
using CVPlatform.Application.Search;
using CVPlatform.Application.Users;
using CVPlatform.Infrastructure.Identity;
using CVPlatform.Infrastructure.Persistence;
using CVPlatform.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CVPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        services.Configure<SmtpOptions>(configuration.GetSection("Smtp"));

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.SignIn.RequireConfirmedAccount = true;
        })
     .AddEntityFrameworkStores<ApplicationDbContext>()
     .AddDefaultTokenProviders();

        services.Configure<SecurityStampValidatorOptions>(options =>
        options.ValidationInterval = TimeSpan.FromMinutes(1));

        services.AddScoped<IAttributeService, AttributeService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<ICandidateAttributeValueService, CandidateAttributeValueService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IPositionService, PositionService>();
        services.AddScoped<IPositionAccessEvaluator, PositionAccessEvaluator>();
        services.AddScoped<ICvService, CvService>();
        services.AddScoped<IRecruiterCvService, RecruiterCvService>();
        services.AddScoped<IDiscussionService, DiscussionService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IRecruiterDashboardService, RecruiterDashboardService>();
        services.AddScoped<ICandidateDashboardService, CandidateDashboardService>();
        services.AddScoped<IPublicSiteService, PublicSiteService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IFileStorageService, MinioFileStorageService>();
        services.Configure<SalesforceOptions>(configuration.GetSection("Salesforce"));
        services.AddHttpClient<ISalesforceService, SalesforceService>();
        var smtpUsername = configuration["Smtp:Username"];
        var smtpConfigured = !string.IsNullOrWhiteSpace(smtpUsername) && !smtpUsername.StartsWith("PUT_");

        if (smtpConfigured)
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        else
            services.AddScoped<IEmailSender, ConsoleEmailSender>();
        return services;
    }
}