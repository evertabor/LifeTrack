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
    public async Task<IActionResult> Create(
        [FromForm] CreateCategoryInputModel input,
        CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            if (input.Type is null)
            {
                ModelState.AddModelError("Type", "El tipo es obligatorio.");
            }
            else
            {
                try
                {
                    await sender.Send(new CreateCategoryCommand(input.Name, input.Type.Value), ct);
                    TempData["Message"] = "Categoría creada.";
                    return RedirectToAction(nameof(Index));
                }
                catch (BusinessRuleException ex)
                {
                    ModelState.AddModelError("Name", ex.Message);
                }
                catch (ResourceNotFoundException ex)
                {
                    logger.LogWarning(ex, "Unexpected resource not found during category creation");
                }
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
            logger.LogWarning(ex, "Unexpected resource not found opening category edit");
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost("{id:guid}/edit")]
    public async Task<IActionResult> Edit(
        Guid id,
        [FromForm] EditCategoryInputModel input,
        CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            if (input.Type is null)
            {
                ModelState.AddModelError("Type", "El tipo es obligatorio.");
            }
            else
            {
                try
                {
                    await sender.Send(new EditCategoryCommand(id, input.NewName, input.Type.Value), ct);
                    TempData["Message"] = "Categoría actualizada.";
                    return RedirectToAction(nameof(Index));
                }
                catch (BusinessRuleException ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
                catch (ResourceNotFoundException ex)
                {
                    logger.LogWarning(ex, "Unexpected resource not found during category edit");
                    return RedirectToAction(nameof(Index));
                }
            }
        }
        return View(nameof(Edit), CategoryEditViewModel.From(id, input.NewName, input.Type is true));
    }

    [HttpPost("{id:guid}/delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await sender.Send(new DeleteCategoryCommand(id), ct);
            TempData["Message"] = "Categoría eliminada.";
        }
        catch (BusinessRuleException ex)
        {
            TempData["Message"] = ex.Message;
        }
        catch (ResourceNotFoundException ex)
        {
            logger.LogWarning(ex, "Unexpected resource not found during category deletion");
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> RenderIndexAsync(CreateCategoryInputModel input, CancellationToken ct)
    {
        var categories = await sender.Send(new ListCategoriesQuery(), ct);
        return View(nameof(Index), CategoryListViewModel.From(categories, TempData["Message"] as string, input));
    }
}
