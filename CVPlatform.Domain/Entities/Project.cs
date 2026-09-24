using CVPlatform.Domain.Common;

namespace CVPlatform.Domain.Entities;

public class Project : IVersionedEntity
{
    public int Id { get; set; }
    public string CandidateId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string DescriptionMarkdown { get; set; } = string.Empty;
    public int Version { get; set; }
    public ICollection<ProjectTag> ProjectTags { get; set; } = new List<ProjectTag>();
}