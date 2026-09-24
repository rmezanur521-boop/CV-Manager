namespace CVPlatform.Application.Profile;

public interface ICandidateAttributeValueService
{
    Task<IReadOnlyList<CandidateAttributeValueDto>> GetMyValuesAsync(string candidateId);
    Task<CandidateAttributeValueDto> AddAttributeAsync(string candidateId, int attributeId);
    Task RemoveByAttributeIdAsync(string candidateId, int attributeId);
    Task<CandidateAttributeValueDto> SetValueAsync(string candidateId, SetAttributeValueRequest request);
}