using CVPlatform.Application.Dashboard;
using CVPlatform.Domain.Enums;
using CVPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CVPlatform.Infrastructure.Services;

public class RecruiterDashboardService : IRecruiterDashboardService
{
    private readonly ApplicationDbContext _db;

    public RecruiterDashboardService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<RecruiterDashboardDto> GetDashboardAsync(string recruiterId)
    {
        var openPositions = await _db.Positions.AsNoTracking().CountAsync();
        var publishedCvs = await _db.Cvs.AsNoTracking().CountAsync(c => c.Status == CvStatus.Published);
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
        var newCvsLast7Days = await _db.Cvs.AsNoTracking().CountAsync(c => c.Status == CvStatus.Published && c.PublishedAt >= sevenDaysAgo);
        var shortlistedCount = await _db.CvLikes.AsNoTracking().CountAsync(l => l.RecruiterId == recruiterId);
        var draftCvs = await _db.Cvs.AsNoTracking().CountAsync(c => c.Status == CvStatus.Draft);
        var positionsWithZeroCvs = await _db.Positions.AsNoTracking().CountAsync(p => !_db.Cvs.Any(c => c.PositionId == p.Id));

        var kpis = new RecruiterKpisDto(
            openPositions,
            publishedCvs,
            newCvsLast7Days,
            shortlistedCount,
            draftCvs,
            positionsWithZeroCvs);

        var thirtyDaysAgo = DateTime.UtcNow.Date.AddDays(-29);
        var rawTrend = await _db.Cvs.AsNoTracking()
            .Where(c => c.Status == CvStatus.Published && c.PublishedAt.HasValue && c.PublishedAt.Value >= thirtyDaysAgo)
            .GroupBy(c => c.PublishedAt!.Value.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        var trendDict = rawTrend.ToDictionary(x => x.Date, x => x.Count);
        var trend = new List<CvTrendPointDto>();
        for (var i = 0; i < 30; i++)
        {
            var day = thirtyDaysAgo.AddDays(i);
            trendDict.TryGetValue(day, out var count);
            trend.Add(new CvTrendPointDto(day.ToString("dd MMM"), count));
        }

        var pipelineRaw = await _db.Positions.AsNoTracking()
            .Select(p => new
            {
                PositionId = p.Id,
                PositionTitle = p.Title,
                PublishedCount = _db.Cvs.Count(c => c.PositionId == p.Id && c.Status == CvStatus.Published),
                DraftCount = _db.Cvs.Count(c => c.PositionId == p.Id && c.Status == CvStatus.Draft),
                TotalCount = _db.Cvs.Count(c => c.PositionId == p.Id)
            })
            .OrderByDescending(x => x.TotalCount)
            .Take(6)
            .ToListAsync();

        var pipeline = pipelineRaw
            .Select(x => new PositionPipelineDto(x.PositionId, x.PositionTitle, x.PublishedCount, x.DraftCount, x.TotalCount))
            .ToList();

        var positionGroups = await _db.Positions.AsNoTracking()
            .GroupBy(p => p.Level)
            .Select(g => new { Level = g.Key, Count = g.Count() })
            .ToListAsync();

        var cvGroups = await _db.Cvs.AsNoTracking()
            .GroupBy(c => c.Position.Level)
            .Select(g => new { Level = g.Key, Count = g.Count() })
            .ToListAsync();

        var levelBreakdown = new List<LevelBreakdownDto>();
        foreach (var lvl in Enum.GetValues<PositionLevel>())
        {
            var posCount = positionGroups.FirstOrDefault(x => x.Level == lvl)?.Count ?? 0;
            var cvCount = cvGroups.FirstOrDefault(x => x.Level == lvl)?.Count ?? 0;
            levelBreakdown.Add(new LevelBreakdownDto(lvl.ToString(), posCount, cvCount));
        }

        var latestPublishedCvsRaw = await _db.Cvs.AsNoTracking()
            .Where(c => c.Status == CvStatus.Published && c.PublishedAt.HasValue)
            .OrderByDescending(c => c.PublishedAt)
            .Take(6)
            .Join(_db.Users,
                c => c.CandidateId,
                u => u.Id,
                (c, u) => new
                {
                    CvId = c.Id,
                    PositionId = c.PositionId,
                    PositionTitle = c.Position.Title,
                    CandidateId = c.CandidateId,
                    CandidateName = (u.FirstName + " " + u.LastName).Trim(),
                    PublishedAt = c.PublishedAt!.Value
                })
            .ToListAsync();

        var latestPublishedCvs = latestPublishedCvsRaw
            .Select(x => new LatestPublishedCvDto(
                x.CvId,
                x.PositionId,
                x.PositionTitle,
                x.CandidateId,
                string.IsNullOrWhiteSpace(x.CandidateName) ? "Candidate" : x.CandidateName,
                x.PublishedAt))
            .ToList();

        var shortlistRaw = await _db.CvLikes.AsNoTracking()
            .Where(l => l.RecruiterId == recruiterId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(6)
            .Join(_db.Cvs,
                l => l.CvId,
                c => c.Id,
                (l, c) => new { l, c })
            .Join(_db.Users,
                x => x.c.CandidateId,
                u => u.Id,
                (x, u) => new
                {
                    CvId = x.c.Id,
                    PositionId = x.c.PositionId,
                    PositionTitle = x.c.Position.Title,
                    CandidateId = x.c.CandidateId,
                    CandidateName = (u.FirstName + " " + u.LastName).Trim(),
                    LikedAt = x.l.CreatedAt
                })
            .ToListAsync();

        var shortlist = shortlistRaw
            .Select(x => new RecruiterShortlistItemDto(
                x.CvId,
                x.PositionId,
                x.PositionTitle,
                x.CandidateId,
                string.IsNullOrWhiteSpace(x.CandidateName) ? "Candidate" : x.CandidateName,
                x.LikedAt))
            .ToList();

        var topTags = await _db.ProjectTags.AsNoTracking()
            .GroupBy(pt => pt.TagId)
            .Select(g => new { TagId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(12)
            .Join(_db.TechnologyTags,
                x => x.TagId,
                t => t.Id,
                (x, t) => new TopTechnologyTagDto(t.Name, x.Count))
            .ToListAsync();

        var recentDiscussionsRaw = await _db.DiscussionPosts.AsNoTracking()
            .OrderByDescending(d => d.CreatedAt)
            .Take(5)
            .Select(d => new
            {
                PostId = d.Id,
                PositionId = d.PositionId,
                PositionTitle = d.Position.Title,
                AuthorId = d.AuthorId,
                Content = d.ContentMarkdown,
                CreatedAt = d.CreatedAt
            })
            .ToListAsync();

        var authorIds = recentDiscussionsRaw
            .Where(d => !string.IsNullOrEmpty(d.AuthorId))
            .Select(d => d.AuthorId!)
            .Distinct()
            .ToList();

        var authorMap = await _db.Users.AsNoTracking()
            .Where(u => authorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => (u.FirstName + " " + u.LastName).Trim());

        var recentDiscussions = recentDiscussionsRaw.Select(d =>
        {
            var authorName = !string.IsNullOrEmpty(d.AuthorId) && authorMap.TryGetValue(d.AuthorId, out var name) && !string.IsNullOrWhiteSpace(name)
                ? name
                : "Community Member";
            var snippet = d.Content.Length > 80 ? d.Content.Substring(0, 80) + "..." : d.Content;
            return new RecentDiscussionDto(d.PostId, d.PositionId, d.PositionTitle, authorName, snippet, d.CreatedAt);
        }).ToList();

        return new RecruiterDashboardDto(
            kpis,
            trend,
            pipeline,
            levelBreakdown,
            latestPublishedCvs,
            shortlist,
            topTags,
            recentDiscussions);
    }
}
