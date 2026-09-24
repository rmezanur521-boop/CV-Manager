using CVPlatform.Domain.Enums;

namespace CVPlatform.Application.Positions;

public class AccessRuleInput
{
    public int AttributeId { get; set; }
    public RuleOperator Operator { get; set; }
    public string ComparisonValue { get; set; } = string.Empty;
    public int? ComparisonOptionId { get; set; }
}

public class SavePositionRequest
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string? Company { get; set; }
    public PositionLevel? Level { get; set; }
    public AccessMode AccessMode { get; set; }
    public int Version { get; set; }
    public List<int> AttributeIds { get; set; } = new();
    public List<int> RequiredAttributeIds { get; set; } = new();
    public List<AccessRuleInput> AccessRules { get; set; } = new();
    public int MaxProjects { get; set; }
    public List<string> RequiredProjectTags { get; set; } = new();
}