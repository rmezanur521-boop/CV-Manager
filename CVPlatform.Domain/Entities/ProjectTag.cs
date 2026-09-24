namespace CVPlatform.Domain.Entities;

public class ProjectTag
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public int TagId { get; set; }
    public TechnologyTag Tag { get; set; } = null!;
}