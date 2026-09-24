using CVPlatform.Domain.Enums;

namespace CVPlatform.Application.Cvs;

public record RecruiterCvHeaderDto(
    int CvId,
    string CandidateName,
    int PositionId,
    string PositionTitle,
    CvStatus Status,
    DateTime? PublishedAt,
    int LikesCount,
    bool LikedByCurrentUser);