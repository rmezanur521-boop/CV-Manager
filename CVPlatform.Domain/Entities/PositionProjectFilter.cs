namespace CVPlatform.Domain.Entities;

public class PositionProjectFilter
{
    public int PositionId { get; set; }
    public Position Position { get; set; } = null!;
    public int MaxProjects { get; set; }
    public ICollection<PositionProjectFilterTag> RequiredTags { get; set; } = new List<PositionProjectFilterTag>();
}