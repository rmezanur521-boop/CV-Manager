using System.ComponentModel.DataAnnotations;
using CVPlatform.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CVPlatform.Web.Models.Attributes;

public class AttributeFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public AttributeType Type { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public int Version { get; set; }

    public string? OptionsCsv { get; set; }

    public List<SelectListItem> Categories { get; set; } = new();
}