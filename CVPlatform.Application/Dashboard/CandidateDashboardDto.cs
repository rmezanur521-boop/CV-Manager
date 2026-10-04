using CVPlatform.Domain.Enums;

namespace CVPlatform.Application.Dashboard;

public record CandidateKpisDto(
    int TotalCvs,
    int PublishedCvs,
    int DraftCvs,
    int TotalProjects,
    int ProfileCompletenessPercentage);

public record MissingCompletenessItemDto(
    string ItemKey,
    string NavigationAction,
    string NavigationController);

public record CandidateCvStatusItemDto(
    int CvId,
    int PositionId,
    string PositionTitle,
    CvStatus Status,
    DateTime CreatedAt,
    DateTime? PublishedAt,
    int MissingRequiredAttributesCount);

public record AvailablePositionToApplyDto(
    int PositionId,
    string Title,
    string? Company,
    PositionLevel? Level);

public record CandidateCrmStatusDto(
    bool IsSynced,
    string? SalesforceAccountId,
    string? SalesforceContactId,
    DateTime? SyncedAt);

public record CandidateDashboardDto(
    CandidateKpisDto Kpis,
    IReadOnlyList<MissingCompletenessItemDto> MissingItems,
    IReadOnlyList<CandidateCvStatusItemDto> MyCvs,
    IReadOnlyList<AvailablePositionToApplyDto> AvailablePositions,
    int TotalLikesReceived,
    IReadOnlyList<TopTechnologyTagDto> MyTopTags,
    CandidateCrmStatusDto CrmStatus);
