namespace CVPlatform.Application.Positions;

public record PositionLookupDto(int Id, string Title);

public interface IPositionService
{
    Task<IReadOnlyList<PositionDto>> GetAllAsync(string? search = null);
    Task<IReadOnlyList<PositionLookupDto>> GetLookupAsync();
    Task<PositionDto> GetByIdAsync(int id);
    Task<PositionDto> CreateAsync(SavePositionRequest request);
    Task<PositionDto> UpdateAsync(SavePositionRequest request);
    Task DeleteAsync(int id);
    Task<PositionDto> DuplicateAsync(int id);
}