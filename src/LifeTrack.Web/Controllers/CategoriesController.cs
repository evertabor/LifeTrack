using LifeTrack.Application.Common;
using LifeTrack.Application.Finance.Commands.CreateCategory;
using LifeTrack.Application.Finance.Commands.DeleteCategory;
using LifeTrack.Application.Finance.Commands.EditCategory;
using LifeTrack.Application.Finance.Commands.GetCategory;
using LifeTrack.Application.Finance.Commands.ListCategories;
using LifeTrack.Web.ViewModels.Categories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeTrack.Web.Controllers;

[Route("categories")]
[Authorize]
public sealed class CategoriesController(
    ISender sender,
    ILogger<CategoriesController> logger) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var categories = await sender.Send(new ListCategoriesQuery(), ct);
        return View(CategoryListViewModel.From(categories, TempData["Message"] as string));
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [FromForm] CreateCategoryInputModel input,
        CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await sender.Send(new CreateCategoryCommand(input.Name), ct);
                TempData["Message"] = "Categoría creada.";
                return RedirectToAction(nameof(Index));
            }
            catch (BusinessRuleException ex)
            {
                ModelState.AddModelError("Name", ex.Message);
            }
            catch (ResourceNotFoundException ex)
            {
                logger.LogWarning("Unexpected resource not found during category creation: {Message}", ex.Message);
            }
        }
        return await RenderIndexAsync(input, ct);
    }

    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        try
        {
            var category = await sender.Send(new GetCategoryQuery(id), ct);
            return View(CategoryEditViewModel.From(category));
        }
        catch (ResourceNotFoundException ex)
        {
            logger.LogWarning("Unexpected resource not found opening category edit: {Message}", ex.Message);
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost("{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id,
        [FromForm] EditCategoryInputModel input,
        CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await sender.Send(new EditCategoryCommand(id, input.NewName), ct);
                TempData["Message"] = "Categoría actualizada.";
                return RedirectToAction(nameof(Index));
            }
            catch (BusinessRuleException ex)
            {
                ModelState.AddModelError("NewName", ex.Message);
            }
            catch (ResourceNotFoundException ex)
            {
                logger.LogWarning("Unexpected resource not found during category edit: {Message}", ex.Message);
                return RedirectToAction(nameof(Index));
            }
        }
        return View(nameof(Edit), new CategoryEditViewModel { Id = id, NewName = input.NewName });
    }

    [HttpPost("{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await sender.Send(new DeleteCategoryCommand(id), ct);
            TempData["Message"] = "Categoría eliminada.";
        }
        catch (ResourceNotFoundException ex)
        {
            logger.LogWarning("Unexpected resource not found during category deletion: {Message}", ex.Message);
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> RenderIndexAsync(CreateCategoryInputModel input, CancellationToken ct)
    {
        var categories = await sender.Send(new ListCategoriesQuery(), ct);
        return View(CategoryListViewModel.From(categories, TempData["Message"] as string, input));
    }
}
