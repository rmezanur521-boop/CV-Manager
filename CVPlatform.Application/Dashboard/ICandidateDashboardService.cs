namespace CVPlatform.Application.Dashboard;

public interface ICandidateDashboardService
{
    Task<CandidateDashboardDto> GetDashboardAsync(string candidateId);
}
