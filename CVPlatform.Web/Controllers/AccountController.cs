using CVPlatform.Application.Common;
using CVPlatform.Domain.Constants;
using CVPlatform.Infrastructure.Identity;
using CVPlatform.Web.Models.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace CVPlatform.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailSender _emailSender;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel vm, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(vm);

        var user = new ApplicationUser
        {
            UserName = vm.Email,
            Email = vm.Email,
            FirstName = vm.FirstName,
            LastName = vm.LastName
        };

        var result = await _userManager.CreateAsync(user, vm.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(vm);
        }

        await _userManager.AddToRoleAsync(user, Roles.Candidate);

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmUrl = Url.Action(nameof(ConfirmEmail), "Account",
            new { userId = user.Id, token }, Request.Scheme)!;

        try
        {
            await _emailSender.SendAsync(
                user.Email,
                "Confirm your CVPlatform account",
                $"Please confirm your account by <a href=\"{HtmlEncoder.Default.Encode(confirmUrl)}\">clicking here</a>.");
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "Your account was created, but the confirmation email could not be sent. Please try again later or contact an administrator.");
            return View(vm);
        }

        return RedirectToAction(nameof(RegisterConfirmation));
    }

    [HttpGet]
    public IActionResult RegisterConfirmation() => View();

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        if (userId is null || token is null)
            return RedirectToAction(nameof(Login));

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return View("ConfirmEmailFailed");

        var result = await _userManager.ConfirmEmailAsync(user, token);
        return View(result.Succeeded ? "ConfirmEmailSuccess" : "ConfirmEmailFailed");
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(vm);

        var result = await _signInManager.PasswordSignInAsync(vm.Email, vm.Password, vm.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
            return LocalRedirectOrHome(returnUrl);

        if (result.IsNotAllowed)
        {
            ModelState.AddModelError(string.Empty, "Please confirm your email before logging in.");
            return View(vm);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Your account has been blocked. Please contact an administrator.");
            return View(vm);
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(vm);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        if (remoteError is not null)
        {
            ModelState.AddModelError(string.Empty, $"External provider error: {remoteError}");
            return View(nameof(Login), new LoginViewModel());
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null)
            return RedirectToAction(nameof(Login));

        var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
        if (signInResult.Succeeded)
            return LocalRedirectOrHome(returnUrl);

        if (signInResult.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Your account has been blocked. Please contact an administrator.");
            return View(nameof(Login), new LoginViewModel());
        }

        var email = info.Principal.FindFirstValue(System.Security.Claims.ClaimTypes.Email);
        if (email is null)
        {
            ModelState.AddModelError(string.Empty, "Email was not provided by the external provider.");
            return View(nameof(Login), new LoginViewModel());
        }

        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser is null)
        {
            existingUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = info.Principal.FindFirstValue(System.Security.Claims.ClaimTypes.GivenName) ?? string.Empty,
                LastName = info.Principal.FindFirstValue(System.Security.Claims.ClaimTypes.Surname) ?? string.Empty,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(existingUser);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View(nameof(Login), new LoginViewModel());
            }

            await _userManager.AddToRoleAsync(existingUser, Roles.Candidate);
        }

        if (await _userManager.IsLockedOutAsync(existingUser))
        {
            ModelState.AddModelError(string.Empty, "Your account has been blocked. Please contact an administrator.");
            return View(nameof(Login), new LoginViewModel());
        }

        await _userManager.AddLoginAsync(existingUser, info);
        await _signInManager.SignInAsync(existingUser, isPersistent: false);

        return LocalRedirectOrHome(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    private IActionResult LocalRedirectOrHome(string? returnUrl)
    {
        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction("Index", "Home");
    }
}