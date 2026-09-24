namespace CVPlatform.Application.Cvs;

public interface IRecruiterCvService
{
    Task<CvSearchResultDto> SearchAsync(string recruiterId, CvSearchRequest request);
    Task<int> ToggleLikeAsync(string recruiterId, int cvId);
    Task<RecruiterCvHeaderDto> GetHeaderAsync(string recruiterId, int cvId);
}