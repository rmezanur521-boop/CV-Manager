namespace CVPlatform.Application.Search;

public interface ISearchService
{
    Task<IReadOnlyList<SearchResultDto>> SearchAsync(string userId, bool isRecruiterOrAdmin, string term);
}