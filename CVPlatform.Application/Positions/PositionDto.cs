using CVPlatform.Domain.Enums;

namespace CVPlatform.Application.Positions;

public record PositionAttributeDto(int AttributeId, string AttributeName, bool IsRequired);

public record PositionAccessRuleDto(
    int Id,
    int AttributeId,
    string AttributeName,
    RuleOperator Operator,
    string ComparisonValue,
    int? ComparisonOptionId,
    string? ComparisonOptionLabel = null);
public record PositionDto(
    int Id,
    string Title,
    string ShortDescription,
    string? Company,
    PositionLevel? Level,
    AccessMode AccessMode,
    DateTime CreatedAt,
    int Version,
    IReadOnlyList<PositionAttributeDto> Attributes,
    IReadOnlyList<PositionAccessRuleDto> AccessRules,
    int MaxProjects,
    IReadOnlyList<string> RequiredProjectTags,
    int CvCount = 0);