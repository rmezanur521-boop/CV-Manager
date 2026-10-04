namespace CVPlatform.Application.Dashboard;

public record RecruiterKpisDto(
    int OpenPositions,
    int PublishedCvs,
    int NewCvsLast7Days,
    int ShortlistedCount,
    int DraftCvs,
    int PositionsWithZeroCvs);

public record CvTrendPointDto(
    string DateLabel,
    int Count);

public record PositionPipelineDto(
    int PositionId,
    string PositionTitle,
    int PublishedCount,
    int DraftCount,
    int TotalCount);

public record LevelBreakdownDto(
    string LevelName,
    int PositionCount,
    int CvCount);

public record LatestPublishedCvDto(
    int CvId,
    int PositionId,
    string PositionTitle,
    string CandidateId,
    string CandidateName,
    DateTime PublishedAt);

public record RecruiterShortlistItemDto(
    int CvId,
    int PositionId,
    string PositionTitle,
    string CandidateId,
    string CandidateName,
    DateTime LikedAt);

public record TopTechnologyTagDto(
    string TagName,
    int UsageCount);

public record RecentDiscussionDto(
    int PostId,
    int PositionId,
    string PositionTitle,
    string AuthorName,
    string ContentSnippet,
    DateTime CreatedAt);

public record RecruiterDashboardDto(
    RecruiterKpisDto Kpis,
    IReadOnlyList<CvTrendPointDto> Trend,
    IReadOnlyList<PositionPipelineDto> Pipeline,
    IReadOnlyList<LevelBreakdownDto> LevelBreakdown,
    IReadOnlyList<LatestPublishedCvDto> LatestPublishedCvs,
    IReadOnlyList<RecruiterShortlistItemDto> Shortlist,
    IReadOnlyList<TopTechnologyTagDto> TopTags,
    IReadOnlyList<RecentDiscussionDto> RecentDiscussions);
