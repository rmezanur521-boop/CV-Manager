using CVPlatform.Application.Common.Exceptions;
using CVPlatform.Application.Users;
using CVPlatform.Domain.Constants;
using CVPlatform.Web.Extensions;
using CVPlatform.Web.Localization;
using CVPlatform.Web.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CVPlatform.Web.Controllers;

[Authorize(Roles = Roles.Administrator)]
public class UsersController : Controller
{
    private readonly IUserManagementService _userManagementService;

    public UsersController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    public async Task<IActionResult> Index(string? q, string? role)
    {
        var users = await _userManagementService.GetAllUsersAsync(q, role);

        ViewBag.AvailableRoles = Roles.All;
        ViewBag.CurrentUserId = User.GetUserId();
        ViewBag.Query = q;
        ViewBag.Role = role;

        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> AssignRole(AssignRoleViewModel vm)
    {
        return ExecuteAsync(
            () => _userManagementService.AssignRoleAsync(
                User.GetUserId(),
                new AssignRoleRequest { UserId = vm.UserId, NewRole = vm.NewRole }),
            "Users.RoleAssigned");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Block(string userId)
    {
        return ExecuteAsync(
            () => _userManagementService.SetBlockedAsync(User.GetUserId(), userId, blocked: true),
            "Users.UserBlocked");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Unblock(string userId)
    {
        return ExecuteAsync(
            () => _userManagementService.SetBlockedAsync(User.GetUserId(), userId, blocked: false),
            "Users.UserUnblocked");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Delete(string userId)
    {
        return ExecuteAsync(
            () => _userManagementService.DeleteUserAsync(User.GetUserId(), userId),
            "Users.UserDeleted");
    }

    private async Task<IActionResult> ExecuteAsync(Func<Task> action, string successKey)
    {
        try
        {
            await action();
            TempData["Success"] = UiStrings.Get(System.Globalization.CultureInfo.CurrentUICulture.Name, successKey);
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotFoundException)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}