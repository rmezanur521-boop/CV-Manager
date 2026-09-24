using CVPlatform.Domain.Enums;

namespace CVPlatform.Application.Attributes;

public class CreateAttributeRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AttributeType Type { get; set; }
    public int CategoryId { get; set; }
    public List<string> Options { get; set; } = new();
}