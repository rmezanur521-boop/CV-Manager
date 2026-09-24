using CVPlatform.Application.Attributes;
using CVPlatform.Application.Common.Exceptions;
using CVPlatform.Domain.Constants;
using CVPlatform.Web.Models.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CVPlatform.Web.Controllers;

[Authorize(Roles = $"{Roles.Recruiter},{Roles.Administrator}")]
public class AttributesController : Controller
{
    private readonly IAttributeService _attributeService;

    public AttributesController(IAttributeService attributeService)
    {
        _attributeService = attributeService;
    }

    public async Task<IActionResult> Index(int? categoryId, string? prefix)
    {
        var attributes = await _attributeService.GetAllAsync(categoryId, prefix);

        ViewBag.Categories = (await _attributeService.GetCategoriesAsync())
            .Select(c => new SelectListItem(c.Name, c.Id.ToString(), c.Id == categoryId))
            .ToList();
        ViewBag.CategoryId = categoryId;
        ViewBag.Prefix = prefix;

        return View(attributes);
    }

    public async Task<IActionResult> Create()
    {
        var vm = new AttributeFormViewModel { Categories = await GetCategorySelectListAsync() };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AttributeFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Categories = await GetCategorySelectListAsync();
            return View(vm);
        }

        var request = new CreateAttributeRequest
        {
            Name = vm.Name,
            Description = vm.Description,
            Type = vm.Type,
            CategoryId = vm.CategoryId,
            Options = (vm.OptionsCsv ?? string.Empty)
         .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
         .ToList()
        };

        try
        {
            await _attributeService.CreateAsync(request);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(vm.OptionsCsv), ex.Message);
            vm.Categories = await GetCategorySelectListAsync();
            return View(vm);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var attribute = await _attributeService.GetByIdAsync(id);
        var vm = new AttributeFormViewModel
        {
            Id = attribute.Id,
            Name = attribute.Name,
            Description = attribute.Description,
            Type = attribute.Type,
            CategoryId = attribute.CategoryId,
            Version = attribute.Version,
            OptionsCsv = string.Join(", ", attribute.Options.Select(o => o.Value)),
            Categories = await GetCategorySelectListAsync()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AttributeFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Categories = await GetCategorySelectListAsync();
            return View(vm);
        }

        var request = new UpdateAttributeRequest
        {
            Id = vm.Id,
            Name = vm.Name,
            Description = vm.Description,
            Type = vm.Type,
            CategoryId = vm.CategoryId,
            Version = vm.Version,
            Options = (vm.OptionsCsv ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToList()
        };

        try
        {
            await _attributeService.UpdateAsync(request);
        }
        catch (Exception ex) when (ex is ConcurrencyConflictException or InvalidOperationException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            vm.Categories = await GetCategorySelectListAsync();
            return View(vm);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _attributeService.DeleteAsync(id);
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotFoundException)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<List<SelectListItem>> GetCategorySelectListAsync()
    {
        var categories = await _attributeService.GetCategoriesAsync();
        return categories.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
    }
}