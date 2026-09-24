namespace CVPlatform.Domain.Entities;

public class DiscussionPost
{
    public int Id { get; set; }
    public int PositionId { get; set; }
    public Position Position { get; set; } = null!;
    public string? AuthorId { get; set; }
    public string ContentMarkdown { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}