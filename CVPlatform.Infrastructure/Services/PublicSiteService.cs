using CVPlatform.Application.PublicSite;
using CVPlatform.Domain.Constants;
using CVPlatform.Domain.Enums;
using CVPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CVPlatform.Infrastructure.Services;

public class PublicSiteService : IPublicSiteService
{
    private readonly ApplicationDbContext _db;

    public PublicSiteService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PublicLandingDto> GetLandingPageDataAsync()
    {
        var openPositionsCount = await _db.Positions.AsNoTracking().CountAsync();

        var candidatesCount = await _db.UserRoles.AsNoTracking()
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .CountAsync(x => x.Name == Roles.Candidate);

        var publishedCvsCount = await _db.Cvs.AsNoTracking().CountAsync(c => c.Status == CvStatus.Published);

        var stats = new PublicStatsDto(openPositionsCount, candidatesCount, publishedCvsCount);

        var openPositions = await _db.Positions.AsNoTracking()
            .Where(p => p.AccessMode == AccessMode.Public)
            .OrderByDescending(p => p.CreatedAt)
            .Take(6)
            .Select(p => new PublicPositionTeaserDto(
                p.Id,
                p.Title,
                p.Company,
                p.Level,
                p.ShortDescription))
            .ToListAsync();

        return new PublicLandingDto(stats, openPositions);
    }
}
