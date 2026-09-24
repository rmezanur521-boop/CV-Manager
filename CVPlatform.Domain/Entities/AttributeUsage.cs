namespace CVPlatform.Domain.Entities;

public class AttributeUsage
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int AttributeId { get; set; }
    public AttributeDefinition Attribute { get; set; } = null!;
    public DateTime LastUsedAt { get; set; }
}