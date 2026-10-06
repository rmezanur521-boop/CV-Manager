namespace CVPlatform.Application.Positions;

public record OptionCountDto(string Value, int Count);

public record ValueCountDto(string Value, int Count);

public record NumericAggregateDto(decimal Min, decimal Max, decimal Average);

public record DateAggregateDto(string MinDate, string MaxDate);

public record DateRangeAggregateDto(string? EarliestStart, string? LatestEnd);

public record BooleanAggregateDto(int TrueCount, int FalseCount);

public record DropdownAggregateDto(IReadOnlyList<OptionCountDto> TopOptions);

public record TextAggregateDto(IReadOnlyList<ValueCountDto> TopValues);

public record MarkdownAggregateDto(double AverageLength);

public record AttributeAggregateDto(
    int AttributeId,
    string Name,
    string Type,
    bool IsRequired,
    int ResponseCount,
    string Summary,
    NumericAggregateDto? Numeric = null,
    DateAggregateDto? Date = null,
    DateRangeAggregateDto? DateRange = null,
    BooleanAggregateDto? Boolean = null,
    DropdownAggregateDto? Dropdown = null,
    TextAggregateDto? SingleLineText = null,
    MarkdownAggregateDto? MarkdownText = null);

public record PositionAggregatesDto(
    int Id,
    string Title,
    string? Company,
    string? Level,
    string ShortDescription,
    int PublishedCvCount,
    DateTime GeneratedAt,
    IReadOnlyList<AttributeAggregateDto> Attributes);

public record PositionSummaryDto(
    int Id,
    string Title,
    string? Company,
    string? Level,
    string ShortDescription,
    int PublishedCvCount,
    DateTime GeneratedAt);
