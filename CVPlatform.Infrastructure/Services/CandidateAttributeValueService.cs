using CVPlatform.Application.Common.Exceptions;
using CVPlatform.Application.Profile;
using CVPlatform.Domain.Entities;
using CVPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CVPlatform.Infrastructure.Services;

public class CandidateAttributeValueService : ICandidateAttributeValueService
{
    private readonly ApplicationDbContext _db;

    public CandidateAttributeValueService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CandidateAttributeValueDto>> GetMyValuesAsync(string candidateId)
    {
        var values = await _db.CandidateAttributeValues
            .Include(v => v.Attribute)
            .Where(v => v.CandidateId == candidateId)
            .OrderBy(v => v.Attribute.Name)
            .ToListAsync();

        return values.Select(MapToDto).ToList();
    }

    public async Task<CandidateAttributeValueDto> AddAttributeAsync(string candidateId, int attributeId)
    {
        if (await _db.Attributes.AnyAsync(a => a.Id == attributeId && a.IsBuiltIn))
            throw new InvalidOperationException("Built-in attributes are part of your profile already.");

        var exists = await _db.CandidateAttributeValues
            .AnyAsync(v => v.CandidateId == candidateId && v.AttributeId == attributeId);

        if (exists)
            throw new InvalidOperationException("This attribute is already in your profile.");

        var value = new CandidateAttributeValue
        {
            CandidateId = candidateId,
            AttributeId = attributeId,
            Version = 1
        };

        _db.CandidateAttributeValues.Add(value);
        await _db.SaveChangesAsync();

        var saved = await _db.CandidateAttributeValues
            .Include(v => v.Attribute)
            .FirstAsync(v => v.Id == value.Id);

        return MapToDto(saved);
    }

    public async Task RemoveByAttributeIdAsync(string candidateId, int attributeId)
    {
        var value = await _db.CandidateAttributeValues
            .Include(v => v.Attribute)
            .FirstOrDefaultAsync(v => v.CandidateId == candidateId && v.AttributeId == attributeId)
            ?? throw new NotFoundException("Attribute value not found.");

        if (value.Attribute.IsBuiltIn)
            throw new InvalidOperationException("Built-in attributes cannot be removed.");

        _db.CandidateAttributeValues.Remove(value);
        await _db.SaveChangesAsync();
    }

    public async Task<CandidateAttributeValueDto> SetValueAsync(string candidateId, SetAttributeValueRequest request)
    {
        var value = await _db.CandidateAttributeValues
    .Include(v => v.Attribute)
    .FirstOrDefaultAsync(v => v.AttributeId == request.AttributeId
                           && v.CandidateId == candidateId)
    ?? throw new NotFoundException("Attribute value not found.");

        _db.Entry(value).Property(v => v.Version).OriginalValue = request.Version;

        value.TextValue = request.TextValue;
        value.NumericValue = request.NumericValue;
        value.DateValue = request.DateValue;
        value.DateRangeStart = request.DateRangeStart;
        value.DateRangeEnd = request.DateRangeEnd;
        value.BooleanValue = request.BooleanValue;
        value.SelectedOptionId = request.SelectedOptionId;
        value.Version++;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException();
        }

        return MapToDto(value);
    }

    private static CandidateAttributeValueDto MapToDto(CandidateAttributeValue value)
    {
        return new CandidateAttributeValueDto(
            value.Id,
            value.AttributeId,
            value.Attribute.Name,
            value.Attribute.Type,
            value.TextValue,
            value.NumericValue,
            value.DateValue,
            value.DateRangeStart,
            value.DateRangeEnd,
            value.BooleanValue,
            value.SelectedOptionId,
            value.Version);
    }
}