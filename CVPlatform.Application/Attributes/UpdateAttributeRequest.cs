using CVPlatform.Domain.Enums;

namespace CVPlatform.Application.Attributes;

public class UpdateAttributeRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AttributeType Type { get; set; }
    public int CategoryId { get; set; }
    public int Version { get; set; }
    public List<string> Options { get; set; } = new();
}