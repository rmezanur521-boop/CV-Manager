namespace CVPlatform.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync();
}