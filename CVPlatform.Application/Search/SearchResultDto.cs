namespace CVPlatform.Application.Search;

public enum SearchResultType
{
    Position,
    Cv,
    Tag
}

public record SearchResultDto(SearchResultType Type, int Id, string Title, string Subtitle, string Url);