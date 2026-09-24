namespace CVPlatform.Application.Cvs;

public interface ICvService
{
    Task<IReadOnlyList<CvSummaryDto>> GetMyCvsAsync(string candidateId);
    Task<IReadOnlyList<AvailablePositionDto>> GetAvailablePositionsAsync(string candidateId);
    Task<GeneratedCvDto> CreateAsync(string candidateId, int positionId);
    Task<GeneratedCvDto> GetGeneratedAsync(string candidateId, int cvId);
    Task<CvAttributeDto> SetAttributeValueAsync(string candidateId, int cvId, SetCvAttributeValueRequest request);
    Task PublishAsync(string candidateId, int cvId, int version);
    Task DeleteAsync(string candidateId, int cvId);
    Task<GeneratedCvDto> GetPublishedForRecruiterAsync(int cvId);
}