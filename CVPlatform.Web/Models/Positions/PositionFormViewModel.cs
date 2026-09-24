using System.ComponentModel.DataAnnotations;
using CVPlatform.Domain.Enums;

namespace CVPlatform.Web.Models.Positions;

public class AccessRuleRowViewModel
{
    public int AttributeId { get; set; }
    public RuleOperator Operator { get; set; }
    public string ComparisonValue { get; set; } = string.Empty;
    public int? ComparisonOptionId { get; set; }
}

public class PositionAttributeOptionViewModel
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
}

public class PositionAttributeCatalogItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<PositionAttributeOptionViewModel> Options { get; set; } = new();
}

public class PositionFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(1000)]
    public string ShortDescription { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Company { get; set; }

    public PositionLevel? Level { get; set; }

    [Required]
    public AccessMode AccessMode { get; set; }

    public int Version { get; set; }

    public List<int> SelectedAttributeIds { get; set; } = new();
    public List<int> RequiredAttributeIds { get; set; } = new();
    public List<AccessRuleRowViewModel> AccessRules { get; set; } = new();

    public int MaxProjects { get; set; }
    public string RequiredProjectTagsCsv { get; set; } = string.Empty;

    public List<PositionAttributeCatalogItemViewModel> AttributeCatalog { get; set; } = new();
}