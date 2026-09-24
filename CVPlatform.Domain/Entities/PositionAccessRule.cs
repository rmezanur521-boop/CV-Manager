using CVPlatform.Domain.Enums;

namespace CVPlatform.Domain.Entities;

public class PositionAccessRule
{
    public int Id { get; set; }
    public int PositionId { get; set; }
    public Position Position { get; set; } = null!;
    public int AttributeId { get; set; }
    public AttributeDefinition Attribute { get; set; } = null!;
    public RuleOperator Operator { get; set; }
    public string ComparisonValue { get; set; } = string.Empty;
    public int? ComparisonOptionId { get; set; }
}