namespace CVPlatform.Application.Users;

public interface IUserManagementService
{
    Task<IReadOnlyList<UserSummaryDto>> GetAllUsersAsync(string? search = null, string? role = null);
    Task AssignRoleAsync(string actingAdminId, AssignRoleRequest request);
    Task SetBlockedAsync(string actingAdminId, string userId, bool blocked);
    Task DeleteUserAsync(string actingAdminId, string userId);
}