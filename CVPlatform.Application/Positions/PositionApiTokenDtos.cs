namespace CVPlatform.Application.Positions;

public record PositionApiTokenStatusDto(
    bool HasToken,
    string? TokenPrefix,
    DateTime? CreatedAt,
    DateTime? LastUsedAt,
    bool IsRevoked,
    DateTime? RevokedAt);

public record GeneratedPositionTokenDto(
    string PlainToken,
    string TokenPrefix,
    DateTime CreatedAt);
