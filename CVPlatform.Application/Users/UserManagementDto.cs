namespace CVPlatform.Application.Users;

public record UserSummaryDto(
    string Id,
    string FullName,
    string Email,
    string CurrentRole,
    bool EmailConfirmed,
    bool IsBlocked);

public class AssignRoleRequest
{
    public string UserId { get; set; } = string.Empty;
    public string NewRole { get; set; } = string.Empty;
}