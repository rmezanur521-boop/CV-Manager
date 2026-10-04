namespace CVPlatform.Application.PublicSite;

public interface IPublicSiteService
{
    Task<PublicLandingDto> GetLandingPageDataAsync();
}
