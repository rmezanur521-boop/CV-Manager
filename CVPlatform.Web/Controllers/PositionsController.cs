using CVPlatform.Application.Attributes;
using CVPlatform.Application.Common.Exceptions;
using CVPlatform.Application.Positions;
using CVPlatform.Domain.Constants;
using CVPlatform.Domain.Enums;
using CVPlatform.Web.Extensions;
using CVPlatform.Web.Models.Positions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CVPlatform.Web.Controllers;

[Authorize(Roles = $"{Roles.Recruiter},{Roles.Administrator}")]
public class PositionsController : Controller
{
    private readonly IPositionService _positionService;
    private readonly IAttributeService _attributeService;

    public PositionsController(IPositionService positionService, IAttributeService attributeService)
    {
        _positionService = positionService;
        _attributeService = attributeService;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index(string? q)
    {
        var positions = await _positionService.GetAllAsync(q);
        ViewBag.Query = q;
        return View(positions);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var position = await _positionService.GetByIdAsync(id);
            return View(position);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    public async Task<IActionResult> Create()
    {
        var vm = new PositionFormViewModel { AttributeCatalog = await GetAttributeCatalogAsync() };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PositionFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.AttributeCatalog = await GetAttributeCatalogAsync();
            return View(vm);
        }

        await _positionService.CreateAsync(ToRequest(vm));

        foreach (var attributeId in vm.SelectedAttributeIds)
        {
            await _attributeService.RecordUsageAsync(User.GetUserId(), attributeId);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var position = await _positionService.GetByIdAsync(id);
        var vm = new PositionFormViewModel
        {
            Id = position.Id,
            Title = position.Title,
            ShortDescription = position.ShortDescription,
            Company = position.Company,
            Level = position.Level,
            AccessMode = position.AccessMode,
            Version = position.Version,
            SelectedAttributeIds = position.Attributes.Select(a => a.AttributeId).ToList(),
            RequiredAttributeIds = position.Attributes.Where(a => a.IsRequired).Select(a => a.AttributeId).ToList(),
            AccessRules = position.AccessRules.Select(r => new AccessRuleRowViewModel
            {
                AttributeId = r.AttributeId,
                Operator = r.Operator,
                ComparisonValue = r.ComparisonValue,
                ComparisonOptionId = r.ComparisonOptionId
            }).ToList(),
            MaxProjects = position.MaxProjects,
            RequiredProjectTagsCsv = string.Join(", ", position.RequiredProjectTags),
            AttributeCatalog = await GetAttributeCatalogAsync()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PositionFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.AttributeCatalog = await GetAttributeCatalogAsync();
            return View(vm);
        }

        try
        {
            await _positionService.UpdateAsync(ToRequest(vm));
            foreach (var attributeId in vm.SelectedAttributeIds)
            {
                await _attributeService.RecordUsageAsync(User.GetUserId(), attributeId);
            }
        }
        catch (ConcurrencyConflictException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            vm.AttributeCatalog = await GetAttributeCatalogAsync();
            return View(vm);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _positionService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duplicate(int id)
    {
        await _positionService.DuplicateAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<PositionAttributeCatalogItemViewModel>> GetAttributeCatalogAsync()
    {
        var attributes = await _attributeService.GetAllAsync();

        return attributes.Select(a => new PositionAttributeCatalogItemViewModel
        {
            Id = a.Id,
            Name = a.Name,
            CategoryName = a.CategoryName,
            Type = a.Type.ToString(),
            Options = a.Options
                .OrderBy(o => o.DisplayOrder)
                .Select(o => new PositionAttributeOptionViewModel { Id = o.Id, Value = o.Value })
                .ToList()
        }).ToList();
    }

    private static SavePositionRequest ToRequest(PositionFormViewModel vm)
    {
        return new SavePositionRequest
        {
            Id = vm.Id,
            Title = vm.Title,
            ShortDescription = vm.ShortDescription,
            Company = vm.Company,
            Level = vm.Level,
            AccessMode = vm.AccessMode,
            Version = vm.Version,
            AttributeIds = vm.SelectedAttributeIds,
            RequiredAttributeIds = vm.RequiredAttributeIds,
            AccessRules = vm.AccessMode == AccessMode.Restricted
                ? vm.AccessRules.Select(r => new AccessRuleInput
                {
                    AttributeId = r.AttributeId,
                    Operator = r.Operator,
                    ComparisonValue = r.ComparisonValue,
                    ComparisonOptionId = r.ComparisonOptionId
                }).ToList()
                : new List<AccessRuleInput>(),
            MaxProjects = vm.MaxProjects,
            RequiredProjectTags = vm.RequiredProjectTagsCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList()
        };
    }
}