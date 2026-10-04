using CVPlatform.Application.Attributes;
using CVPlatform.Application.Common;
using CVPlatform.Application.Common.Exceptions;
using CVPlatform.Application.Crm;
using CVPlatform.Application.Cvs;
using CVPlatform.Application.Profile;
using CVPlatform.Application.Projects;
using CVPlatform.Domain.Enums;
using CVPlatform.Web.Extensions;
using CVPlatform.Web.Localization;
using CVPlatform.Web.Models.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CVPlatform.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly IProfileService _profileService;
    private readonly ICandidateAttributeValueService _valueService;
    private readonly IAttributeService _attributeService;
    private readonly IProjectService _projectService;
    private readonly ICvService _cvService;
    private readonly ICurrentUserContext _currentUser;
    private readonly IFileStorageService _fileStorageService;
    private readonly ISalesforceService _salesforceService;

    public ProfileController(
        IProfileService profileService,
        ICandidateAttributeValueService valueService,
        IAttributeService attributeService,
        IProjectService projectService,
        ICvService cvService,
        ICurrentUserContext currentUser,
        IFileStorageService fileStorageService,
        ISalesforceService salesforceService)
    {
        _profileService = profileService;
        _valueService = valueService;
        _attributeService = attributeService;
        _projectService = projectService;
        _cvService = cvService;
        _fileStorageService = fileStorageService;
        _currentUser = currentUser;
        _salesforceService = salesforceService;
    }

    private async Task<ProfileSummaryViewModel> BuildSummaryAsync(string targetId)
    {
        var me = await _profileService.GetMeAsync(targetId);
        var values = await _valueService.GetMyValuesAsync(targetId);
        var cvs = await _cvService.GetMyCvsAsync(targetId);

        var filled = values.Count(v =>
            !string.IsNullOrWhiteSpace(v.TextValue) ||
            v.NumericValue.HasValue ||
            v.DateValue.HasValue ||
            v.DateRangeStart.HasValue ||
            v.BooleanValue.HasValue ||
            v.SelectedOptionId.HasValue);

        return new ProfileSummaryViewModel
        {
            FullName = $"{me.FirstName} {me.LastName}".Trim(),
            Location = me.Location,
            PhotoUrl = ResolveImageUrl(me.PhotoUrl),
            TotalCvs = cvs.Count,
            PublishedCvs = cvs.Count(c => c.Status == CvStatus.Published),
            AttributesFilled = filled,
            AttributesTotal = values.Count,
            TargetId = targetId,
            IsAdminViewingOther = targetId != User.GetUserId()
        };
    }

    private IActionResult? EnsureAccessAllowed(string targetCandidateId)
    {
        if (targetCandidateId == _currentUser.UserId || _currentUser.IsAdmin)
            return null;

        return Forbid();
    }

    public async Task<IActionResult> Index(string? id)
    {
        var targetId = id ?? User.GetUserId();

        var guard = EnsureAccessAllowed(targetId);
        if (guard is not null) return guard;

        var me = await _profileService.GetMeAsync(targetId);
        ViewBag.TargetId = targetId;
        ViewBag.IsAdminViewingOther = targetId != User.GetUserId();
        ViewBag.Summary = await BuildSummaryAsync(targetId);

        return View(new MeViewModel
        {
            FirstName = me.FirstName,
            LastName = me.LastName,
            Location = me.Location,
            PhotoUrl = me.PhotoUrl,
            ConcurrencyStamp = me.ConcurrencyStamp
        });
    }

    [HttpGet]
    public async Task<IActionResult> ViewProfile(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return RedirectToAction(nameof(Index));

        if (id == User.GetUserId())
            return RedirectToAction(nameof(Index));

        try
        {
            var me = await _profileService.GetMeAsync(id);
            var values = await _valueService.GetMyValuesAsync(id);
            var projects = await _projectService.GetMyProjectsAsync(id);
            var cvs = await _cvService.GetMyCvsAsync(id);

            ViewBag.Me = me;
            ViewBag.Values = values;
            ViewBag.Projects = projects;
            ViewBag.Cvs = cvs.Where(c => c.Status == CvStatus.Published).ToList();
            ViewBag.PhotoUrl = ResolveImageUrl(me.PhotoUrl);

            return View("ViewProfile");
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> SaveMe([FromBody] MeViewModel vm, string? id)
    {
        var targetId = id ?? User.GetUserId();

        var guard = EnsureAccessAllowed(targetId);
        if (guard is not null) return guard;

        try
        {
            var request = new UpdateMeRequest
            {
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                Location = vm.Location,
                PhotoUrl = vm.PhotoUrl,
                ConcurrencyStamp = vm.ConcurrencyStamp
            };
            var updated = await _profileService.UpdateMeAsync(targetId, request);
            return Json(new { success = true, concurrencyStamp = updated.ConcurrencyStamp });
        }
        catch (ConcurrencyConflictException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> CrmSync(string? id)
    {
        var targetId = id ?? User.GetUserId();

        var guard = EnsureAccessAllowed(targetId);
        if (guard is not null) return guard;

        var me = await _profileService.GetMeAsync(targetId);
        var latestSync = await _salesforceService.GetLatestSyncAsync(targetId);

        ViewBag.TargetId = targetId;
        ViewBag.IsAdminViewingOther = targetId != User.GetUserId();
        ViewBag.Summary = await BuildSummaryAsync(targetId);

        var vm = new CrmSyncViewModel
        {
            TargetId = targetId,
            FirstName = me.FirstName,
            LastName = me.LastName,
            Email = me.Email ?? string.Empty,
            Company = latestSync?.Company,
            JobTitle = latestSync?.JobTitle,
            Phone = latestSync?.Phone,
            MarketingOptIn = latestSync?.MarketingOptIn ?? false,
            LastSyncedSalesforceAccountId = latestSync?.SalesforceAccountId,
            LastSyncedSalesforceContactId = latestSync?.SalesforceContactId,
            LastSyncedAt = latestSync?.SyncedAt
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrmSync(CrmSyncViewModel vm, string? id)
    {
        var targetId = id ?? vm.TargetId ?? User.GetUserId();

        var guard = EnsureAccessAllowed(targetId);
        if (guard is not null) return guard;

        if (!ModelState.IsValid)
        {
            var me = await _profileService.GetMeAsync(targetId);
            var latestSync = await _salesforceService.GetLatestSyncAsync(targetId);
            vm.TargetId = targetId;
            vm.FirstName = me.FirstName;
            vm.LastName = me.LastName;
            vm.Email = me.Email ?? string.Empty;
            vm.LastSyncedSalesforceAccountId = latestSync?.SalesforceAccountId;
            vm.LastSyncedSalesforceContactId = latestSync?.SalesforceContactId;
            vm.LastSyncedAt = latestSync?.SyncedAt;
            ViewBag.TargetId = targetId;
            ViewBag.IsAdminViewingOther = targetId != User.GetUserId();
            ViewBag.Summary = await BuildSummaryAsync(targetId);
            return View(vm);
        }

        try
        {
            var codeVerifier = _salesforceService.GenerateCodeVerifier();
            var codeChallenge = _salesforceService.GenerateCodeChallenge(codeVerifier);

            var callbackUrl = ResolveCallbackUrl();
            var stateObj = new
            {
                TargetId = targetId,
                Company = vm.Company,
                JobTitle = vm.JobTitle,
                Phone = vm.Phone,
                MarketingOptIn = vm.MarketingOptIn,
                CodeVerifier = codeVerifier
            };
            var state = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(stateObj)));
            var authUrl = _salesforceService.GetAuthorizationUrl(state, callbackUrl, codeChallenge);
            return Redirect(authUrl);
        }
        catch (Exception ex)
        {
            var me = await _profileService.GetMeAsync(targetId);
            var latestSync = await _salesforceService.GetLatestSyncAsync(targetId);
            vm.TargetId = targetId;
            vm.FirstName = me.FirstName;
            vm.LastName = me.LastName;
            vm.Email = me.Email ?? string.Empty;
            vm.LastSyncedSalesforceAccountId = latestSync?.SalesforceAccountId;
            vm.LastSyncedSalesforceContactId = latestSync?.SalesforceContactId;
            vm.LastSyncedAt = latestSync?.SyncedAt;
            ViewBag.TargetId = targetId;
            ViewBag.IsAdminViewingOther = targetId != User.GetUserId();
            ViewBag.Summary = await BuildSummaryAsync(targetId);

            ModelState.AddModelError(string.Empty, ex.Message);
            return View(vm);
        }
    }

    [HttpGet]
    public async Task<IActionResult> SalesforceCallback(
        string? code,
        string? state,
        string? error,
        [FromQuery(Name = "error_description")] string? errorDescription)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            TempData["Error"] = errorDescription ?? error;
            return RedirectToAction(nameof(CrmSync));
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
        {
            TempData["Error"] = "Authorization code or state was missing.";
            return RedirectToAction(nameof(CrmSync));
        }

        string targetId;
        string? company = null;
        string? jobTitle = null;
        string? phone = null;
        bool marketingOptIn = false;
        string? codeVerifier = null;

        try
        {
            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(state));
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            targetId = doc.RootElement.GetProperty("TargetId").GetString() ?? User.GetUserId();
            if (doc.RootElement.TryGetProperty("Company", out var compProp)) company = compProp.GetString();
            if (doc.RootElement.TryGetProperty("JobTitle", out var jobProp)) jobTitle = jobProp.GetString();
            if (doc.RootElement.TryGetProperty("Phone", out var phoneProp)) phone = phoneProp.GetString();
            if (doc.RootElement.TryGetProperty("MarketingOptIn", out var optProp)) marketingOptIn = optProp.GetBoolean();
            if (doc.RootElement.TryGetProperty("CodeVerifier", out var cvProp)) codeVerifier = cvProp.GetString();
        }
        catch
        {
            TempData["Error"] = "Invalid authentication state.";
            return RedirectToAction(nameof(CrmSync));
        }

        if (string.IsNullOrWhiteSpace(codeVerifier))
        {
            TempData["Error"] = "Invalid authentication state: code verifier missing.";
            return RedirectToAction(nameof(CrmSync));
        }

        var guard = EnsureAccessAllowed(targetId);
        if (guard is not null) return guard;

        try
        {
            var callbackUrl = ResolveCallbackUrl();
            var request = new CrmSyncRequestDto(
                company ?? string.Empty,
                jobTitle ?? string.Empty,
                phone ?? string.Empty,
                marketingOptIn);

            var result = await _salesforceService.SyncWithCodeAsync(targetId, code, callbackUrl, codeVerifier, request);
            TempData["Success"] = string.Format(
                UiStrings.Get(System.Globalization.CultureInfo.CurrentUICulture.Name, "Crm.SyncSuccess"),
                result.SalesforceAccountId,
                result.SalesforceContactId);

            return RedirectToAction(nameof(CrmSync), new { id = targetId == User.GetUserId() ? null : targetId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(CrmSync), new { id = targetId == User.GetUserId() ? null : targetId });
        }
    }

    private string ResolveCallbackUrl()
    {
        return Url.Action(nameof(SalesforceCallback), "Profile", null, Request.Scheme)
            ?? $"{Request.Scheme}://{Request.Host}/Profile/SalesforceCallback";
    }

    public async Task<IActionResult> Info(string? id)
    {
        var targetId = id ?? User.GetUserId();

        var guard = EnsureAccessAllowed(targetId);
        if (guard is not null) return guard;

        var myValues = await _valueService.GetMyValuesAsync(targetId);
        var allAttributes = await _attributeService.GetAllAsync();

        ViewBag.AvailableAttributes = allAttributes
        .Where(a => !a.IsBuiltIn && myValues.All(v => v.AttributeId != a.Id))
        .ToList();
        ViewBag.RecentlyUsed = await _attributeService.GetRecentlyUsedAsync(User.GetUserId());
        ViewBag.TargetId = targetId;
        ViewBag.Summary = await BuildSummaryAsync(targetId);

        var inputs = myValues.Select(v =>
        {
            var attribute = allAttributes.First(a => a.Id == v.AttributeId);
            return new CVPlatform.Web.Models.Shared.AttributeInputViewModel
            {
                AttributeId = v.AttributeId,
                AttributeName = v.AttributeName,
                Type = v.Type,
                IsRequired = false,
                IsMissing = false,
                Version = v.Version,
                TextValue = v.TextValue,
                NumericValue = v.NumericValue,
                DateValue = v.DateValue,
                DateRangeStart = v.DateRangeStart,
                DateRangeEnd = v.DateRangeEnd,
                BooleanValue = v.BooleanValue,
                SelectedOptionId = v.SelectedOptionId,
                ImageDisplayUrl = attribute.Type == AttributeType.Image
                    ? ResolveImageUrl(v.TextValue)
                    : null,
                Options = attribute.Options
                    .Select(o => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(o.Value, o.Id.ToString(), o.Id == v.SelectedOptionId))
                    .ToList()
            };
        }).ToList();

        return View(inputs);
    }

    [HttpPost]
    public async Task<IActionResult> AddAttribute(int attributeId, string? id)
    {
        var targetId = id ?? User.GetUserId();

        var guard = EnsureAccessAllowed(targetId);
        if (guard is not null) return guard;

        await _valueService.AddAttributeAsync(targetId, attributeId);
        await _attributeService.RecordUsageAsync(User.GetUserId(), attributeId);

        return RedirectToAction(nameof(Info), new { id = targetId == User.GetUserId() ? null : targetId });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveAttribute(int attributeId, string? id)
    {
        var targetId = id ?? User.GetUserId();

        var guard = EnsureAccessAllowed(targetId);
        if (guard is not null) return guard;

        await _valueService.RemoveByAttributeIdAsync(targetId, attributeId);
        return RedirectToAction(nameof(Info), new { id = targetId == User.GetUserId() ? null : targetId });
    }
    [HttpPost]
    public async Task<IActionResult> SaveValue([FromBody] SetAttributeValueRequest request, string? id)
    {
        var targetId = id ?? User.GetUserId();

        var guard = EnsureAccessAllowed(targetId);
        if (guard is not null) return guard;

        try
        {
            var updated = await _valueService.SetValueAsync(targetId, request);
            return Json(new { success = true, version = updated.Version });
        }
        catch (Exception ex) when (ex is ConcurrencyConflictException or NotFoundException)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> Projects(string? id)
    {
        var targetId = id ?? User.GetUserId();

        var guard = EnsureAccessAllowed(targetId);
        if (guard is not null) return guard;

        var projects = await _projectService.GetMyProjectsAsync(targetId);
        ViewBag.TargetId = targetId;
        ViewBag.Summary = await BuildSummaryAsync(targetId);
        return View(projects);
    }

    public IActionResult CreateProject(string? id)
    {
        var targetId = id ?? User.GetUserId();

        var guard = EnsureAccessAllowed(targetId);
        if (guard is not null) return guard;

        ViewBag.TargetId = targetId;
        return View(new ProjectFormViewModel { StartDate = DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProject(ProjectFormViewModel vm, string? id)
    {
        var targetId = id ?? User.GetUserId();

        var guard = EnsureAccessAllowed(targetId);
        if (guard is not null) return guard;

        if (!ModelState.IsValid)
        {
            ViewBag.TargetId = targetId;
            return View(vm);
        }

        await _projectService.CreateAsync(targetId, ToRequest(vm));
        return RedirectToAction(nameof(Projects), new { id = targetId == User.GetUserId() ? null : targetId });
    }

    public async Task<IActionResult> EditProject(int id, string? candidateId)
    {
        var targetId = candidateId ?? User.GetUserId();

        var guard = EnsureAccessAllowed(targetId);
        if (guard is not null) return guard;

        var project = await _projectService.GetByIdAsync(targetId, id);
        ViewBag.TargetId = targetId;

        return View(new ProjectFormViewModel
        {
            Id = project.Id,
            Name = project.Name,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            DescriptionMarkdown = project.DescriptionMarkdown,
            TagsCsv = string.Join(", ", project.Tags),
            Version = project.Version
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProject(ProjectFormViewModel vm, string? candidateId)
    {
        var targetId = candidateId ?? User.GetUserId();

        var guard = EnsureAccessAllowed(targetId);
        if (guard is not null) return guard;

        if (!ModelState.IsValid)
        {
            ViewBag.TargetId = targetId;
            return View(vm);
        }

        try
        {
            await _projectService.UpdateAsync(targetId, ToRequest(vm));
        }
        catch (ConcurrencyConflictException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewBag.TargetId = targetId;
            return View(vm);
        }

        return RedirectToAction(nameof(Projects), new { id = targetId == User.GetUserId() ? null : targetId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProject(int id, string? candidateId)
    {
        var targetId = candidateId ?? User.GetUserId();

        var guard = EnsureAccessAllowed(targetId);
        if (guard is not null) return guard;

        await _projectService.DeleteAsync(targetId, id);
        return RedirectToAction(nameof(Projects), new { id = targetId == User.GetUserId() ? null : targetId });
    }

    [HttpGet]
    public async Task<IActionResult> SearchTags(string prefix)
    {
        var tags = await _projectService.SearchTagsAsync(prefix ?? string.Empty);
        return Json(tags);
    }

    private static SaveProjectRequest ToRequest(ProjectFormViewModel vm)
    {
        return new SaveProjectRequest
        {
            Id = vm.Id,
            Name = vm.Name,
            StartDate = vm.StartDate,
            EndDate = vm.EndDate,
            DescriptionMarkdown = vm.DescriptionMarkdown,
            Version = vm.Version,
            Tags = vm.TagsCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList()
        };
    }

    private string? ResolveImageUrl(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null
    : value.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? value
    : _fileStorageService.GetPresignedUrl(value);
}