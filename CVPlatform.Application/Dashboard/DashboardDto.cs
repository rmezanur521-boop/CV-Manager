namespace CVPlatform.Application.Dashboard;

public record LatestPositionDto(int Id, string Title, string? Company, DateTime CreatedAt);

public record PopularPositionDto(int Id, string Title, int CvCount);

public record TagCloudItemDto(string TagName, int UsageCount);

public record DashboardStatsDto(
    int CvsCreatedLast24Hours,
    int TotalPositions,
    int TotalCandidates,
    int TotalRecruiters,
    int TotalSubmittedCvs);

public record DashboardDto(
    IReadOnlyList<LatestPositionDto> LatestPositions,
    IReadOnlyList<PopularPositionDto> PopularPositions,
    IReadOnlyList<TagCloudItemDto> TagCloud,
    DashboardStatsDto Stats);