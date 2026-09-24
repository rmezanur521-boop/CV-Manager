using CVPlatform.Application.Common.Exceptions;
using CVPlatform.Application.Discussions;
using CVPlatform.Domain.Entities;
using CVPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CVPlatform.Infrastructure.Services;

public class DiscussionService : IDiscussionService
{
    private const int MaxPostLength = 4000;
    private const string DeletedUserName = "Deleted User";

    private readonly ApplicationDbContext _db;

    public DiscussionService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<DiscussionPostDto>> GetPostsAsync(int positionId)
    {
        return await QueryPosts(positionId, 0).ToListAsync();
    }

    public async Task<IReadOnlyList<DiscussionPostDto>> GetPostsAfterAsync(int positionId, int afterId)
    {
        return await QueryPosts(positionId, afterId).ToListAsync();
    }

    public async Task<DiscussionPostDto> AddPostAsync(int positionId, string authorId, string? contentMarkdown)
    {
        var content = contentMarkdown?.Trim() ?? string.Empty;

        if (content.Length == 0)
            throw new InvalidOperationException("A post cannot be empty.");

        if (content.Length > MaxPostLength)
            throw new InvalidOperationException($"A post cannot be longer than {MaxPostLength} characters.");

        var positionExists = await _db.Positions.AnyAsync(p => p.Id == positionId);
        if (!positionExists)
            throw new NotFoundException($"Position {positionId} was not found.");

        var post = new DiscussionPost
        {
            PositionId = positionId,
            AuthorId = authorId,
            ContentMarkdown = content,
            CreatedAt = DateTime.UtcNow
        };

        _db.DiscussionPosts.Add(post);
        await _db.SaveChangesAsync();

        var authorName = await _db.Users
            .Where(u => u.Id == authorId)
            .Select(u => u.FirstName + " " + u.LastName)
            .FirstAsync();

        return new DiscussionPostDto(
            post.Id,
            post.PositionId,
            authorId,
            authorName,
            post.ContentMarkdown,
            post.CreatedAt);
    }

    private IQueryable<DiscussionPostDto> QueryPosts(int positionId, int afterId)
    {
        return from post in _db.DiscussionPosts
               where post.PositionId == positionId && post.Id > afterId
               join user in _db.Users on post.AuthorId equals user.Id into authors
               from author in authors.DefaultIfEmpty()
               orderby post.CreatedAt, post.Id
               select new DiscussionPostDto(
                   post.Id,
                   post.PositionId,
                   post.AuthorId ?? string.Empty,
                   author == null ? DeletedUserName : author.FirstName + " " + author.LastName,
                   post.ContentMarkdown,
                   post.CreatedAt);
    }
}