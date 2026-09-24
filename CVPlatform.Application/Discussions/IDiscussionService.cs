namespace CVPlatform.Application.Discussions;

public interface IDiscussionService
{
    Task<IReadOnlyList<DiscussionPostDto>> GetPostsAsync(int positionId);
    Task<IReadOnlyList<DiscussionPostDto>> GetPostsAfterAsync(int positionId, int afterId);
    Task<DiscussionPostDto> AddPostAsync(int positionId, string authorId, string? contentMarkdown);
}