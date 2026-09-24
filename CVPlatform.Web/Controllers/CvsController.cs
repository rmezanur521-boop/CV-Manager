using CVPlatform.Application.Common.Exceptions;
using CVPlatform.Application.Cvs;
using CVPlatform.Domain.Constants;
using CVPlatform.Web.Extensions;
using CVPlatform.Web.Models.Cvs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CVPlatform.Application.Common;

namespace CVPlatform.Web.Controllers;

[Authorize(Roles = $"{Roles.Candidate},{Roles.Administrator}")]
public class CvsController : Controller
{
    private readonly ICvService _cvService;
    private readonly ICurrentUserContext _currentUser;
    private readonly IFileStorageService _fileStorageService;
    public CvsController(ICvService cvService, ICurrentUserContext currentUser, IFileStorageService fileStorageService)
    {
        _cvService = cvService;
        _currentUser = currentUser;
        _fileStorageService = fileStorageService;
    }
    public async Task<IActionResult> Index(string? candidateId)
    {
        var targetId = candidateId ?? CurrentCandidateId();

        if (targetId != CurrentCandidateId() && !_currentUser.IsAdmin)
            return Forbid();

        var cvs = await _cvService.GetMyCvsAsync(targetId);
        ViewBag.CandidateId = targetId;
        ViewBag.IsAdminViewingOther = targetId != CurrentCandidateId();
        return View(cvs);
    }
    public async Task<IActionResult> Create()
    {
        var candidateId = CurrentCandidateId();
        var positions = await _cvService.GetAvailablePositionsAsync(candidateId);
        return View(positions);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int positionId)
    {
        var candidateId = CurrentCandidateId();

        try
        {
            var cv = await _cvService.CreateAsync(candidateId, positionId);
            return RedirectToAction(nameof(Edit), new { id = cv.CvId });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Create));
        }
    }

    public async Task<IActionResult> Edit(int id, string? candidateId)
    {
        var targetId = candidateId ?? CurrentCandidateId();

        if (targetId != CurrentCandidateId() && !_currentUser.IsAdmin)
            return Forbid();

        var cv = await _cvService.GetGeneratedAsync(targetId, id);
        var summary = (await _cvService.GetMyCvsAsync(targetId)).FirstOrDefault(x => x.Id == id);

        var inputs = new List<CVPlatform.Web.Models.Shared.AttributeInputViewModel>();

        foreach (var attribute in cv.Attributes)
        {
            var vm = new CVPlatform.Web.Models.Shared.AttributeInputViewModel
            {
                AttributeId = attribute.AttributeId,
                AttributeName = attribute.AttributeName,
                Type = attribute.Type,
                IsRequired = attribute.IsRequired,
                IsMissing = attribute.IsMissing,
                Version = attribute.ValueVersion,
                TextValue = attribute.TextValue,
                NumericValue = attribute.NumericValue,
                DateValue = attribute.DateValue,
                DateRangeStart = attribute.DateRangeStart,
                DateRangeEnd = attribute.DateRangeEnd,
                BooleanValue = attribute.BooleanValue,
                SelectedOptionId = attribute.SelectedOptionId,
                ImageDisplayUrl = attribute.Type == CVPlatform.Domain.Enums.AttributeType.Image && !string.IsNullOrWhiteSpace(attribute.TextValue)
                    ? _fileStorageService.GetPresignedUrl(attribute.TextValue)
                    : null,
                Options = attribute.Options
                    .Select(o => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(o.Value, o.Id.ToString(), o.Id == attribute.SelectedOptionId))
                    .ToList()
            };

            inputs.Add(vm);
        }

        ViewBag.CandidateId = targetId;
        ViewBag.CvId = id;

        var viewModel = new CVPlatform.Web.Models.Cvs.CvEditViewModel
        {
            CvId = cv.CvId,
            PositionTitle = cv.PositionTitle,
            Status = cv.Status,
            Version = cv.Version,
            CreatedAt = summary?.CreatedAt,
            PublishedAt = summary?.PublishedAt,
            Attributes = inputs,
            Projects = cv.Projects
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> SaveAttribute([FromBody] SetAttributeValueViewModel vm, int cvId, string? candidateId)
    {
        var targetId = candidateId ?? CurrentCandidateId();

        if (targetId != CurrentCandidateId() && !_currentUser.IsAdmin)
            return Forbid();

        try
        {
            var request = new SetCvAttributeValueRequest
            {
                AttributeId = vm.AttributeId,
                TextValue = vm.TextValue,
                NumericValue = vm.NumericValue,
                DateValue = vm.DateValue,
                DateRangeStart = vm.DateRangeStart,
                DateRangeEnd = vm.DateRangeEnd,
                BooleanValue = vm.BooleanValue,
                SelectedOptionId = vm.SelectedOptionId,
                Version = vm.Version
            };
            var updated = await _cvService.SetAttributeValueAsync(targetId, cvId, request);
            return Json(new { success = true, version = updated.ValueVersion, isMissing = updated.IsMissing });
        }
        catch (ConcurrencyConflictException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id, int version, string? candidateId)
    {
        var targetId = candidateId ?? CurrentCandidateId();

        if (targetId != CurrentCandidateId() && !_currentUser.IsAdmin)
            return Forbid();

        try
        {
            await _cvService.PublishAsync(targetId, id, version);
        }
        catch (Exception ex) when (ex is ConcurrencyConflictException or InvalidOperationException)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Edit), new { id, candidateId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? candidateId)
    {
        var targetId = candidateId ?? CurrentCandidateId();

        if (targetId != CurrentCandidateId() && !_currentUser.IsAdmin)
            return Forbid();

        await _cvService.DeleteAsync(targetId, id);
        return RedirectToAction(nameof(Index));
    }

    private string CurrentCandidateId()
    {
        return User.GetUserId();
    }
}