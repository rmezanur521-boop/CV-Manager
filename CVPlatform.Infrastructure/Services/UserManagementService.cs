using CVPlatform.Application.Common.Exceptions;
using CVPlatform.Application.Users;
using CVPlatform.Domain.Constants;
using CVPlatform.Infrastructure.Identity;
using CVPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CVPlatform.Infrastructure.Services;

public class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public UserManagementService(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }
    public async Task<IReadOnlyList<UserSummaryDto>> GetAllUsersAsync(string? search = null, string? role = null)
    {
        var query = _db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                (u.FirstName + " " + u.LastName).Contains(term) ||
                (u.Email != null && u.Email.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(u => _db.UserRoles.Any(ur =>
                ur.UserId == u.Id && _db.Roles.Any(r => r.Id == ur.RoleId && r.Name == role)));
        }

        var users = await query
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email, u.EmailConfirmed, u.LockoutEnd })
            .ToListAsync();

        var userIds = users.Select(u => u.Id).ToList();

        var roleRows = await _db.UserRoles
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, RoleName = r.Name })
            .ToListAsync();

        var roleByUser = roleRows
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.First().RoleName ?? string.Empty);

        var now = DateTimeOffset.UtcNow;

        return users
            .Select(u => new UserSummaryDto(
                u.Id,
                $"{u.FirstName} {u.LastName}".Trim(),
                u.Email ?? string.Empty,
                roleByUser.GetValueOrDefault(u.Id, string.Empty),
                u.EmailConfirmed,
                u.LockoutEnd.HasValue && u.LockoutEnd.Value > now))
            .ToList();
    }
    public async Task AssignRoleAsync(string actingAdminId, AssignRoleRequest request)
    {
        if (!Roles.All.Contains(request.NewRole))
            throw new InvalidOperationException("Invalid role.");

        var user = await _userManager.FindByIdAsync(request.UserId)
            ?? throw new NotFoundException("User not found.");

        var currentRoles = await _userManager.GetRolesAsync(user);

        if (currentRoles.Contains(Roles.Administrator) && request.NewRole != Roles.Administrator)
        {
            // Per course project requirement: Administrators may remove their own Administrator role.
            if (request.UserId != actingAdminId)
            {
                await EnsureNotLastAdminAsync("remove the Administrator role from");
            }
        }

        if (currentRoles.Contains(request.NewRole) && currentRoles.Count == 1)
            return;

        if (currentRoles.Count > 0)
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

        await _userManager.AddToRoleAsync(user, request.NewRole);
    }

    public async Task SetBlockedAsync(string actingAdminId, string userId, bool blocked)
    {
        if (blocked && userId == actingAdminId)
            throw new InvalidOperationException("You cannot block your own account.");

        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("User not found.");

        if (blocked)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(Roles.Administrator))
                await EnsureNotLastAdminAsync("block");
        }

        user.LockoutEnabled = true;
        user.LockoutEnd = blocked ? DateTimeOffset.MaxValue : null;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        if (blocked)
            await _userManager.UpdateSecurityStampAsync(user);
    }

    public async Task DeleteUserAsync(string actingAdminId, string userId)
    {
        if (userId == actingAdminId)
            throw new InvalidOperationException("You cannot delete your own account.");

        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("User not found.");

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains(Roles.Administrator))
            await EnsureNotLastAdminAsync("delete");

        await using var transaction = await _db.Database.BeginTransactionAsync();

        await _db.CvLikes.Where(l => l.RecruiterId == userId).ExecuteDeleteAsync();
        await _db.FileAssets.Where(f => f.UploadedByUserId == userId).ExecuteDeleteAsync();

        try
        {
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("This user still has data that prevents deletion.");
        }

        await transaction.CommitAsync();
    }
    private async Task EnsureNotLastAdminAsync(string action)
    {
        var adminCount = (await _userManager.GetUsersInRoleAsync(Roles.Administrator)).Count;
        if (adminCount <= 1)
            throw new InvalidOperationException($"Cannot {action} the last Administrator.");
    }
}