using CVPlatform.Domain.Enums;

namespace CVPlatform.Application.Cvs;

public record CvSummaryDto(
    int Id,
    int PositionId,
    string PositionTitle,
    CvStatus Status,
    DateTime CreatedAt,
    DateTime? PublishedAt);

public record CvAttributeDto(
    int AttributeId,
    string AttributeName,
    AttributeType Type,
    bool IsRequired,
    int? ValueId,
    int ValueVersion,
    string? TextValue,
    decimal? NumericValue,
    DateTime? DateValue,
    bool? BooleanValue,
    int? SelectedOptionId,
    bool IsMissing,
    IReadOnlyList<AttributeOptionInfo> Options,
    DateTime? DateRangeStart = null,
    DateTime? DateRangeEnd = null);

public record AttributeOptionInfo(int Id, string Value);

public record CvProjectDto(
    int ProjectId,
    string Name,
    DateTime StartDate,
    DateTime? EndDate,
    string DescriptionMarkdown,
    IReadOnlyList<string> Tags);

public record GeneratedCvDto(
    int CvId,
    int PositionId,
    string PositionTitle,
    CvStatus Status,
    int Version,
    IReadOnlyList<CvAttributeDto> Attributes,
    IReadOnlyList<CvProjectDto> Projects);

public record AvailablePositionDto(int PositionId, string Title);