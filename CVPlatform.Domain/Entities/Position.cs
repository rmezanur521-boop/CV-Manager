using CVPlatform.Domain.Common;
using CVPlatform.Domain.Enums;

namespace CVPlatform.Domain.Entities;

public class Position : IVersionedEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string? Company { get; set; }
    public PositionLevel? Level { get; set; }
    public AccessMode AccessMode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int Version { get; set; }

    public ICollection<PositionAttribute> PositionAttributes { get; set; } = new List<PositionAttribute>();
    public ICollection<PositionAccessRule> AccessRules { get; set; } = new List<PositionAccessRule>();
    public PositionProjectFilter? ProjectFilter { get; set; }
}