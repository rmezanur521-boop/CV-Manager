using CVPlatform.Application.Dashboard;
using CVPlatform.Application.Positions;
using CVPlatform.Domain.Enums;
using CVPlatform.Infrastructure.Identity;
using CVPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CVPlatform.Infrastructure.Services;

public class CandidateDashboardService : ICandidateDashboardService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPositionAccessEvaluator _accessEvaluator;

    public CandidateDashboardService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IPositionAccessEvaluator accessEvaluator)
    {
        _db = db;
        _userManager = userManager;
        _accessEvaluator = accessEvaluator;
    }

    public async Task<CandidateDashboardDto> GetDashboardAsync(string candidateId)
    {
        var rawCvs = await _db.Cvs.AsNoTracking()
            .Where(c => c.CandidateId == candidateId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.PositionId,
                PositionTitle = c.Position.Title,
                c.Status,
                c.CreatedAt,
                c.PublishedAt
            })
            .ToListAsync();

        var positionIds = rawCvs.Select(c => c.PositionId).Distinct().ToList();
        var requiredPositionAttributes = await _db.PositionAttributes.AsNoTracking()
            .Where(pa => positionIds.Contains(pa.PositionId) && pa.IsRequired)
            .Select(pa => new { pa.PositionId, pa.AttributeId })
            .ToListAsync();

        var candidateFilledAttributeIds = await _db.CandidateAttributeValues.AsNoTracking()
            .Where(v => v.CandidateId == candidateId &&
                (!string.IsNullOrEmpty(v.TextValue) ||
                 v.NumericValue.HasValue ||
                 v.DateValue.HasValue ||
                 v.DateRangeStart.HasValue ||
                 v.BooleanValue.HasValue ||
                 v.SelectedOptionId.HasValue))
            .Select(v => v.AttributeId)
            .ToListAsync();

        var candidateFilledSet = candidateFilledAttributeIds.ToHashSet();

        var myCvs = rawCvs.Select(c =>
        {
            var missingReqCount = c.Status == CvStatus.Draft
                ? requiredPositionAttributes.Count(pa => pa.PositionId == c.PositionId && !candidateFilledSet.Contains(pa.AttributeId))
                : 0;

            return new CandidateCvStatusItemDto(
                c.Id,
                c.PositionId,
                c.PositionTitle,
                c.Status,
                c.CreatedAt,
                c.PublishedAt,
                missingReqCount);
        }).ToList();

        var totalCvs = myCvs.Count;
        var publishedCvs = myCvs.Count(c => c.Status == CvStatus.Published);
        var draftCvs = myCvs.Count(c => c.Status == CvStatus.Draft);
        var totalProjects = await _db.Projects.AsNoTracking().CountAsync(p => p.CandidateId == candidateId);

        var user = await _userManager.FindByIdAsync(candidateId);
        var missingItems = new List<MissingCompletenessItemDto>();
        var completenessScore = 0;

        var hasName = user != null && !string.IsNullOrWhiteSpace(user.FirstName) && !string.IsNullOrWhiteSpace(user.LastName);
        if (hasName) completenessScore += 20;
        else missingItems.Add(new MissingCompletenessItemDto("CandidateDashboard.MissingName", "Index", "Profile"));

        var hasLocation = user != null && !string.IsNullOrWhiteSpace(user.Location);
        if (hasLocation) completenessScore += 20;
        else missingItems.Add(new MissingCompletenessItemDto("CandidateDashboard.MissingLocation", "Index", "Profile"));

        var hasPhoto = user != null && !string.IsNullOrWhiteSpace(user.PhotoUrl);
        if (hasPhoto) completenessScore += 20;
        else missingItems.Add(new MissingCompletenessItemDto("CandidateDashboard.MissingPhoto", "Index", "Profile"));

        var hasAttributes = candidateFilledSet.Count > 0;
        if (hasAttributes) completenessScore += 20;
        else missingItems.Add(new MissingCompletenessItemDto("CandidateDashboard.MissingAttributes", "Info", "Profile"));

        if (totalProjects > 0) completenessScore += 20;
        else missingItems.Add(new MissingCompletenessItemDto("CandidateDashboard.MissingProjects", "Projects", "Profile"));

        var kpis = new CandidateKpisDto(
            totalCvs,
            publishedCvs,
            draftCvs,
            totalProjects,
            completenessScore);

        var existingCvPositionIds = rawCvs.Select(c => c.PositionId).ToHashSet();
        var candidateUnappliedPositions = await _db.Positions.AsNoTracking()
            .Where(p => !existingCvPositionIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Title, p.Company, p.Level })
            .ToListAsync();

        var eligiblePositionIds = await _accessEvaluator.GetEligiblePositionIdsAsync(
            candidateId, candidateUnappliedPositions.Select(p => p.Id).ToList());

        var availablePositions = candidateUnappliedPositions
            .Where(p => eligiblePositionIds.Contains(p.Id))
            .Take(6)
            .Select(p => new AvailablePositionToApplyDto(p.Id, p.Title, p.Company, p.Level))
            .ToList();

        var totalLikesReceived = await _db.CvLikes.AsNoTracking()
            .CountAsync(l => _db.Cvs.Any(c => c.Id == l.CvId && c.CandidateId == candidateId && c.Status == CvStatus.Published));

        var myTopTags = await _db.ProjectTags.AsNoTracking()
            .Where(pt => pt.Project.CandidateId == candidateId)
            .GroupBy(pt => pt.TagId)
            .Select(g => new { TagId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .Join(_db.TechnologyTags,
                x => x.TagId,
                t => t.Id,
                (x, t) => new TopTechnologyTagDto(t.Name, x.Count))
            .ToListAsync();

        var crmRecord = await _db.CrmSyncRecords.AsNoTracking()
            .Where(r => r.UserId == candidateId)
            .OrderByDescending(r => r.SyncedAt)
            .FirstOrDefaultAsync();

        var crmStatus = new CandidateCrmStatusDto(
            crmRecord != null,
            crmRecord?.SalesforceAccountId,
            crmRecord?.SalesforceContactId,
            crmRecord?.SyncedAt);

        return new CandidateDashboardDto(
            kpis,
            missingItems,
            myCvs,
            availablePositions,
            totalLikesReceived,
            myTopTags,
            crmStatus);
    }
}
