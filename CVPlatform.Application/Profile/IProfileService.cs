namespace CVPlatform.Application.Profile;

public interface IProfileService
{
    Task<MeDto> GetMeAsync(string candidateId);
    Task<MeDto> UpdateMeAsync(string candidateId, UpdateMeRequest request);
}