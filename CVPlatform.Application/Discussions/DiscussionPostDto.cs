namespace CVPlatform.Application.Discussions;

public record DiscussionPostDto(
    int Id,
    int PositionId,
    string AuthorId,
    string AuthorName,
    string ContentMarkdown,
    DateTime CreatedAt);