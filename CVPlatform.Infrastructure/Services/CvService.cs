using CVPlatform.Application.Common.Exceptions;
using CVPlatform.Application.Cvs;
using CVPlatform.Application.Positions;
using CVPlatform.Domain.Entities;
using CVPlatform.Domain.Enums;
using CVPlatform.Infrastructure.Persistence;
using CVPlatform.Domain.Constants;
using CVPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CVPlatform.Infrastructure.Services;

public class CvService : ICvService
{
    private readonly ApplicationDbContext _db;
    private readonly IPositionAccessEvaluator _accessEvaluator;
    private readonly UserManager<ApplicationUser> _userManager;
    public CvService(ApplicationDbContext db, IPositionAccessEvaluator accessEvaluator, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _accessEvaluator = accessEvaluator;
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<CvSummaryDto>> GetMyCvsAsync(string candidateId)
    {
        var cvs = await _db.Cvs
            .AsNoTracking()
            .Where(c => c.CandidateId == candidateId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new { c.Id, c.PositionId, c.Position.Title, c.Status, c.CreatedAt, c.PublishedAt })
            .ToListAsync();

        var eligibleIds = await _accessEvaluator.GetEligiblePositionIdsAsync(
            candidateId, cvs.Select(c => c.PositionId).Distinct().ToList());

        return cvs
            .Where(c => eligibleIds.Contains(c.PositionId))
            .Select(c => new CvSummaryDto(c.Id, c.PositionId, c.Title, c.Status, c.CreatedAt, c.PublishedAt))
            .ToList();
    }

    public async Task<IReadOnlyList<AvailablePositionDto>> GetAvailablePositionsAsync(string candidateId)
    {
        var existingPositionIds = await _db.Cvs
            .Where(c => c.CandidateId == candidateId)
            .Select(c => c.PositionId)
            .ToListAsync();

        var candidates = await _db.Positions
            .AsNoTracking()
            .Where(p => !existingPositionIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Title })
            .ToListAsync();

        var eligibleIds = await _accessEvaluator.GetEligiblePositionIdsAsync(
            candidateId, candidates.Select(p => p.Id).ToList());

        return candidates
            .Where(p => eligibleIds.Contains(p.Id))
            .Select(p => new AvailablePositionDto(p.Id, p.Title))
            .ToList();
    }

    public async Task<GeneratedCvDto> CreateAsync(string candidateId, int positionId)
    {
        var alreadyExists = await _db.Cvs.AnyAsync(c => c.CandidateId == candidateId && c.PositionId == positionId);
        if (alreadyExists)
            throw new InvalidOperationException("You already have a CV for this position.");

        var eligible = await _accessEvaluator.IsCandidateEligibleAsync(candidateId, positionId);
        if (!eligible)
            throw new InvalidOperationException("You are not eligible for this position.");

        var cv = new Cv
        {
            CandidateId = candidateId,
            PositionId = positionId,
            Status = CvStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            Version = 1
        };

        _db.Cvs.Add(cv);
        await _db.SaveChangesAsync();

        return await GetGeneratedAsync(candidateId, cv.Id);
    }

    public async Task<GeneratedCvDto> GetGeneratedAsync(string candidateId, int cvId)
    {
        var cv = await _db.Cvs
            .Include(c => c.Position).ThenInclude(p => p.PositionAttributes).ThenInclude(pa => pa.Attribute).ThenInclude(a => a.Options)
            .Include(c => c.Position).ThenInclude(p => p.ProjectFilter).ThenInclude(f => f!.RequiredTags).ThenInclude(t => t.Tag)
            .FirstOrDefaultAsync(c => c.Id == cvId && c.CandidateId == candidateId)
            ?? throw new NotFoundException("CV not found.");

        var user = await _userManager.FindByIdAsync(candidateId)
            ?? throw new NotFoundException("Candidate not found.");

        var templateIds = cv.Position.PositionAttributes.Select(pa => pa.AttributeId).ToList();

        var extraBuiltIns = await _db.Attributes
            .AsNoTracking()
            .Include(a => a.Options)
            .Where(a => a.IsBuiltIn && !templateIds.Contains(a.Id))
            .OrderBy(a => a.Id)
            .ToListAsync();

        var rows = extraBuiltIns
            .Select(a => (Attribute: a, IsRequired: BuiltInAttributes.RequiredNames.Contains(a.Name)))
            .Concat(cv.Position.PositionAttributes.Select(pa => (Attribute: pa.Attribute, IsRequired: pa.IsRequired)))
            .OrderByDescending(r => r.Attribute.IsBuiltIn)   
            .ToList();

        var customIds = rows.Where(r => !r.Attribute.IsBuiltIn).Select(r => r.Attribute.Id).ToList();

        var values = await _db.CandidateAttributeValues
            .AsNoTracking()
            .Where(v => v.CandidateId == candidateId && customIds.Contains(v.AttributeId))
            .ToDictionaryAsync(v => v.AttributeId);

        var attributes = rows.Select(r =>
        {
            CandidateAttributeValue? value = r.Attribute.IsBuiltIn
                ? BuiltInValue(r.Attribute, user)
                : values.GetValueOrDefault(r.Attribute.Id);

            return ToCvAttributeDto(r.Attribute, r.IsRequired, value);
        }).ToList();

        var projects = await GetFilteredProjectsAsync(candidateId, cv.Position.ProjectFilter);

        return new GeneratedCvDto(
            cv.Id, cv.PositionId, cv.Position.Title, cv.Status, cv.Version, attributes, projects);
    }

