using CVPlatform.Application.Search;
using CVPlatform.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CVPlatform.Web.Controllers;

[Authorize]
public class SearchController : Controller
{
    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    [HttpGet]
    public async Task<IActionResult> Quick(string term)
    {
        var userId = User.GetUserId();
        var isRecruiterOrAdmin = User.IsInRole("Recruiter") || User.IsInRole("Administrator");

        var results = await _searchService.SearchAsync(userId, isRecruiterOrAdmin, term ?? string.Empty);
        return Json(results);
    }
}