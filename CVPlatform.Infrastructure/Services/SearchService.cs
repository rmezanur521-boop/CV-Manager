using CVPlatform.Application.Positions;
using CVPlatform.Application.Search;
using CVPlatform.Domain.Enums;
using CVPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CVPlatform.Infrastructure.Services;

public class SearchService : ISearchService
{
    private readonly ApplicationDbContext _db;
    private readonly IPositionAccessEvaluator _accessEvaluator;

    public SearchService(ApplicationDbContext db, IPositionAccessEvaluator accessEvaluator)
    {
        _db = db;
        _accessEvaluator = accessEvaluator;
    }

    public async Task<IReadOnlyList<SearchResultDto>> SearchAsync(string userId, bool isRecruiterOrAdmin, string term)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            return new List<SearchResultDto>();

        var results = new List<SearchResultDto>();
        var ftsTerm = $"\"{term.Replace("\"", "\"\"")}*\"";

        var positions = await _db.Positions
            .FromSqlInterpolated($@"
                SELECT * FROM Positions
                WHERE CONTAINS((Title, ShortDescription), {ftsTerm})")
            .OrderByDescending(p => p.CreatedAt)
            .Take(10)
            .Select(p => new { p.Id, p.Title, p.Company })
            .ToListAsync();

        foreach (var position in positions)
        {
            results.Add(new SearchResultDto(
                SearchResultType.Position,
                position.Id,
                position.Title,
                position.Company ?? string.Empty,
                $"/Positions/Edit/{position.Id}"));
        }

        if (isRecruiterOrAdmin)
        {
            var matches = await _db.Cvs
                .Where(c => c.Status == CvStatus.Published)
                .Join(_db.Users, c => c.CandidateId, u => u.Id, (c, u) => new { Cv = c, Candidate = u })
                .Where(x => (x.Candidate.FirstName + " " + x.Candidate.LastName).Contains(term)
                            || x.Cv.Position.Title.Contains(term)
                            || _db.CandidateAttributeValues.Any(v => v.CandidateId == x.Candidate.Id && v.TextValue != null && v.TextValue.Contains(term))
                            || _db.Projects.Any(p => p.CandidateId == x.Candidate.Id && (p.Name.Contains(term) || p.DescriptionMarkdown.Contains(term))))
                .Take(10)
                .Select(x => new
                {
                    CvId = x.Cv.Id,
                    CandidateId = x.Candidate.Id,
                    CandidateName = x.Candidate.FirstName + " " + x.Candidate.LastName,
                    PositionId = x.Cv.PositionId,
                    PositionTitle = x.Cv.Position.Title
                })
                .ToListAsync();

            if (matches.Count > 0)
            {
                var candidatePairs = matches.Select(m => (m.CandidateId, m.PositionId)).ToList();
                var eligiblePairs = await _accessEvaluator.GetEligibleCandidatePositionPairsAsync(candidatePairs);

                var cvIds = matches.Select(m => m.CvId).ToList();
                var likeCounts = await _db.CvLikes
                    .Where(l => cvIds.Contains(l.CvId))
                    .GroupBy(l => l.CvId)
                    .Select(g => new { CvId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(g => g.CvId, g => g.Count);

                foreach (var match in matches)
                {
                    if (!eligiblePairs.Contains((match.CandidateId, match.PositionId)))
                        continue;

                    var likes = likeCounts.GetValueOrDefault(match.CvId, 0);
                    var subtitle = $"CV: {match.PositionTitle} • {likes} {(likes == 1 ? "like" : "likes")}";

                    results.Add(new SearchResultDto(
                        SearchResultType.Cv,
                        match.CvId,
                        match.CandidateName,
                        subtitle,
                        $"/RecruiterCvs/Index?q={Uri.EscapeDataString(match.CandidateName)}"));
                }
            }
        }

        var tags = await _db.TechnologyTags
            .Where(t => t.Name.Contains(term))
            .Take(5)
            .Select(t => t.Name)
            .ToListAsync();

        foreach (var tag in tags)
        {
            results.Add(new SearchResultDto(
                SearchResultType.Tag,
                0,
                tag,
                "Tag",
                isRecruiterOrAdmin
                    ? $"/RecruiterCvs/Index?q={Uri.EscapeDataString(tag)}"
                    : "/Positions/Index"));
        }

        return results;
    }
}