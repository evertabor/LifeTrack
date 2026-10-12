using LifeTrack.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeTrack.Web.Controllers;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
[Route("Error")]
public sealed class ErrorController : Controller
{
    [HttpGet]
    [HttpPost]
    public IActionResult Index(int? statusCode)
    {
        var vm = new ErrorViewModel(
            HttpContext.TraceIdentifier,
            ErrorViewModel.DefaultMessage);
        return View("Index", vm);
    }
}
