using CVPlatform.Application.Positions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CVPlatform.Web.Controllers.Api;

[ApiController]
[Route("api/external/v1/position")]
[AllowAnonymous]
[EnableRateLimiting("ExternalApi")]
public class ExternalPositionApiController : ControllerBase
{
    private readonly IPositionApiTokenService _tokenService;
    private readonly IPositionAggregateService _aggregateService;

    public ExternalPositionApiController(
        IPositionApiTokenService tokenService,
        IPositionAggregateService aggregateService)
    {
        _tokenService = tokenService;
        _aggregateService = aggregateService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPosition()
    {
        Response.Headers.CacheControl = "no-store";

        var token = ExtractToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Unauthorized(new { error = "Missing API token." });
        }

        var positionId = await _tokenService.ValidateAsync(token);
        if (!positionId.HasValue)
        {
            return Unauthorized(new { error = "Invalid or revoked API token." });
        }

        var summary = await _aggregateService.GetSummaryAsync(positionId.Value);
        if (summary == null)
        {
            return NotFound(new { error = "Position not found." });
        }

        await _tokenService.RecordUsageAsync(positionId.Value);

        return Ok(summary);
    }

    [HttpGet("aggregates")]
    public async Task<IActionResult> GetAggregates()
    {
        Response.Headers.CacheControl = "no-store";

        var token = ExtractToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Unauthorized(new { error = "Missing API token." });
        }

        var positionId = await _tokenService.ValidateAsync(token);
        if (!positionId.HasValue)
        {
            return Unauthorized(new { error = "Invalid or revoked API token." });
        }

        var aggregates = await _aggregateService.GetAggregatesAsync(positionId.Value);
        if (aggregates == null)
        {
            return NotFound(new { error = "Position not found." });
        }

        await _tokenService.RecordUsageAsync(positionId.Value);

        return Ok(aggregates);
    }

    private string? ExtractToken()
    {
        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var headerValue = authHeader.ToString().Trim();
            if (headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return headerValue.Substring("Bearer ".Length).Trim();
            }
        }

        if (Request.Headers.TryGetValue("X-Api-Token", out var customHeader))
        {
            var headerValue = customHeader.ToString().Trim();
            if (!string.IsNullOrEmpty(headerValue))
            {
                return headerValue;
            }
        }

        return null;
    }
}
