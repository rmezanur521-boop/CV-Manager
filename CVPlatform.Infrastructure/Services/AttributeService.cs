using CVPlatform.Application.Attributes;
using CVPlatform.Application.Common.Exceptions;
using CVPlatform.Domain.Entities;
using CVPlatform.Domain.Enums;
using CVPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CVPlatform.Infrastructure.Services;

public class AttributeService : IAttributeService
{
    private readonly ApplicationDbContext _db;

    public AttributeService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AttributeDto>> GetAllAsync(int? categoryId = null, string? prefix = null)
    {
        var query = _db.Attributes.Include(a => a.Category).Include(a => a.Options).AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(a => a.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(prefix))
            query = query.Where(a => a.Name.StartsWith(prefix));

        var attributes = await query.OrderBy(a => a.Name).ToListAsync();
        return attributes.Select(MapToDto).ToList();
    }

    public async Task<AttributeDto> GetByIdAsync(int id)
    {
        var attribute = await _db.Attributes
            .Include(a => a.Category)
            .Include(a => a.Options)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attribute is null)
            throw new NotFoundException($"Attribute {id} was not found.");

        return MapToDto(attribute);
    }

    public async Task<AttributeDto> CreateAsync(CreateAttributeRequest request)
    {
        var nameExists = await _db.Attributes.AnyAsync(a => a.Name == request.Name);
        if (nameExists)
            throw new InvalidOperationException($"Attribute name '{request.Name}' already exists.");

        if (request.Type == AttributeType.Dropdown && request.Options.Count < 2)
            throw new InvalidOperationException("Dropdown attributes require at least two options.");

        var attribute = new AttributeDefinition
        {
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            CategoryId = request.CategoryId,
            IsBuiltIn = false,
            Version = 1
        };

        if (request.Type == AttributeType.Dropdown)
        {
            foreach (var (value, index) in request.Options.Select((v, i) => (v, i)))
            {
                attribute.Options.Add(new AttributeOption { Value = value, DisplayOrder = index });
            }
        }

        _db.Attributes.Add(attribute);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(attribute.Id);
    }

    public async Task<AttributeDto> UpdateAsync(UpdateAttributeRequest request)
    {
        var attribute = await _db.Attributes
            .Include(a => a.Options)
            .FirstOrDefaultAsync(a => a.Id == request.Id);

        if (attribute is null)
            throw new NotFoundException($"Attribute {request.Id} was not found.");

        if (attribute.IsBuiltIn)
            throw new InvalidOperationException("Built-in attributes cannot be modified.");

        var nameTaken = await _db.Attributes.AnyAsync(a => a.Id != request.Id && a.Name == request.Name);
        if (nameTaken)
            throw new InvalidOperationException($"Attribute name '{request.Name}' already exists.");

        var wantedOptions = request.Type == AttributeType.Dropdown
            ? NormalizeOptions(request.Options)
            : new List<string>();

        if (request.Type == AttributeType.Dropdown && wantedOptions.Count < 2)
            throw new InvalidOperationException("Dropdown attributes require at least two options.");

        await EnsureChangeIsSafeAsync(attribute, request.Type, wantedOptions);

        _db.Entry(attribute).Property(a => a.Version).OriginalValue = request.Version;

        attribute.Name = request.Name;
        attribute.Description = request.Description;
        attribute.Type = request.Type;
        attribute.CategoryId = request.CategoryId;
        attribute.Version++;

        ApplyOptions(attribute, wantedOptions);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException();
        }

        return await GetByIdAsync(attribute.Id);
    }

    public async Task DeleteAsync(int id)
    {
        var attribute = await _db.Attributes.FirstOrDefaultAsync(a => a.Id == id);
        if (attribute is null)
            throw new NotFoundException($"Attribute {id} was not found.");

        if (attribute.IsBuiltIn)
            throw new InvalidOperationException("Built-in attributes cannot be deleted.");

        _db.Attributes.Remove(attribute);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("This attribute is used by candidate profiles or positions and cannot be deleted.");
        }
    }

    private static List<string> NormalizeOptions(IEnumerable<string> options)
    {
        return options
            .Select(o => o.Trim())
            .Where(o => o.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task EnsureChangeIsSafeAsync(AttributeDefinition attribute, AttributeType newType, List<string> wantedOptions)
    {
        if (attribute.Type != newType)
        {
            var hasValues = await _db.CandidateAttributeValues.AnyAsync(v => v.AttributeId == attribute.Id);
            if (hasValues)
                throw new InvalidOperationException("The type of an attribute that already has candidate values cannot be changed.");
        }

        var removedOptionIds = attribute.Options
            .Where(o => !wantedOptions.Contains(o.Value, StringComparer.OrdinalIgnoreCase))
            .Select(o => o.Id)
            .ToList();

        if (removedOptionIds.Count == 0)
            return;

        var usedByCandidates = await _db.CandidateAttributeValues
            .AnyAsync(v => v.SelectedOptionId != null && removedOptionIds.Contains(v.SelectedOptionId.Value));

        var usedByRules = await _db.PositionAccessRules
            .AnyAsync(r => r.ComparisonOptionId != null && removedOptionIds.Contains(r.ComparisonOptionId.Value));

        if (usedByCandidates || usedByRules)
            throw new InvalidOperationException("An option you removed is still used by candidate values or position access rules, so it cannot be deleted.");
    }

    private static void ApplyOptions(AttributeDefinition attribute, List<string> wantedOptions)
    {
        var removed = attribute.Options
            .Where(o => !wantedOptions.Contains(o.Value, StringComparer.OrdinalIgnoreCase))
            .ToList();

        foreach (var option in removed)
            attribute.Options.Remove(option);

        for (var index = 0; index < wantedOptions.Count; index++)
        {
            var existing = attribute.Options
                .FirstOrDefault(o => string.Equals(o.Value, wantedOptions[index], StringComparison.OrdinalIgnoreCase));

            if (existing is null)
                attribute.Options.Add(new AttributeOption { Value = wantedOptions[index], DisplayOrder = index });
            else
                existing.DisplayOrder = index;
        }
    }

    public async Task<IReadOnlyList<AttributeCategoryDto>> GetCategoriesAsync()
    {
        return await _db.AttributeCategories
            .OrderBy(c => c.Name)
            .Select(c => new AttributeCategoryDto(c.Id, c.Name))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AttributeDto>> GetRecentlyUsedAsync(string userId, int count = 5)
    {
        var recentAttributeIds = await _db.AttributeUsages
            .Where(u => u.UserId == userId)
            .OrderByDescending(u => u.LastUsedAt)
            .Select(u => u.AttributeId)
            .Take(count)
            .ToListAsync();

        if (recentAttributeIds.Count == 0)
            return new List<AttributeDto>();

        var attributes = await _db.Attributes
            .Include(a => a.Category)
            .Include(a => a.Options)
            .Where(a => recentAttributeIds.Contains(a.Id))
            .ToListAsync();

        return recentAttributeIds
            .Select(id => attributes.FirstOrDefault(a => a.Id == id))
            .Where(a => a is not null)
            .Select(a => MapToDto(a!))
            .ToList();
    }

    public async Task RecordUsageAsync(string userId, int attributeId)
    {
        var usage = await _db.AttributeUsages
            .FirstOrDefaultAsync(u => u.UserId == userId && u.AttributeId == attributeId);

        if (usage is null)
        {
            _db.AttributeUsages.Add(new AttributeUsage
            {
                UserId = userId,
                AttributeId = attributeId,
                LastUsedAt = DateTime.UtcNow
            });
        }
        else
        {
            usage.LastUsedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    private static AttributeDto MapToDto(AttributeDefinition attribute)
    {
        return new AttributeDto(
            attribute.Id,
            attribute.Name,
            attribute.Description,
            attribute.Type,
            attribute.CategoryId,
            attribute.Category.Name,
            attribute.IsBuiltIn,
            attribute.Version,
            attribute.Options
                .OrderBy(o => o.DisplayOrder)
                .Select(o => new AttributeOptionDto(o.Id, o.Value, o.DisplayOrder))
                .ToList());
    }
}