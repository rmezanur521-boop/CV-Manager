using System.Globalization;
using CVPlatform.Application.Common.Exceptions;
using CVPlatform.Application.Discussions;
using CVPlatform.Application.Positions;
using CVPlatform.Web.Extensions;
using CVPlatform.Web.Hubs;
using CVPlatform.Web.Models.Discussions;
using CVPlatform.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace CVPlatform.Web.Controllers;

[Authorize]
public class DiscussionsController : Controller
{
    private readonly IDiscussionService _discussionService;
    private readonly IPositionService _positionService;
    private readonly IHubContext<DiscussionHub> _hubContext;

    public DiscussionsController(
        IDiscussionService discussionService,
        IPositionService positionService,
        IHubContext<DiscussionHub> hubContext)
    {
        _discussionService = discussionService;
        _positionService = positionService;
        _hubContext = hubContext;
    }

    public async Task<IActionResult> Index(int positionId)
    {
        try
        {
            var position = await _positionService.GetByIdAsync(positionId);
            var posts = await _discussionService.GetPostsAsync(positionId);

            return View(new DiscussionPageViewModel(position, posts));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    public async Task<IActionResult> Since(int positionId, int afterId)
    {
        var posts = await _discussionService.GetPostsAfterAsync(positionId, afterId);
        return Json(posts.Select(ToClientPost));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Post(int positionId, string? content)
    {
        try
        {
            var post = await _discussionService.AddPostAsync(positionId, User.GetUserId(), content);
            var clientPost = ToClientPost(post);

            await _hubContext.Clients
                .Group(DiscussionHub.GroupName(positionId))
                .SendAsync("ReceivePost", clientPost);

            return Json(clientPost);
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotFoundException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static object ToClientPost(DiscussionPostDto post)
    {
        return new
        {
            id = post.Id,
            authorId = post.AuthorId,
            authorName = post.AuthorName,
            contentHtml = MarkdownRenderer.ToHtml(post.ContentMarkdown),
            createdAt = post.CreatedAt.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)
        };
    }
}