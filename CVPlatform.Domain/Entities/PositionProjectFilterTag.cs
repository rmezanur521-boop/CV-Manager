namespace CVPlatform.Domain.Entities;

public class PositionProjectFilterTag
{
    public int PositionId { get; set; }
    public PositionProjectFilter Filter { get; set; } = null!;
    public int TagId { get; set; }
    public TechnologyTag Tag { get; set; } = null!;
}