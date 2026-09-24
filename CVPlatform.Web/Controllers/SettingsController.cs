using Microsoft.AspNetCore.Mvc;

namespace CVPlatform.Web.Controllers;

public class SettingsController : Controller
{
    [HttpGet]
    public IActionResult SetTheme(string theme, string returnUrl)
    {
        Response.Cookies.Append("cvplatform-theme", theme, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true
        });

        return LocalRedirect(returnUrl);
    }

    [HttpGet]
    public IActionResult SetLanguage(string culture, string returnUrl)
    {
        Response.Cookies.Append(
            Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.DefaultCookieName,
            Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.MakeCookieValue(
                new Microsoft.AspNetCore.Localization.RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });

        return LocalRedirect(returnUrl);
    }
}