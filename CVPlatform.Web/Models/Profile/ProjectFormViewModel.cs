using System.ComponentModel.DataAnnotations;

namespace CVPlatform.Web.Models.Profile;

public class ProjectFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    public string DescriptionMarkdown { get; set; } = string.Empty;

    public string TagsCsv { get; set; } = string.Empty;

    public int Version { get; set; }
}