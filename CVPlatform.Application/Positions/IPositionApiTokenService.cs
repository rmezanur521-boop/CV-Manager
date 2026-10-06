namespace CVPlatform.Application.Positions;

public interface IPositionApiTokenService
{
    Task<GeneratedPositionTokenDto> GenerateAsync(int positionId, string userId);
    Task RevokeAsync(int positionId);
    Task<PositionApiTokenStatusDto> GetStatusAsync(int positionId);
    Task<int?> ValidateAsync(string plainToken);
    Task RecordUsageAsync(int positionId);
}
