using CVPlatform.Domain.Common;

namespace CVPlatform.Domain.Entities;

public class CandidateAttributeValue : IVersionedEntity
{
    public int Id { get; set; }
    public string CandidateId { get; set; } = string.Empty;
    public int AttributeId { get; set; }
    public AttributeDefinition Attribute { get; set; } = null!;
    public string? TextValue { get; set; }
    public decimal? NumericValue { get; set; }
    public DateTime? DateValue { get; set; }
    public DateTime? DateRangeStart { get; set; }
    public DateTime? DateRangeEnd { get; set; }
    public bool? BooleanValue { get; set; }
    public int? SelectedOptionId { get; set; }
    public AttributeOption? SelectedOption { get; set; }
    public int Version { get; set; }
}