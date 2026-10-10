using LifeTrack.Application.Common;
using LifeTrack.Application.Identity;
using LifeTrack.Web.ViewModels.Account;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeTrack.Web.Controllers;

[Route("Account")]
[AllowAnonymous]
public sealed class AccountController(
    ISender sender,
    ILogger<AccountController> logger) : Controller
{

    [HttpGet("Register")]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost("Register")]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var result = await sender.Send(new RegisterAccountCommand(model.Email, model.Password), ct);
            if (result.Succeeded)
                return RedirectToAction("Index", "Home");

            return View(new RegisterViewModel { Email = model.Email, ErrorMessage = string.Join(" ", result.Errors ?? Array.Empty<string>()) });
        }
        catch (BusinessRuleException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
        catch (ResourceNotFoundException ex)
        {
            logger.LogWarning("Unexpected resource not found during registration: {Message}", ex.Message);
            return View(model);
        }
    }

    [HttpGet("Login")]
    public IActionResult Login([FromQuery] string? returnUrl) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost("Login")]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var result = await sender.Send(new LoginCommand(model.Email, model.Password), ct);
            if (result.Succeeded)
                return RedirectToAction("Index", "Home");

            return View(new LoginViewModel { Email = model.Email, ReturnUrl = model.ReturnUrl, ErrorMessage = result.ErrorMessage });
        }
        catch (BusinessRuleException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(new LoginViewModel { Email = model.Email, ReturnUrl = model.ReturnUrl });
        }
        catch (ResourceNotFoundException ex)
        {
            logger.LogWarning("Resource not found during login: {Message}", ex.Message);
            return View(new LoginViewModel { Email = model.Email, ReturnUrl = model.ReturnUrl });
        }
    }

    [HttpPost("Logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await sender.Send(new LogoutCommand(), ct);
        return RedirectToAction("Index", "Home");
    }
}
