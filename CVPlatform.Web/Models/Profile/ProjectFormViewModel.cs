using System.ComponentModel.DataAnnotations;

namespace CVPlatform.Web.Models.Profile;

public class ProjectFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Project name is required"), StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start date is required")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    public string? DescriptionMarkdown { get; set; }

    public string? TagsCsv { get; set; }

    public int Version { get; set; }
}