    public async Task<CvAttributeDto> SetAttributeValueAsync(
        string candidateId, int cvId, SetCvAttributeValueRequest request)
    {
        var cv = await _db.Cvs
            .Include(c => c.Position).ThenInclude(p => p.PositionAttributes)
            .FirstOrDefaultAsync(c => c.Id == cvId && c.CandidateId == candidateId)
            ?? throw new NotFoundException("CV not found.");

        var attribute = await _db.Attributes
            .Include(a => a.Options)
            .FirstOrDefaultAsync(a => a.Id == request.AttributeId)
            ?? throw new InvalidOperationException("Attribute not found.");

        var templateEntry = cv.Position.PositionAttributes
            .FirstOrDefault(pa => pa.AttributeId == attribute.Id);

        if (templateEntry is null && !attribute.IsBuiltIn)
            throw new InvalidOperationException("This attribute does not belong to the position template.");

        var isRequired = templateEntry?.IsRequired
            ?? BuiltInAttributes.RequiredNames.Contains(attribute.Name);

        if (attribute.IsBuiltIn)
        {
            var user = await _userManager.FindByIdAsync(candidateId)
                ?? throw new NotFoundException("Candidate not found.");

            SetBuiltInText(user, attribute.Name, request.TextValue);

            var result = await _userManager.UpdateAsync(user);  
            if (!result.Succeeded)
                throw new ConcurrencyConflictException();

            return ToCvAttributeDto(attribute, isRequired, BuiltInValue(attribute, user));
        }

        var value = await _db.CandidateAttributeValues
            .FirstOrDefaultAsync(v => v.CandidateId == candidateId && v.AttributeId == request.AttributeId);

        if (value is null)
        {
            value = new CandidateAttributeValue
            {
                CandidateId = candidateId,
                AttributeId = request.AttributeId,
                Version = 1
            };
            _db.CandidateAttributeValues.Add(value);
        }
        else
        {
            _db.Entry(value).Property(v => v.Version).OriginalValue = request.Version;
            value.Version++;
        }

        value.TextValue = request.TextValue;
        value.NumericValue = request.NumericValue;
        value.DateValue = request.DateValue;
        value.DateRangeStart = request.DateRangeStart;
        value.DateRangeEnd = request.DateRangeEnd;
        value.BooleanValue = request.BooleanValue;
        value.SelectedOptionId = request.SelectedOptionId;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException();
        }

