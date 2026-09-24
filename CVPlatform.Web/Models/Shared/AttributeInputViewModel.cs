using CVPlatform.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CVPlatform.Web.Models.Shared;

public class AttributeInputViewModel
{
    public int AttributeId { get; set; }
    public string AttributeName { get; set; } = string.Empty;
    public AttributeType Type { get; set; }
    public bool IsRequired { get; set; }
    public bool IsMissing { get; set; }
    public int Version { get; set; }

    public string? TextValue { get; set; }
    public decimal? NumericValue { get; set; }
    public DateTime? DateValue { get; set; }
    public DateTime? DateRangeStart { get; set; }
    public DateTime? DateRangeEnd { get; set; }
    public bool? BooleanValue { get; set; }
    public int? SelectedOptionId { get; set; }
    public string? ImageDisplayUrl { get; set; }
    public List<SelectListItem> Options { get; set; } = new();
}