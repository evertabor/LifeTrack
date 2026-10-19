using LifeTrack.Application.Common;
using LifeTrack.Application.Identity;
using LifeTrack.Web.ViewModels.Account;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(RegisterViewModel.From(input));

        try
        {
            var result = await sender.Send(new RegisterAccountCommand(input.Email, input.Password, input.DisplayName, input.PhoneNumber), ct);
            if (result.Succeeded)
                return RedirectToAction("Index", "Dashboard");

            return View(RegisterViewModel.From(input, "No se pudo crear la cuenta. Revisa el correo y la contraseña."));
        }
        catch (BusinessRuleException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(RegisterViewModel.From(input));
        }
        catch (ResourceNotFoundException ex)
        {
            logger.LogWarning(ex, "Unexpected resource not found during registration");
            return View(RegisterViewModel.From(input));
        }
    }

    [HttpGet("Login")]
    [AllowAnonymous]
    public IActionResult Login() =>
        View(new LoginViewModel());

    [HttpPost("Login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(LoginViewModel.From(input));

        try
        {
            var result = await sender.Send(new LoginCommand(input.Email, input.Password), ct);
            if (result.Succeeded)
                return RedirectToAction("Index", "Dashboard");

            return View(LoginViewModel.From(input, "Correo o contraseña no válidos."));
        }
        catch (BusinessRuleException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(LoginViewModel.From(input));
        }
        catch (ResourceNotFoundException ex)
        {
            logger.LogWarning(ex, "Resource not found during login");
            return View(LoginViewModel.From(input));
        }
    }

    [HttpPost("Logout")]
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
            logger.LogWarning(ex, "Profile requested with unknown account");
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

                ModelState.AddModelError(string.Empty, "No se pudo actualizar el perfil. Revisa los datos.");
            }
            catch (BusinessRuleException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (ResourceNotFoundException ex)
            {
                logger.LogWarning(ex, "Profile update requested with unknown account");
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
            logger.LogWarning(ex, "Profile requested with unknown account");
            return RedirectToAction("Index", "Transactions");
        }
    }

    [HttpGet("Delete")]
    public async Task<IActionResult> DeleteAccount(CancellationToken ct)
    {
        try
        {
            var profile = await sender.Send(new GetProfileQuery(), ct);
            return View(DeleteAccountViewModel.From(profile));
        }
        catch (ResourceNotFoundException ex)
        {
            logger.LogWarning(ex, "Delete account requested with unknown account");
            return RedirectToAction("Index", "Transactions");
        }
    }

    [HttpPost("Delete")]
    public async Task<IActionResult> DeleteAccountConfirmed(CancellationToken ct)
    {
        try
        {
            await sender.Send(new DeleteAccountCommand(), ct);
            return RedirectToAction("Login");
        }
        catch (BusinessRuleException ex)
        {
            logger.LogWarning(ex, "Delete account failed");
            return RedirectToAction("Index", "Transactions");
        }
        catch (ResourceNotFoundException ex)
        {
            logger.LogWarning(ex, "Delete account requested with unknown account");
            return RedirectToAction("Index", "Transactions");
        }
    }
}
