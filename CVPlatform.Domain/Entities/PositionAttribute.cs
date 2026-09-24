namespace CVPlatform.Domain.Entities;

public class PositionAttribute
{
    public int PositionId { get; set; }
    public Position Position { get; set; } = null!;
    public int AttributeId { get; set; }
    public AttributeDefinition Attribute { get; set; } = null!;
    public bool IsRequired { get; set; }
}