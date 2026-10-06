using System.Text.RegularExpressions;
using CVPlatform.Application.Support;
using CVPlatform.Web.Extensions;
using CVPlatform.Web.Models.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CVPlatform.Web.Controllers;

[Authorize]
[Route("[controller]")]
public class SupportController : Controller
{
    private static readonly Regex PositionPathRegex = new(@"/Positions/(?:Edit|Details|Duplicate)/(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DiscussionPathRegex = new(@"/Discussions(?:/Index)?/(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PositionQueryRegex = new(@"[?&]positionId=(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly ISupportTicketService _ticketService;
    private readonly ILogger<SupportController> _logger;

    public SupportController(
        ISupportTicketService ticketService,
        ILogger<SupportController> logger)
    {
        _ticketService = ticketService;
        _logger = logger;
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("SupportTicket")]
    public async Task<IActionResult> Create([FromForm] SupportTicketViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Invalid ticket submission.";
            return Json(new { success = false, message = firstError });
        }

        var validatedUrl = ValidateAndNormalizePageUrl(model.PageUrl);
        if (validatedUrl == null)
        {
            return Json(new { success = false, message = "Invalid page URL provided." });
        }

        var positionId = model.PositionId;
        if (!positionId.HasValue || positionId.Value <= 0)
        {
            positionId = InferPositionIdFromUrl(validatedUrl);
        }

        var userId = User.GetUserId();
        var request = new SupportTicketRequest(
            model.Summary.Trim(),
            model.Priority,
            validatedUrl,
            positionId);

        var result = await _ticketService.CreateAsync(request, userId, ct);
        if (!result.Success)
        {
            _logger.LogError("Support ticket creation failed: {ErrorCode}", result.ErrorMessage);
            return Json(new { success = false, message = "Failed to submit support ticket. Please try again later." });
        }

        return Json(new
        {
            success = true,
            ticketId = result.TicketId,
            message = "Support ticket submitted successfully."
        });
    }

    private string? ValidateAndNormalizePageUrl(string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        }

        if (rawUrl.StartsWith('/'))
        {
            return $"{Request.Scheme}://{Request.Host}{Request.PathBase}{rawUrl}";
        }

        if (Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri))
        {
            if (string.Equals(uri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase))
            {
                return uri.ToString();
            }
        }

        return null;
    }

    private static int? InferPositionIdFromUrl(string url)
    {
        var match = PositionPathRegex.Match(url);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var posId))
        {
            return posId;
        }

        match = DiscussionPathRegex.Match(url);
        if (match.Success && int.TryParse(match.Groups[1].Value, out posId))
        {
            return posId;
        }

        match = PositionQueryRegex.Match(url);
        if (match.Success && int.TryParse(match.Groups[1].Value, out posId))
        {
            return posId;
        }

        return null;
    }
}