        return ToCvAttributeDto(attribute, isRequired, value);
    }


    private static CvAttributeDto ToCvAttributeDto(
        AttributeDefinition attribute, bool isRequired, CandidateAttributeValue? value) =>
        new(
            attribute.Id,
            attribute.Name,
            attribute.Type,
            isRequired,
            value?.Id,
            value?.Version ?? 0,
            value?.TextValue,
            value?.NumericValue,
            value?.DateValue,
            value?.BooleanValue,
            value?.SelectedOptionId,
            value is null || !HasValue(attribute.Type, value),
            attribute.Options
                .OrderBy(o => o.DisplayOrder)
                .Select(o => new AttributeOptionInfo(o.Id, o.Value))
                .ToList(),
            value?.DateRangeStart,
            value?.DateRangeEnd);

    private static CandidateAttributeValue BuiltInValue(AttributeDefinition attribute, ApplicationUser user) =>
        new()
        {
            CandidateId = user.Id,
            AttributeId = attribute.Id,
            TextValue = GetBuiltInText(user, attribute.Name),
            Version = 0
        };

    private static string? GetBuiltInText(ApplicationUser user, string name) => name switch
    {
        BuiltInAttributes.FirstName => user.FirstName,
        BuiltInAttributes.LastName => user.LastName,
        BuiltInAttributes.Location => user.Location,
        BuiltInAttributes.PersonalPhoto => user.PhotoUrl,
        _ => null
    };

    private static void SetBuiltInText(ApplicationUser user, string name, string? value)
    {
        switch (name)
        {
            case BuiltInAttributes.FirstName: user.FirstName = value?.Trim() ?? string.Empty; break;
            case BuiltInAttributes.LastName: user.LastName = value?.Trim() ?? string.Empty; break;
            case BuiltInAttributes.Location: user.Location = value?.Trim(); break;
            case BuiltInAttributes.PersonalPhoto: user.PhotoUrl = value; break;
        }
    }

    public async Task PublishAsync(string candidateId, int cvId, int version)
    {
        var generated = await GetGeneratedAsync(candidateId, cvId);

        var missing = generated.Attributes.Where(a => a.IsRequired && a.IsMissing).Select(a => a.AttributeName).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException($"Cannot publish, missing required values: {string.Join(", ", missing)}");

        var cv = await _db.Cvs.FirstOrDefaultAsync(c => c.Id == cvId && c.CandidateId == candidateId)
            ?? throw new NotFoundException("CV not found.");

        _db.Entry(cv).Property(c => c.Version).OriginalValue = version;

        cv.Status = CvStatus.Published;
        cv.PublishedAt = DateTime.UtcNow;
        cv.Version++;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException();
        }
    }

    public async Task DeleteAsync(string candidateId, int cvId)
    {
        var cv = await _db.Cvs.FirstOrDefaultAsync(c => c.Id == cvId && c.CandidateId == candidateId)
            ?? throw new NotFoundException("CV not found.");

        _db.Cvs.Remove(cv);
        await _db.SaveChangesAsync();
    }

    private async Task<IReadOnlyList<CvProjectDto>> GetFilteredProjectsAsync(string candidateId, PositionProjectFilter? filter)
    {
        var query = _db.Projects.Where(p => p.CandidateId == candidateId);

        var requiredTagIds = filter?.RequiredTags.Select(t => t.TagId).Distinct().ToList() ?? new List<int>();
        var requiredCount = requiredTagIds.Count;

        if (requiredCount > 0)
        {
            query = query.Where(p => p.ProjectTags.Count(pt => requiredTagIds.Contains(pt.TagId)) == requiredCount);
        }

        query = query.OrderByDescending(p => p.StartDate).ThenByDescending(p => p.Id);

        if (filter is not null && filter.MaxProjects > 0)
        {
            query = query.Take(filter.MaxProjects);
        }

        return await query
            .Select(p => new CvProjectDto(
                p.Id,
                p.Name,
                p.StartDate,
                p.EndDate,
                p.DescriptionMarkdown,
                p.ProjectTags.Select(pt => pt.Tag.Name).OrderBy(name => name).ToList()))
            .ToListAsync();
    }
    public async Task<GeneratedCvDto> GetPublishedForRecruiterAsync(int cvId)
    {
        var cv = await _db.Cvs.FirstOrDefaultAsync(c => c.Id == cvId && c.Status == CvStatus.Published)
            ?? throw new NotFoundException("CV not found.");

        var eligible = await _accessEvaluator.IsCandidateEligibleAsync(cv.CandidateId, cv.PositionId);
        if (!eligible)
            throw new NotFoundException("CV not found.");

        return await GetGeneratedAsync(cv.CandidateId, cvId);
    }

    private static bool HasValue(AttributeType type, CandidateAttributeValue value) => type switch
    {
        AttributeType.Numeric => value.NumericValue.HasValue,
        AttributeType.Date => value.DateValue.HasValue,
        AttributeType.DateRange => value.DateRangeStart.HasValue,
        AttributeType.Boolean => value.BooleanValue.HasValue,
        AttributeType.Dropdown => value.SelectedOptionId.HasValue,
        AttributeType.Image => !string.IsNullOrWhiteSpace(value.TextValue),
        _ => !string.IsNullOrWhiteSpace(value.TextValue)
    };
}