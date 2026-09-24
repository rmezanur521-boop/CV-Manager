using CVPlatform.Domain.Enums;

namespace CVPlatform.Application.Profile;

public record CandidateAttributeValueDto(
    int ValueId,
    int AttributeId,
    string AttributeName,
    AttributeType Type,
    string? TextValue,
    decimal? NumericValue,
    DateTime? DateValue,
    DateTime? DateRangeStart,
    DateTime? DateRangeEnd,
    bool? BooleanValue,
    int? SelectedOptionId,
    int Version);

public class SetAttributeValueRequest
{
    public int AttributeId { get; set; }
    public string? TextValue { get; set; }
    public decimal? NumericValue { get; set; }
    public DateTime? DateValue { get; set; }
    public DateTime? DateRangeStart { get; set; }
    public DateTime? DateRangeEnd { get; set; }
    public bool? BooleanValue { get; set; }
    public int? SelectedOptionId { get; set; }
    public int Version { get; set; }
}