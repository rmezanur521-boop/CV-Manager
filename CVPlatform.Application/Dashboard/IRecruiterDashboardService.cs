namespace CVPlatform.Application.Dashboard;

public interface IRecruiterDashboardService
{
    Task<RecruiterDashboardDto> GetDashboardAsync(string recruiterId);
}
