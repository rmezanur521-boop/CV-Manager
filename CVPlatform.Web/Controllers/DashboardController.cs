using CVPlatform.Application.Dashboard;
using CVPlatform.Domain.Constants;
using CVPlatform.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CVPlatform.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _adminDashboardService;
    private readonly IRecruiterDashboardService _recruiterDashboardService;
    private readonly ICandidateDashboardService _candidateDashboardService;

    public DashboardController(
        IDashboardService adminDashboardService,
        IRecruiterDashboardService recruiterDashboardService,
        ICandidateDashboardService candidateDashboardService)
    {
        _adminDashboardService = adminDashboardService;
        _recruiterDashboardService = recruiterDashboardService;
        _candidateDashboardService = candidateDashboardService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        if (User.IsInRole(Roles.Administrator))
        {
            return RedirectToAction(nameof(Admin));
        }

        if (User.IsInRole(Roles.Recruiter))
        {
            return RedirectToAction(nameof(Recruiter));
        }

        if (User.IsInRole(Roles.Candidate))
        {
            return RedirectToAction(nameof(Candidate));
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> Admin()
    {
        var model = await _adminDashboardService.GetDashboardAsync();
        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = $"{Roles.Recruiter},{Roles.Administrator}")]
    public async Task<IActionResult> Recruiter()
    {
        var model = await _recruiterDashboardService.GetDashboardAsync(User.GetUserId());
        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = Roles.Candidate)]
    public async Task<IActionResult> Candidate()
    {
        var model = await _candidateDashboardService.GetDashboardAsync(User.GetUserId());
        return View(model);
    }
}
