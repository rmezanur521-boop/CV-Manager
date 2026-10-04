using CVPlatform.Domain.Enums;

namespace CVPlatform.Application.PublicSite;

public record PublicStatsDto(
    int OpenPositionsCount,
    int CandidatesCount,
    int PublishedCvsCount);

public record PublicPositionTeaserDto(
    int Id,
    string Title,
    string? Company,
    PositionLevel? Level,
    string ShortDescription);

public record PublicLandingDto(
    PublicStatsDto Stats,
    IReadOnlyList<PublicPositionTeaserDto> OpenPositions);
