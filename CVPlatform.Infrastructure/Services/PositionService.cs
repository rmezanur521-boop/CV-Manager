using CVPlatform.Application.Common.Exceptions;
using CVPlatform.Application.Positions;
using CVPlatform.Domain.Entities;
using CVPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CVPlatform.Infrastructure.Services;

public class PositionService : IPositionService
{
    private readonly ApplicationDbContext _db;

    public PositionService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PositionDto>> GetAllAsync(string? search = null)
    {
        var query = LoadFullQuery();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => EF.Functions.Like(p.Title, $"%{term}%")
                || (p.Company != null && EF.Functions.Like(p.Company, $"%{term}%")));
        }

        var positions = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        var positionIds = positions.Select(p => p.Id).ToList();

        var cvCounts = await _db.Cvs
            .Where(c => positionIds.Contains(c.PositionId))
            .GroupBy(c => c.PositionId)
            .Select(g => new { PositionId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.PositionId, g => g.Count);

        return positions
            .Select(p => MapToDto(p, cvCounts.GetValueOrDefault(p.Id)))
            .ToList();
    }

    public async Task<PositionDto> GetByIdAsync(int id)
    {
        var position = await LoadFullQuery().FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException($"Position {id} was not found.");

        var cvCount = await _db.Cvs.CountAsync(c => c.PositionId == id);
        var dto = MapToDto(position, cvCount);

        var optionIds = dto.AccessRules
            .Where(r => r.ComparisonOptionId.HasValue)
            .Select(r => r.ComparisonOptionId!.Value)
            .ToList();

        if (optionIds.Count == 0)
            return dto;

        var labels = await _db.AttributeOptions
            .Where(o => optionIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, o => o.Value);

        var rules = dto.AccessRules
            .Select(r => r.ComparisonOptionId.HasValue && labels.TryGetValue(r.ComparisonOptionId.Value, out var label)
                ? r with { ComparisonOptionLabel = label }
                : r)
            .ToList();

        return dto with { AccessRules = rules };
    }
    public async Task<PositionDto> CreateAsync(SavePositionRequest request)
    {
        var position = new Position
        {
            Title = request.Title,
            ShortDescription = request.ShortDescription,
            Company = request.Company,
            Level = request.Level,
            AccessMode = request.AccessMode,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Version = 1
        };

        ApplyTemplate(position, request);

        _db.Positions.Add(position);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(position.Id);
    }

    public async Task<PositionDto> UpdateAsync(SavePositionRequest request)
    {
        var position = await _db.Positions
            .Include(p => p.PositionAttributes)
            .Include(p => p.AccessRules)
            .Include(p => p.ProjectFilter).ThenInclude(f => f!.RequiredTags)
            .FirstOrDefaultAsync(p => p.Id == request.Id)
            ?? throw new NotFoundException($"Position {request.Id} was not found.");

        _db.Entry(position).Property(p => p.Version).OriginalValue = request.Version;

        position.Title = request.Title;
        position.ShortDescription = request.ShortDescription;
        position.Company = request.Company;
        position.Level = request.Level;
        position.AccessMode = request.AccessMode;
        position.UpdatedAt = DateTime.UtcNow;
        position.Version++;

        position.PositionAttributes.Clear();
        position.AccessRules.Clear();
        if (position.ProjectFilter is not null)
        {
            position.ProjectFilter.RequiredTags.Clear();
            _db.PositionProjectFilters.Remove(position.ProjectFilter);
            position.ProjectFilter = null;
        }

        ApplyTemplate(position, request);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException();
        }

        return await GetByIdAsync(position.Id);
    }

    public async Task DeleteAsync(int id)
    {
        var position = await _db.Positions.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException($"Position {id} was not found.");

        _db.Positions.Remove(position);
        await _db.SaveChangesAsync();
    }

    public async Task<PositionDto> DuplicateAsync(int id)
    {
        var source = await GetByIdAsync(id);

        var request = new SavePositionRequest
        {
            Title = source.Title + " (Copy)",
            ShortDescription = source.ShortDescription,
            Company = source.Company,
            Level = source.Level,
            AccessMode = source.AccessMode,
            AttributeIds = source.Attributes.Select(a => a.AttributeId).ToList(),
            RequiredAttributeIds = source.Attributes.Where(a => a.IsRequired).Select(a => a.AttributeId).ToList(),
            AccessRules = source.AccessRules.Select(r => new AccessRuleInput
            {
                AttributeId = r.AttributeId,
                Operator = r.Operator,
                ComparisonValue = r.ComparisonValue,
                ComparisonOptionId = r.ComparisonOptionId
            }).ToList(),
            MaxProjects = source.MaxProjects,
            RequiredProjectTags = source.RequiredProjectTags.ToList()
        };

        return await CreateAsync(request);
    }

    private void ApplyTemplate(Position position, SavePositionRequest request)
    {
        foreach (var attributeId in request.AttributeIds.Distinct())
        {
            position.PositionAttributes.Add(new PositionAttribute
            {
                AttributeId = attributeId,
                IsRequired = request.RequiredAttributeIds.Contains(attributeId)
            });
        }

        foreach (var rule in request.AccessRules)
        {
            position.AccessRules.Add(new PositionAccessRule
            {
                AttributeId = rule.AttributeId,
                Operator = rule.Operator,
                ComparisonValue = rule.ComparisonValue,
                ComparisonOptionId = rule.ComparisonOptionId
            });
        }

        if (request.MaxProjects > 0 || request.RequiredProjectTags.Count > 0)
        {
            var filter = new PositionProjectFilter { MaxProjects = request.MaxProjects };

            foreach (var tagName in request.RequiredProjectTags.Distinct())
            {
                var tag = _db.TechnologyTags.FirstOrDefault(t => t.Name == tagName);
                if (tag is null)
                {
                    tag = new TechnologyTag { Name = tagName };
                    _db.TechnologyTags.Add(tag);
                }
                filter.RequiredTags.Add(new PositionProjectFilterTag { Tag = tag });
            }

            position.ProjectFilter = filter;
        }
    }

    private IQueryable<Position> LoadFullQuery()
    {
        return _db.Positions
            .Include(p => p.PositionAttributes).ThenInclude(pa => pa.Attribute)
            .Include(p => p.AccessRules).ThenInclude(r => r.Attribute)
            .Include(p => p.ProjectFilter).ThenInclude(f => f!.RequiredTags).ThenInclude(t => t.Tag);
    }

    private static PositionDto MapToDto(Position position, int cvCount = 0)
    {
        return new PositionDto(
            position.Id,
            position.Title,
            position.ShortDescription,
            position.Company,
            position.Level,
            position.AccessMode,
            position.CreatedAt,
            position.Version,
            position.PositionAttributes
                .Select(pa => new PositionAttributeDto(pa.AttributeId, pa.Attribute.Name, pa.IsRequired))
                .ToList(),
            position.AccessRules
                .Select(r => new PositionAccessRuleDto(r.Id, r.AttributeId, r.Attribute.Name, r.Operator, r.ComparisonValue, r.ComparisonOptionId))
                .ToList(),
            position.ProjectFilter?.MaxProjects ?? 0,
            position.ProjectFilter?.RequiredTags.Select(t => t.Tag.Name).ToList() ?? new List<string>(),
            cvCount);
    }
}