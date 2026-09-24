using CVPlatform.Domain.Common;
using CVPlatform.Domain.Enums;

namespace CVPlatform.Domain.Entities;

public class AttributeDefinition : IVersionedEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AttributeType Type { get; set; }
    public int CategoryId { get; set; }
    public AttributeCategory Category { get; set; } = null!;
    public bool IsBuiltIn { get; set; }
    public int Version { get; set; }
    public ICollection<AttributeOption> Options { get; set; } = new List<AttributeOption>();
}