using CVPlatform.Application.Common.Exceptions;
using CVPlatform.Application.Cvs;
using CVPlatform.Application.Positions;
using CVPlatform.Domain.Entities;
using CVPlatform.Domain.Enums;
using CVPlatform.Infrastructure.Identity;
using CVPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CVPlatform.Infrastructure.Services;

public class RecruiterCvService : IRecruiterCvService
{
    private readonly ApplicationDbContext _db;
    private readonly IPositionAccessEvaluator _accessEvaluator;

    public RecruiterCvService(ApplicationDbContext db, IPositionAccessEvaluator accessEvaluator)
    {
        _db = db;
        _accessEvaluator = accessEvaluator;
    }

    private sealed record MatchedCv(
        int CvId,
        string CandidateId,
        string CandidateName,
        int PositionId,
        string PositionTitle,
        CvStatus Status,
        DateTime? PublishedAt);

    public async Task<CvSearchResultDto> SearchAsync(string recruiterId, CvSearchRequest request)
    {
        var query = _db.Cvs
            .AsNoTracking()
            .Where(c => c.Status == CvStatus.Published)
            .Join(_db.Users, c => c.CandidateId, u => u.Id, (c, u) => new { Cv = c, Candidate = u })
            .Join(_db.Positions, x => x.Cv.PositionId, p => p.Id, (x, p) => new { x.Cv, x.Candidate, Position = p });

        if (request.PositionId.HasValue)
            query = query.Where(x => x.Position.Id == request.PositionId.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var text = request.SearchText;
            query = query.Where(x =>
                (x.Candidate.FirstName + " " + x.Candidate.LastName).Contains(text) ||
                x.Position.Title.Contains(text));
        }

        var matches = await query
            .Select(x => new MatchedCv(
                x.Cv.Id,
                x.Candidate.Id,
                x.Candidate.FirstName + " " + x.Candidate.LastName,
                x.Position.Id,
                x.Position.Title,
                x.Cv.Status,
                x.Cv.PublishedAt))
            .ToListAsync();

        var candidatePositionPairs = matches.Select(m => (m.CandidateId, m.PositionId)).ToList();
        var eligiblePairs = await _accessEvaluator.GetEligibleCandidatePositionPairsAsync(candidatePositionPairs);

        var eligible = matches
            .Where(m => eligiblePairs.Contains((m.CandidateId, m.PositionId)))
            .ToList();

        var eligibleCvIds = eligible.Select(e => e.CvId).ToList();

        var likeCounts = eligibleCvIds.Count == 0
            ? new Dictionary<int, int>()
            : await _db.CvLikes
                .AsNoTracking()
                .Where(l => eligibleCvIds.Contains(l.CvId))
                .GroupBy(l => l.CvId)
                .Select(g => new { CvId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.CvId, g => g.Count);

        var myLikes = eligibleCvIds.Count == 0
            ? new HashSet<int>()
            : (await _db.CvLikes
                .AsNoTracking()
                .Where(l => l.RecruiterId == recruiterId && eligibleCvIds.Contains(l.CvId))
                .Select(l => l.CvId)
                .ToListAsync()).ToHashSet();

        IEnumerable<MatchedCv> sorted = request.SortBy switch
        {
            "CandidateName" => request.SortDescending
                ? eligible.OrderByDescending(x => x.CandidateName)
                : eligible.OrderBy(x => x.CandidateName),
            "PositionTitle" => request.SortDescending
                ? eligible.OrderByDescending(x => x.PositionTitle)
                : eligible.OrderBy(x => x.PositionTitle),
            "LikesCount" => request.SortDescending
                ? eligible.OrderByDescending(x => likeCounts.GetValueOrDefault(x.CvId, 0))
                : eligible.OrderBy(x => likeCounts.GetValueOrDefault(x.CvId, 0)),
            _ => request.SortDescending
                ? eligible.OrderByDescending(x => x.PublishedAt)
                : eligible.OrderBy(x => x.PublishedAt)
        };

        var totalCount = eligible.Count;
        var pagedMatches = sorted
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        IReadOnlyList<PositionAttributeColumnDto> columns = Array.Empty<PositionAttributeColumnDto>();
        var valuesByCandidate = new Dictionary<string, Dictionary<int, AttributeColumnValueDto>>();

        if (request.PositionId.HasValue && pagedMatches.Count > 0)
        {
            var positionAttributes = await _db.PositionAttributes
                .AsNoTracking()
                .Where(pa => pa.PositionId == request.PositionId.Value)
                .Include(pa => pa.Attribute)
                .OrderBy(pa => pa.Attribute.Name)
                .ToListAsync();

            columns = positionAttributes.Select(pa => new PositionAttributeColumnDto(pa.AttributeId, pa.Attribute.Name)).ToList();

            var attributeIds = positionAttributes.Select(pa => pa.AttributeId).ToList();
            var pagedCandidateIds = pagedMatches.Select(e => e.CandidateId).Distinct().ToList();

            var candidateValues = await _db.CandidateAttributeValues
                .AsNoTracking()
                .Include(v => v.SelectedOption)
                .Where(v => pagedCandidateIds.Contains(v.CandidateId) && attributeIds.Contains(v.AttributeId))
                .ToListAsync();

            foreach (var candidateId in pagedCandidateIds)
            {
                var map = new Dictionary<int, AttributeColumnValueDto>();
                foreach (var pa in positionAttributes)
                {
                    var value = candidateValues.FirstOrDefault(v => v.CandidateId == candidateId && v.AttributeId == pa.AttributeId);
                    map[pa.AttributeId] = FormatValue(pa.AttributeId, pa.Attribute.Type, value);
                }
                valuesByCandidate[candidateId] = map;
            }
        }

        var items = pagedMatches.Select(e => new RecruiterCvListItemDto(
            e.CvId,
            e.CandidateId,
            e.CandidateName,
            e.PositionId,
            e.PositionTitle,
            e.Status,
            e.PublishedAt,
            likeCounts.GetValueOrDefault(e.CvId, 0),
            myLikes.Contains(e.CvId),
            valuesByCandidate.TryGetValue(e.CandidateId, out var vals)
                ? vals
                : new Dictionary<int, AttributeColumnValueDto>())).ToList();

        return new CvSearchResultDto(columns, new PagedResult<RecruiterCvListItemDto>(items, totalCount, request.Page, request.PageSize));
    }

    public async Task<int> ToggleLikeAsync(string recruiterId, int cvId)
    {
        var cv = await _db.Cvs.FirstOrDefaultAsync(c => c.Id == cvId && c.Status == CvStatus.Published)
            ?? throw new NotFoundException("CV not found.");

        var existing = await _db.CvLikes.FirstOrDefaultAsync(l => l.CvId == cvId && l.RecruiterId == recruiterId);

        if (existing is not null)
        {
            _db.CvLikes.Remove(existing);
        }
        else
        {
            _db.CvLikes.Add(new CvLike { CvId = cvId, RecruiterId = recruiterId, CreatedAt = DateTime.UtcNow });
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
        }

        return await _db.CvLikes.CountAsync(l => l.CvId == cvId);
    }

    public async Task<RecruiterCvHeaderDto> GetHeaderAsync(string recruiterId, int cvId)
    {
        var cv = await _db.Cvs
            .Include(c => c.Position)
            .FirstOrDefaultAsync(c => c.Id == cvId && c.Status == CvStatus.Published)
            ?? throw new NotFoundException("CV not found.");

        // Same visibility rule as the CV list (section 16): if the candidate no
        // longer satisfies the position's access rules, the CV stays hidden.
        var eligible = await _accessEvaluator.IsCandidateEligibleAsync(cv.CandidateId, cv.PositionId);
        if (!eligible)
            throw new NotFoundException("CV not found.");

        var candidate = await _db.Users.FirstAsync(u => u.Id == cv.CandidateId);

        var likesCount = await _db.CvLikes.CountAsync(l => l.CvId == cvId);
        var likedByCurrentUser = await _db.CvLikes.AnyAsync(l => l.CvId == cvId && l.RecruiterId == recruiterId);

        return new RecruiterCvHeaderDto(
            cv.Id,
            $"{candidate.FirstName} {candidate.LastName}",
            cv.PositionId,
            cv.Position.Title,
            cv.Status,
            cv.PublishedAt,
            likesCount,
            likedByCurrentUser);
    }

    private static AttributeColumnValueDto FormatValue(int attributeId, AttributeType type, CandidateAttributeValue? value)
    {
        if (value is null)
            return new AttributeColumnValueDto(attributeId, string.Empty, true);

        var display = type switch
        {
            AttributeType.Numeric => value.NumericValue?.ToString() ?? string.Empty,
            AttributeType.Date => value.DateValue?.ToString("dd MMM yyyy") ?? string.Empty,
            AttributeType.Boolean => value.BooleanValue is null ? string.Empty : (value.BooleanValue.Value ? "Yes" : "No"),
            AttributeType.Dropdown => value.SelectedOption?.Value ?? string.Empty,
            _ => value.TextValue ?? string.Empty
        };

        return new AttributeColumnValueDto(attributeId, display, string.IsNullOrWhiteSpace(display));
    }
}