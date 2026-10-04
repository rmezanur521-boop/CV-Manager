using CVPlatform.Application.PublicSite;
using CVPlatform.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CVPlatform.Web.Controllers;

[AllowAnonymous]
public class HomeController : Controller
{
    private readonly IPublicSiteService _publicSiteService;

    public HomeController(IPublicSiteService publicSiteService)
    {
        _publicSiteService = publicSiteService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var data = await _publicSiteService.GetLandingPageDataAsync();
        return View(data);
    }

    [HttpGet]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}