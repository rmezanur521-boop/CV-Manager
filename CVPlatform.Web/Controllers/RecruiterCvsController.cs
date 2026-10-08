using CVPlatform.Application.Common.Exceptions;
using CVPlatform.Application.Cvs;
using CVPlatform.Application.Positions;
using CVPlatform.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using CVPlatform.Web.Extensions;
using CVPlatform.Web.Models.Cvs;

namespace CVPlatform.Web.Controllers;

[Authorize(Roles = $"{Roles.Recruiter},{Roles.Administrator}")]
public class RecruiterCvsController : Controller
{
    private readonly IRecruiterCvService _recruiterCvService;
    private readonly IPositionService _positionService;
    private readonly ICvService _cvService;

    public RecruiterCvsController(IRecruiterCvService recruiterCvService, IPositionService positionService, ICvService cvService)
    {
        _recruiterCvService = recruiterCvService;
        _positionService = positionService;
        _cvService = cvService;
    }

    public async Task<IActionResult> Index(int? positionId, string? q, string sortBy = "PublishedAt", bool desc = true, int page = 1)
    {
        var recruiterId = User.GetUserId();

        var request = new CvSearchRequest
        {
            PositionId = positionId,
            SearchText = q,
            SortBy = sortBy,
            SortDescending = desc,
            Page = page,
            PageSize = 20
        };

        var result = await _recruiterCvService.SearchAsync(recruiterId, request);

        var positions = await _positionService.GetLookupAsync();
        ViewBag.Positions = positions
            .Select(p => new SelectListItem(p.Title, p.Id.ToString(), p.Id == positionId))
            .ToList();
        ViewBag.PositionId = positionId;
        ViewBag.Query = q;
        ViewBag.SortBy = sortBy;
        ViewBag.Desc = desc;

        return View(result);
    }

    public async Task<IActionResult> View(int id)
    {
        var recruiterId = User.GetUserId();

        try
        {
            var cv = await _cvService.GetPublishedForRecruiterAsync(id);
            var header = await _recruiterCvService.GetHeaderAsync(recruiterId, id);

            return View(new RecruiterCvDetailsViewModel(header, cv));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> ToggleLike(int cvId)
    {
        var recruiterId = User.GetUserId();
        var newCount = await _recruiterCvService.ToggleLikeAsync(recruiterId, cvId);
        return Json(new { likesCount = newCount });
    }
}