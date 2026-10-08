using CVPlatform.Application.Positions;
using CVPlatform.Domain.Enums;
using CVPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using CVPlatform.Domain.Entities;

namespace CVPlatform.Infrastructure.Services;

public class PositionAccessEvaluator : IPositionAccessEvaluator
{
    private readonly ApplicationDbContext _db;

    public PositionAccessEvaluator(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> IsCandidateEligibleAsync(string candidateId, int positionId)
    {
        var ids = await GetEligiblePositionIdsAsync(candidateId, new[] { positionId });
        return ids.Contains(positionId);
    }

    public async Task<HashSet<int>> GetEligiblePositionIdsAsync(
        string candidateId, IReadOnlyCollection<int> positionIds)
    {
        var eligible = new HashSet<int>();
        if (positionIds.Count == 0) return eligible;

        var positions = await _db.Positions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.AccessRules).ThenInclude(r => r.Attribute)
            .Where(p => positionIds.Contains(p.Id))
            .ToListAsync();

        var attributeIds = positions
            .Where(p => p.AccessMode != AccessMode.Public)
            .SelectMany(p => p.AccessRules)
            .Select(r => r.AttributeId)
            .Distinct()
            .ToList();

        var values = attributeIds.Count == 0
            ? new Dictionary<int, CandidateAttributeValue>()
            : await _db.CandidateAttributeValues
                .AsNoTracking()
                .Where(v => v.CandidateId == candidateId && attributeIds.Contains(v.AttributeId))
                .ToDictionaryAsync(v => v.AttributeId);

        foreach (var position in positions)
        {
            if (position.AccessMode == AccessMode.Public || position.AccessRules.Count == 0)
            {
                eligible.Add(position.Id);
                continue;
            }

            var allRulesPass = position.AccessRules.All(r =>
                values.TryGetValue(r.AttributeId, out var v) && EvaluateRule(r, v));

            if (allRulesPass) eligible.Add(position.Id);
        }

        return eligible;
    }

    public async Task<HashSet<(string CandidateId, int PositionId)>> GetEligibleCandidatePositionPairsAsync(
        IReadOnlyCollection<(string CandidateId, int PositionId)> pairs)
    {
        var eligible = new HashSet<(string CandidateId, int PositionId)>();
        if (pairs.Count == 0) return eligible;

        var positionIds = pairs.Select(p => p.PositionId).Distinct().ToList();
        var candidateIds = pairs.Select(p => p.CandidateId).Distinct().ToList();

        var positions = await _db.Positions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.AccessRules).ThenInclude(r => r.Attribute)
            .Where(p => positionIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var attributeIds = positions.Values
            .Where(p => p.AccessMode != AccessMode.Public)
            .SelectMany(p => p.AccessRules)
            .Select(r => r.AttributeId)
            .Distinct()
            .ToList();

        var valuesList = attributeIds.Count == 0
            ? new List<CandidateAttributeValue>()
            : await _db.CandidateAttributeValues
                .AsNoTracking()
                .Where(v => candidateIds.Contains(v.CandidateId) && attributeIds.Contains(v.AttributeId))
                .ToListAsync();

        var valuesLookup = valuesList
            .ToLookup(v => (v.CandidateId, v.AttributeId), v => v);

        foreach (var pair in pairs)
        {
            if (!positions.TryGetValue(pair.PositionId, out var position))
                continue;

            if (position.AccessMode == AccessMode.Public || position.AccessRules.Count == 0)
            {
                eligible.Add(pair);
                continue;
            }

            var allRulesPass = position.AccessRules.All(r =>
            {
                var candidateValue = valuesLookup[(pair.CandidateId, r.AttributeId)].FirstOrDefault();
                return candidateValue is not null && EvaluateRule(r, candidateValue);
            });

            if (allRulesPass)
                eligible.Add(pair);
        }

        return eligible;
    }
    private static bool EvaluateRule(Domain.Entities.PositionAccessRule rule, Domain.Entities.CandidateAttributeValue value)
    {
        switch (rule.Attribute.Type)
        {
            case AttributeType.Numeric:
                if (value.NumericValue is null || !decimal.TryParse(rule.ComparisonValue, out var target))
                    return false;
                return CompareNumeric(value.NumericValue.Value, target, rule.Operator);

            case AttributeType.Boolean:
                if (value.BooleanValue is null || !bool.TryParse(rule.ComparisonValue, out var boolTarget))
                    return false;
                return rule.Operator == RuleOperator.Equals
                    ? value.BooleanValue == boolTarget
                    : value.BooleanValue != boolTarget;

            case AttributeType.Dropdown:
                return rule.Operator == RuleOperator.Equals && value.SelectedOptionId == rule.ComparisonOptionId;

            case AttributeType.Date:
                if (value.DateValue is null || !DateTime.TryParse(rule.ComparisonValue, out var dateTarget))
                    return false;
                return CompareNumeric(value.DateValue.Value.Ticks, dateTarget.Ticks, rule.Operator);

            default:
                var text = value.TextValue ?? string.Empty;
                return rule.Operator switch
                {
                    RuleOperator.Equals => text.Equals(rule.ComparisonValue, StringComparison.OrdinalIgnoreCase),
                    RuleOperator.NotEquals => !text.Equals(rule.ComparisonValue, StringComparison.OrdinalIgnoreCase),
                    RuleOperator.Contains => text.Contains(rule.ComparisonValue, StringComparison.OrdinalIgnoreCase),
                    _ => false
                };
        }
    }

    private static bool CompareNumeric(decimal actual, decimal target, RuleOperator op) => op switch
    {
        RuleOperator.Equals => actual == target,
        RuleOperator.NotEquals => actual != target,
        RuleOperator.GreaterThan => actual > target,
        RuleOperator.LessThan => actual < target,
        RuleOperator.GreaterThanOrEqual => actual >= target,
        RuleOperator.LessThanOrEqual => actual <= target,
        _ => false
    };

    private static bool CompareNumeric(long actual, long target, RuleOperator op) =>
        CompareNumeric((decimal)actual, target, op);
}