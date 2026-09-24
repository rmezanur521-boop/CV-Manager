using CVPlatform.Domain.Common;
using CVPlatform.Domain.Enums;

namespace CVPlatform.Domain.Entities;

public class Cv : IVersionedEntity
{
    public int Id { get; set; }
    public string CandidateId { get; set; } = string.Empty;
    public int PositionId { get; set; }
    public Position Position { get; set; } = null!;
    public CvStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int Version { get; set; }
}