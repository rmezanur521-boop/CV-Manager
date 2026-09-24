using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using CVPlatform.Web.Localization;
using CVPlatform.Web.Services;

namespace CVPlatform.Web.Extensions;

public static class HtmlHelperLocalizationExtensions
{
    public static string T(this IHtmlHelper html, string key)
    {
        var culture = System.Globalization.CultureInfo.CurrentUICulture.Name;
        return UiStrings.Get(culture, key);
    }

    public static IHtmlContent Markdown(this IHtmlHelper html, string? markdown)
    {
        return new HtmlString(MarkdownRenderer.ToHtml(markdown));
    }
}