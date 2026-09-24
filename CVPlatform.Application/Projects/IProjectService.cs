namespace CVPlatform.Application.Projects;

public interface IProjectService
{
    Task<IReadOnlyList<ProjectDto>> GetMyProjectsAsync(string candidateId);
    Task<ProjectDto> GetByIdAsync(string candidateId, int id);
    Task<ProjectDto> CreateAsync(string candidateId, SaveProjectRequest request);
    Task<ProjectDto> UpdateAsync(string candidateId, SaveProjectRequest request);
    Task DeleteAsync(string candidateId, int id);
    Task<IReadOnlyList<string>> SearchTagsAsync(string prefix);
}