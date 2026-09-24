namespace CVPlatform.Web.Models.Cvs;

public class SetAttributeValueViewModel
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