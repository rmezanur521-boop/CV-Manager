using CVPlatform.Application.Positions;
using CVPlatform.Domain.Enums;
using CVPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CVPlatform.Infrastructure.Services;

public class PositionAggregateService : IPositionAggregateService
{
    private readonly ApplicationDbContext _db;

    public PositionAggregateService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PositionSummaryDto?> GetSummaryAsync(int positionId)
    {
        var position = await _db.Positions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == positionId);

        if (position == null)
        {
            return null;
        }

        var publishedCount = await _db.Cvs
            .AsNoTracking()
            .CountAsync(c => c.PositionId == positionId && c.Status == CvStatus.Published);

        return new PositionSummaryDto(
            position.Id,
            position.Title,
            position.Company,
            position.Level?.ToString(),
            position.ShortDescription,
            publishedCount,
            DateTime.UtcNow);
    }

    public async Task<PositionAggregatesDto?> GetAggregatesAsync(int positionId)
    {
        var position = await _db.Positions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == positionId);

        if (position == null)
        {
            return null;
        }

        var candidateIds = await _db.Cvs
            .AsNoTracking()
            .Where(c => c.PositionId == positionId && c.Status == CvStatus.Published)
            .Select(c => c.CandidateId)
            .Distinct()
            .ToListAsync();

        var positionAttributes = await _db.PositionAttributes
            .AsNoTracking()
            .Include(pa => pa.Attribute)
            .ThenInclude(a => a.Options)
            .Where(pa => pa.PositionId == positionId)
            .OrderBy(pa => pa.AttributeId)
            .ToListAsync();

        var attributeIds = positionAttributes.Select(pa => pa.AttributeId).ToList();

        var candidateValues = await _db.CandidateAttributeValues
            .AsNoTracking()
            .Include(v => v.SelectedOption)
            .Where(v => candidateIds.Contains(v.CandidateId) && attributeIds.Contains(v.AttributeId))
            .ToListAsync();

        var valueGroups = candidateValues
            .GroupBy(v => v.AttributeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var attributeAggregates = new List<AttributeAggregateDto>();

        foreach (var pa in positionAttributes)
        {
            var attr = pa.Attribute;
            valueGroups.TryGetValue(attr.Id, out var vals);
            vals ??= new();

            switch (attr.Type)
            {
                case AttributeType.Numeric:
                    var numVals = vals.Where(v => v.NumericValue.HasValue).Select(v => v.NumericValue!.Value).ToList();
                    if (numVals.Any())
                    {
                        var min = numVals.Min();
                        var max = numVals.Max();
                        var avg = Math.Round(numVals.Average(), 2);
                        var summary = $"Avg: {avg}, Min: {min}, Max: {max}";
                        attributeAggregates.Add(new AttributeAggregateDto(
                            attr.Id, attr.Name, attr.Type.ToString(), pa.IsRequired, numVals.Count, summary,
                            Numeric: new NumericAggregateDto(min, max, avg)));
                    }
                    else
                    {
                        attributeAggregates.Add(new AttributeAggregateDto(
                            attr.Id, attr.Name, attr.Type.ToString(), pa.IsRequired, 0, "No responses"));
                    }
                    break;

                case AttributeType.Date:
                    var dateVals = vals.Where(v => v.DateValue.HasValue).Select(v => v.DateValue!.Value).OrderBy(d => d).ToList();
                    if (dateVals.Any())
                    {
                        var minDate = dateVals.First().ToString("yyyy-MM-dd");
                        var maxDate = dateVals.Last().ToString("yyyy-MM-dd");
                        var summary = $"From: {minDate}, To: {maxDate}";
                        attributeAggregates.Add(new AttributeAggregateDto(
                            attr.Id, attr.Name, attr.Type.ToString(), pa.IsRequired, dateVals.Count, summary,
                            Date: new DateAggregateDto(minDate, maxDate)));
                    }
                    else
                    {
                        attributeAggregates.Add(new AttributeAggregateDto(
                            attr.Id, attr.Name, attr.Type.ToString(), pa.IsRequired, 0, "No responses"));
                    }
                    break;

                case AttributeType.DateRange:
                    var rangeVals = vals.Where(v => v.DateRangeStart.HasValue || v.DateRangeEnd.HasValue).ToList();
                    if (rangeVals.Any())
                    {
                        var starts = rangeVals.Where(v => v.DateRangeStart.HasValue).Select(v => v.DateRangeStart!.Value).OrderBy(d => d).ToList();
                        var ends = rangeVals.Where(v => v.DateRangeEnd.HasValue).Select(v => v.DateRangeEnd!.Value).OrderBy(d => d).ToList();
                        var minStart = starts.FirstOrDefault().ToString("yyyy-MM-dd");
                        var maxEnd = ends.LastOrDefault().ToString("yyyy-MM-dd");
                        var summary = $"Earliest: {minStart ?? "N/A"}, Latest: {maxEnd ?? "N/A"}";
                        attributeAggregates.Add(new AttributeAggregateDto(
                            attr.Id, attr.Name, attr.Type.ToString(), pa.IsRequired, rangeVals.Count, summary,
                            DateRange: new DateRangeAggregateDto(minStart, maxEnd)));
                    }
                    else
                    {
                        attributeAggregates.Add(new AttributeAggregateDto(
                            attr.Id, attr.Name, attr.Type.ToString(), pa.IsRequired, 0, "No responses"));
                    }
                    break;

                case AttributeType.Boolean:
                    var boolVals = vals.Where(v => v.BooleanValue.HasValue).Select(v => v.BooleanValue!.Value).ToList();
                    var trueCount = boolVals.Count(b => b);
                    var falseCount = boolVals.Count(b => !b);
                    var boolSummary = boolVals.Any() ? $"true: {trueCount} / false: {falseCount}" : "No responses";
                    attributeAggregates.Add(new AttributeAggregateDto(
                        attr.Id, attr.Name, attr.Type.ToString(), pa.IsRequired, boolVals.Count, boolSummary,
                        Boolean: new BooleanAggregateDto(trueCount, falseCount)));
                    break;

                case AttributeType.Dropdown:
                    var dropdownVals = vals.Where(v => v.SelectedOptionId.HasValue).ToList();
                    var optionCounts = dropdownVals
                        .GroupBy(v => v.SelectedOption != null ? v.SelectedOption.Value : v.SelectedOptionId!.Value.ToString())
                        .Select(g => new OptionCountDto(g.Key, g.Count()))
                        .OrderByDescending(o => o.Count)
                        .Take(5)
                        .ToList();
                    var dropSummary = optionCounts.Any()
                        ? string.Join(", ", optionCounts.Select(o => $"{o.Value}: {o.Count}"))
                        : "No responses";
                    attributeAggregates.Add(new AttributeAggregateDto(
                        attr.Id, attr.Name, attr.Type.ToString(), pa.IsRequired, dropdownVals.Count, dropSummary,
                        Dropdown: new DropdownAggregateDto(optionCounts)));
                    break;

                case AttributeType.SingleLineText:
                    var textVals = vals.Where(v => !string.IsNullOrWhiteSpace(v.TextValue)).Select(v => v.TextValue!.Trim()).ToList();
                    var topText = textVals
                        .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
                        .Select(g => new ValueCountDto(g.First(), g.Count()))
                        .OrderByDescending(v => v.Count)
                        .Take(5)
                        .ToList();
                    var textSummary = topText.Any()
                        ? string.Join(", ", topText.Select(t => $"{t.Value}: {t.Count}"))
                        : "No responses";
                    attributeAggregates.Add(new AttributeAggregateDto(
                        attr.Id, attr.Name, attr.Type.ToString(), pa.IsRequired, textVals.Count, textSummary,
                        SingleLineText: new TextAggregateDto(topText)));
                    break;

                case AttributeType.MarkdownText:
                    var mdVals = vals.Where(v => !string.IsNullOrWhiteSpace(v.TextValue)).Select(v => v.TextValue!.Length).ToList();
                    var avgLen = mdVals.Any() ? Math.Round(mdVals.Average(), 2) : 0;
                    var mdSummary = mdVals.Any() ? $"{mdVals.Count} responses (avg length: {avgLen} chars)" : "No responses";
                    attributeAggregates.Add(new AttributeAggregateDto(
                        attr.Id, attr.Name, attr.Type.ToString(), pa.IsRequired, mdVals.Count, mdSummary,
                        MarkdownText: new MarkdownAggregateDto(avgLen)));
                    break;

                case AttributeType.Image:
                    var imgVals = vals.Where(v => !string.IsNullOrWhiteSpace(v.TextValue)).ToList();
                    var imgSummary = imgVals.Any() ? $"{imgVals.Count} images uploaded" : "No responses";
                    attributeAggregates.Add(new AttributeAggregateDto(
                        attr.Id, attr.Name, attr.Type.ToString(), pa.IsRequired, imgVals.Count, imgSummary));
                    break;
            }
        }

        return new PositionAggregatesDto(
            position.Id,
            position.Title,
            position.Company,
            position.Level?.ToString(),
            position.ShortDescription,
            candidateIds.Count,
            DateTime.UtcNow,
            attributeAggregates);
    }
}
