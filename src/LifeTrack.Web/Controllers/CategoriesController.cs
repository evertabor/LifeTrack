using LifeTrack.Application.Common;
using LifeTrack.Application.Finance.Commands.CreateCategory;
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
        return await RenderIndexAsync(ct);
    }

    private async Task<IActionResult> RenderIndexAsync(CancellationToken ct)
    {
        var categories = await sender.Send(new ListCategoriesQuery(), ct);
        return View(CategoryListViewModel.From(categories, TempData["Message"] as string));
    }
}
