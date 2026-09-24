namespace CVPlatform.Domain.Entities;

public class AttributeOption
{
    public int Id { get; set; }
    public int AttributeId { get; set; }
    public AttributeDefinition Attribute { get; set; } = null!;
    public string Value { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}