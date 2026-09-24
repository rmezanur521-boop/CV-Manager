using CVPlatform.Domain.Enums;

namespace CVPlatform.Application.Cvs;

public record AttributeColumnValueDto(int AttributeId, string DisplayValue, bool IsMissing);

public record PositionAttributeColumnDto(int AttributeId, string AttributeName);

public record RecruiterCvListItemDto(
    int CvId,
    string CandidateId,
    string CandidateName,
    int PositionId,
    string PositionTitle,
    CvStatus Status,
    DateTime? PublishedAt,
    int LikesCount,
    bool LikedByCurrentUser,
    IReadOnlyDictionary<int, AttributeColumnValueDto> AttributeValues);

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public record CvSearchResultDto(
    IReadOnlyList<PositionAttributeColumnDto> Columns,
    PagedResult<RecruiterCvListItemDto> Cvs);

public class CvSearchRequest
{
    public int? PositionId { get; set; }
    public string? SearchText { get; set; }
    public string SortBy { get; set; } = "PublishedAt";
    public bool SortDescending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}