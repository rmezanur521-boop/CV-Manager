namespace CVPlatform.Application.Positions;

public interface IPositionAggregateService
{
    Task<PositionAggregatesDto?> GetAggregatesAsync(int positionId);
    Task<PositionSummaryDto?> GetSummaryAsync(int positionId);
}
