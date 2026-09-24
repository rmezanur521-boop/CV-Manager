namespace CVPlatform.Application.Projects;

public record ProjectDto(
    int Id,
    string Name,
    DateTime StartDate,
    DateTime? EndDate,
    string DescriptionMarkdown,
    int Version,
    IReadOnlyList<string> Tags);

public class SaveProjectRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string DescriptionMarkdown { get; set; } = string.Empty;
    public int Version { get; set; }
    public List<string> Tags { get; set; } = new();
}