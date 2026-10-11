using LifeTrack.Application.Common;
using LifeTrack.Application.Identity;
using LifeTrack.Web.ViewModels.Account;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeTrack.Web.Controllers;

[Route("account")]
public sealed class AccountController(
    ISender sender,
    ILogger<AccountController> logger) : Controller
{

    [HttpGet("Register")]
    [AllowAnonymous]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost("Register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var result = await sender.Send(new RegisterAccountCommand(model.Email, model.Password, model.DisplayName, model.PhoneNumber), ct);
            if (result.Succeeded)
                return RedirectToAction("Index", "Transactions");

            return View(new RegisterViewModel { Email = model.Email, DisplayName = model.DisplayName, PhoneNumber = model.PhoneNumber, ErrorMessage = string.Join(" ", result.Errors ?? Array.Empty<string>()) });
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
    [AllowAnonymous]
    public IActionResult Login() =>
        View(new LoginViewModel());

    [HttpPost("Login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var result = await sender.Send(new LoginCommand(model.Email, model.Password), ct);
            if (result.Succeeded)
                return RedirectToAction("Index", "Transactions");

            return View(new LoginViewModel { Email = model.Email, ErrorMessage = result.ErrorMessage });
        }
        catch (BusinessRuleException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
        catch (ResourceNotFoundException ex)
        {
            logger.LogWarning("Resource not found during login: {Message}", ex.Message);
            return View(model);
        }
    }

    [HttpPost("Logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await sender.Send(new LogoutCommand(), ct);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("Profile")]
    public async Task<IActionResult> Profile(CancellationToken ct)
    {
        try
        {
            var profile = await sender.Send(new GetProfileQuery(), ct);
            return View(ProfileViewModel.From(profile, null, TempData["Message"] as string));
        }
        catch (ResourceNotFoundException ex)
        {
            logger.LogWarning("Profile requested with unknown account: {Message}", ex.Message);
            return RedirectToAction("Index", "Transactions");
        }
    }

    [HttpPost("Profile")]
    public async Task<IActionResult> Profile([FromForm(Name = "Input")] EditProfileInputModel input, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var result = await sender.Send(new UpdateProfileCommand(input.DisplayName, input.PhoneNumber), ct);
                if (result.Succeeded)
                {
                    TempData["Message"] = "Perfil actualizado.";
                    return RedirectToAction(nameof(Profile));
                }

                ModelState.AddModelError(string.Empty, string.Join(" ", result.Errors ?? Array.Empty<string>()));
            }
            catch (BusinessRuleException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (ResourceNotFoundException ex)
            {
                logger.LogWarning("Profile update requested with unknown account: {Message}", ex.Message);
                return RedirectToAction("Index", "Transactions");
            }
        }

        try
        {
            var profile = await sender.Send(new GetProfileQuery(), ct);
            return View(ProfileViewModel.From(profile, input));
        }
        catch (ResourceNotFoundException ex)
        {
            logger.LogWarning("Profile requested with unknown account: {Message}", ex.Message);
            return RedirectToAction("Index", "Transactions");
        }
    }
}
