using CVPlatform.Application.Dashboard;
using CVPlatform.Domain.Constants;
using CVPlatform.Domain.Enums;
using CVPlatform.Infrastructure.Identity;
using CVPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CVPlatform.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<DashboardDto> GetDashboardAsync()
    {
        var latestPositions = await _db.Positions
            .OrderByDescending(p => p.UpdatedAt)
            .Take(5)
            .Select(p => new LatestPositionDto(p.Id, p.Title, p.Company, p.CreatedAt))
            .ToListAsync();

        var popularPositions = await _db.Cvs
             .GroupBy(c => c.PositionId)
             .Select(g => new { PositionId = g.Key, CvCount = g.Count() })
             .OrderByDescending(x => x.CvCount)
             .Take(5)
             .Join(_db.Positions,
                 x => x.PositionId,
                 p => p.Id,
                 (x, p) => new PopularPositionDto(p.Id, p.Title, x.CvCount))
             .ToListAsync();

        var tagCloud = await _db.ProjectTags
            .GroupBy(pt => pt.TagId)
            .Select(g => new { TagId = g.Key, UsageCount = g.Count() })
            .OrderByDescending(x => x.UsageCount)
            .Take(30)
            .Join(_db.TechnologyTags,
                x => x.TagId,
                t => t.Id,
                (x, t) => new TagCloudItemDto(t.Name, x.UsageCount))
            .ToListAsync();

        var stats = await GetStatsAsync();

        return new DashboardDto(latestPositions, popularPositions, tagCloud, stats);
    }

    private async Task<DashboardStatsDto> GetStatsAsync()
    {
        var since = DateTime.UtcNow.AddHours(-24);

        var cvsLast24h = await _db.Cvs.CountAsync(c => c.CreatedAt >= since);
        var totalPositions = await _db.Positions.CountAsync();
        var totalCandidates = await _db.UserRoles
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .CountAsync(x => x.Name == Roles.Candidate);
        var totalRecruiters = await _db.UserRoles
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .CountAsync(x => x.Name == Roles.Recruiter);
        var totalSubmittedCvs = await _db.Cvs.CountAsync(c => c.Status == CvStatus.Published);

        return new DashboardStatsDto(cvsLast24h, totalPositions, totalCandidates, totalRecruiters, totalSubmittedCvs);
    }
}