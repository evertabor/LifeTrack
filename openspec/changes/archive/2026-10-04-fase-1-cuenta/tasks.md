# Tasks: fase-1-cuenta — Phase 1 Account (Identity & Security Foundation)

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 2,200–2,800 authored (+ ~500 tests) |
| 400-line budget risk | High |
| Chained PRs recommended | No |
| Suggested split | Single PR (fase-1-cuenta is one atomic deliverable) |
| Delivery strategy | single-pr |
| Chain strategy | size:exception |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: size:exception
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|------|------|-----------|----------------------|-----------------|-------------------|
| 1 | Full Identity foundation — Domain + Application + Infrastructure | PR 1 | `dotnet test --filter "RegisterAccountHandler|LoginHandler|LogoutHandler|FallbackPolicy"` | Manual: register → login → logout → access protected resource → check 401/redirect | Delete Identity tables via migration; revert Program.cs |
| 2 | Full Web + tests — Controllers, Views, Middleware, Program.cs, architecture tests | PR 1 | `dotnet test` | Manual: /health anonymous, /Account/Register/Login/Logout functional | Revert Program.cs composition |

---

## Phase 1: Domain — Foundation Types

- [x] 1.1 Create `src/LifeTrack.Domain/Common/Entity.cs` with base class declaring `public Guid Id { get; }` and `protected Entity(Guid id)` constructor; implement `IEquatable<Entity>` by Id
- [x] 1.2 Create `src/LifeTrack.Domain/Common/Exceptions/DomainException.cs` as `public class DomainException : Exception` with `public DomainException(string message) : base(message)` — Spanish-safe message, throwable by domain invariants
- [x] 1.3 Create `src/LifeTrack.Domain/Common/Exceptions/NotFoundException.cs` as `public class NotFoundException : Exception` with `public NotFoundException(string message) : base(message)` — used when a domain entity is not found

## Phase 2: Application — Abstractions, Exceptions, Identity Commands, and IAccountService

- [x] 2.1 Create `src/LifeTrack.Application/Abstractions/ICurrentUser.cs` declaring `public interface ICurrentUser` with `Guid? UserId { get; }` property — returns `null` when unauthenticated, no ASP.NET types
- [x] 2.2 Create `src/LifeTrack.Application/Common/Exceptions/BusinessRuleException.cs` as `public class BusinessRuleException : Exception` with `public BusinessRuleException(string message) : base(message)` — frontier exception for business rule violations; Spanish-safe message
- [x] 2.3 Create `src/LifeTrack.Application/Common/Exceptions/ResourceNotFoundException.cs` as `public class ResourceNotFoundException : Exception` with `public ResourceNotFoundException(string message) : base(message)` — generic message only; never reveals whether resource exists
- [x] 2.4 Create `src/LifeTrack.Application/Identity/IAccountService.cs` — **Application-owned abstraction; Infrastructure implements it; NO ASP.NET Identity types in this file**: declare `public sealed record AccountOperationResult(bool Succeeded, string[]? Errors)`, `public sealed record LoginOperationResult(bool Succeeded, string? ErrorMessage)`, and `public interface IAccountService` with `Task<AccountOperationResult> RegisterAsync(string email, string password, CancellationToken cancellationToken)`, `Task<LoginOperationResult> LoginAsync(string email, string password, CancellationToken cancellationToken)`, and `Task LogoutAsync(CancellationToken cancellationToken)` — all parameters use primitives only (no Identity types); **no** comments
- [x] 2.5 Create `src/LifeTrack.Application/Identity/Commands/RegisterAccount/RegisterAccountCommand.cs` with `public sealed record RegisterAccountCommand(string Email, string Password) : ICommand<RegisterAccountResult>`
- [x] 2.6 Create `src/LifeTrack.Application/Identity/Commands/RegisterAccount/RegisterAccountHandler.cs` as `public sealed class RegisterAccountHandler : IRequestHandler<RegisterAccountCommand, RegisterAccountResult>` — inject `IAccountService` (NOT `UserManager`/`SignInManager`); on success call `IAccountService.RegisterAsync`; translate duplicate email Identity errors to `Succeeded=false` with `"An account with this email already exists."`; catch `DomainException`/`NotFoundException` and rethrow as `BusinessRuleException`/`ResourceNotFoundException`; **no** comments
- [x] 2.7 Create `src/LifeTrack.Application/Identity/Commands/Login/LoginCommand.cs` with `public sealed record LoginCommand(string Email, string Password) : ICommand<LoginResult>`
- [x] 2.8 Create `src/LifeTrack.Application/Identity/Commands/Login/LoginHandler.cs` as `public sealed class LoginHandler : IRequestHandler<LoginCommand, LoginResult>` — inject `IAccountService` (NOT `SignInManager`); call `IAccountService.LoginAsync`; on lockout/wrong password return generic `"Invalid email or password."` — **never** reveal account lockout status; catch exceptions and rethrow as `BusinessRuleException`/`ResourceNotFoundException`; **no** comments
- [x] 2.9 Create `src/LifeTrack.Application/Identity/Commands/Logout/LogoutCommand.cs` with `public sealed record LogoutCommand : IRequest`
- [x] 2.10 Create `src/LifeTrack.Application/Identity/Commands/Logout/LogoutHandler.cs` as `public sealed class LogoutHandler : IRequestHandler<LogoutCommand>` — inject `IAccountService` (NOT `SignInManager`); call `IAccountService.LogoutAsync`; **no** comments

## Phase 3: Infrastructure — Persistence, Identity Service, and DI Registration

- [x] 3.1 Create `src/LifeTrack.Infrastructure/Identity/AccountService.cs` as `public sealed class AccountService : IAccountService` — inject `UserManager<ApplicationUser>` and `SignInManager<ApplicationUser>`; implement `RegisterAsync` (calls `_userManager.CreateAsync`, on success calls `_signInManager.SignInAsync`, returns `AccountOperationResult`), `LoginAsync` (calls `_signInManager.PasswordSignInAsync`, returns `LoginOperationResult` with generic error on lockout/failure), and `LogoutAsync` (calls `_signInManager.SignOutAsync`); **this is the ONLY file in the solution that references both `UserManager<ApplicationUser>` and `SignInManager<ApplicationUser>`**; **no** comments
- [x] 3.2 Create `src/LifeTrack.Infrastructure/Persistence/ApplicationUser.cs` as `public class ApplicationUser : IdentityUser<Guid>` with `private ApplicationUser()` { } and `public static ApplicationUser Create(string email)` factory — `Email` set from parameter, `UserName` equals `Email`
- [x] 3.3 Create `src/LifeTrack.Infrastructure/Persistence/AppDbContext.cs` as `public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IUnitOfWork` — implement `IUnitOfWork` wrapping `DbContext.SaveChangesAsync`; `DbSet` for `Users` exposed for queries; **no** comments
- [x] 3.4 Create `src/LifeTrack.Infrastructure/Persistence/Configurations/ApplicationUserConfiguration.cs` as `IEntityTypeConfiguration<ApplicationUser>` — map `UserName` max length 256, `Email` max length 256, `NormalizedEmail` max length 256, `NormalizedUserName` max length 256
- [x] 3.5 Create `src/LifeTrack.Infrastructure/Persistence/UnitOfWork.cs` as `public class UnitOfWork : IUnitOfWork` wrapping `AppDbContext` — implement `SaveChangesAsync(CancellationToken)` delegating to `DbContext`; `IUnitOfWork` is the boundary for handlers calling `SaveChanges`
- [x] 3.6 Create `src/LifeTrack.Infrastructure/DependencyInjection.cs` with `public static class DependencyInjection` and `public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)` — register `DbContext` with SQL Server connection from `ConnectionStrings:DefaultConnection`; register `UserManager<ApplicationUser>` and `SignInManager<ApplicationUser>`; register `IUnitOfWork` as scoped; register `IAccountService` as scoped implementation (`AccountService`); configure `Identity:Password` options (min 12 chars, lockout 5 attempts / 5 min); **no** comments

## Phase 4: Web — Services, Controllers, ViewModels, and Views

- [x] 4.1 Create `src/LifeTrack.Web/Services/CurrentUser.cs` as `public class CurrentUser : ICurrentUser` — inject `IHttpContextAccessor`; `UserId` returns `Guid?` from `NameIdentifier` claim; handles missing/malformed claim gracefully (returns `null`, never throws); **no** comments
- [x] 4.2 Create `src/LifeTrack.Web/ViewModels/Account/RegisterViewModel.cs` with `public sealed record RegisterViewModel` input model (properties: `Email [Required, EmailAddress]`, `Password [Required, MinLength(12)]`) and nested `public sealed record DisplayModel(string Email, string? ErrorMessage)` with `public static DisplayModel From(RegisterAccountResult result)` factory; **no** comments
- [x] 4.3 Create `src/LifeTrack.Web/ViewModels/Account/LoginViewModel.cs` with `public sealed record LoginViewModel` input model (properties: `Email [Required, EmailAddress]`, `Password [Required]`, `ReturnUrl`) and nested `public sealed record DisplayModel(string Email, string? ReturnUrl, string? ErrorMessage)` with `public static DisplayModel From(LoginResult result, string returnUrl)` factory; **no** comments
- [x] 4.4 Create `src/LifeTrack.Web/ViewModels/ErrorViewModel.cs` with `public sealed record ErrorViewModel(string RequestId, string? Message)` — Message is the Spanish generic: `"Ha ocurrido un error inesperado. Por favor, inténtelo de nuevo más tarde."`
- [x] 4.5 Create `src/LifeTrack.Web/Controllers/AccountController.cs` as `[Route("Account")]` — inject `ISender` and `ILogger<AccountController>`; `HttpGet Register()` returns `IActionResult` with view; `HttpPost Register(RegisterViewModel model, CancellationToken ct)` creates `RegisterAccountCommand`, calls `sender.Send`, maps `RegisterAccountResult` to `RegisterViewModel.DisplayModel`, returns view on error or `RedirectToAction("Index","Home")` on success; `HttpGet Login([FromQuery] string? returnUrl)` returns view; `HttpPost Login(LoginViewModel model, CancellationToken ct)` creates `LoginCommand`, calls `sender.Send`, maps `LoginResult` to `DisplayModel`, returns view on error or `RedirectToAction("Index","Home")` (preserving ReturnUrl); `HttpPost Logout(CancellationToken ct)` sends `LogoutCommand`, redirects to `Index`; catch `BusinessRuleException` → `ModelState.AddModelError`; catch `ResourceNotFoundException` → `LogWarning`; **no** comments
- [x] 4.6 Create `src/LifeTrack.Web/Controllers/ErrorController.cs` with `[AllowAnonymous]` and `[Route("Error")]` — `Index(int? statusCode)` reads `HttpContext.TraceIdentifier`, returns `ErrorViewModel` with Spanish generic message; **no** comments
- [x] 4.7 Create `src/LifeTrack.Web/Views/Account/Register.cshtml` — Razor view with registration form; fields: Email (text input), Password (password input); validation messages; Spanish labels; form posts to same action with `asp-for`; **zero** logic in view; **no** comments; `<label for>` on every field
- [x] 4.8 Create `src/LifeTrack.Web/Views/Account/Login.cshtml` — Razor view with login form; fields: Email (text input), Password (password input); `ReturnUrl` hidden or preserved; validation messages; Spanish labels; form posts to same action with `asp-for`; **zero** logic in view; **no** comments; `<label for>` on every field
- [x] 4.9 Create `src/LifeTrack.Web/Views/Account/Logout.cshtml` — minimal Razor view with logout confirmation form; POST-only, `asp-action="Logout"`; Spanish text; **zero** logic; **no** comments
- [x] 4.10 Create `src/LifeTrack.Web/Views/Error/Index.cshtml` — Razor view; displays Spanish generic error message; displays `Model.RequestId` from `ErrorViewModel`; `<p>` with Spanish label `"Identificador de solicitud:"`; uses `_Layout.cshtml`; **no** stack trace or exception detail; **zero** logic; **no** comments
- [x] 4.11 Create `src/LifeTrack.Web/Security/SecurityHeadersMiddleware.cs` — `public class SecurityHeadersMiddleware` with `RequestDelegate next` and `IConfiguration configuration`; reads `SecurityHeaders:Csp`, `SecurityHeaders:ReferrerPolicy`, `SecurityHeaders:XFrameOptions`, `SecurityHeaders:PermissionsPolicy` from config; applies to every response in `InvokeAsync`: `Content-Security-Policy` (default-src 'self'; style-src 'self' 'unsafe-inline'; script-src 'self'; no unsafe-eval), `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, `Permissions-Policy: geolocation=(), microphone=(), camera=(), payment=()`; applies before response body written; **no** comments

## Phase 5: Program.cs Composition and EF Core Migration

- [x] 5.1 Modify `src/LifeTrack.Web/Program.cs` — add `using LifeTrack.Infrastructure;`, `using Microsoft.AspNetCore.Identity;`, `using Microsoft.AspNetCore.Authentication.Cookies;`; after `AddControllersWithViews`, call `builder.Services.AddInfrastructure(builder.Configuration);` to register DbContext, Identity, IUnitOfWork, and IAccountService (as AccountService)
- [x] 5.2 Modify `src/LifeTrack.Web/Program.cs` — after `AddInfrastructure`, configure `AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)` with `options.LoginPath = "/Account/Login"`, `options.AccessDeniedPath = "/Account/AccessDenied"`, `options.ExpireTimeSpan = TimeSpan.FromHours(1)`; register cookie as `HttpOnly, Secure, SameSiteMode.Strict`
- [x] 5.3 Modify `src/LifeTrack.Web/Program.cs` — after `AddAuthentication`, call `AddAuthorization` with `options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()` — fallback policy covering all non-[AllowAnonymous] endpoints
- [x] 5.4 Modify `src/LifeTrack.Web/Program.cs` — after `app.UseRouting()`, insert `app.UseAuthentication()` then `app.UseAuthorization()`; before all middleware, insert `app.UseMiddleware<SecurityHeadersMiddleware>()`; after `UseHttpsRedirection`, insert health endpoint: `app.MapGet("/health", () => Results.Ok(new { status = "ok" }))` with `Content-Type: application/json` — this must be **before** `UseRouting` to bypass authorization
- [x] 5.5 Modify `src/LifeTrack.Web/Program.cs` — after `UseExceptionHandler` (in non-Development) and `UseStatusCodePagesWithReExecute`, wire `app.UseStatusCodePagesWithReExecute("/Error?statusCode={0}")` for non-success status codes; register `ICurrentUser` as scoped: `builder.Services.AddScoped<ICurrentUser, CurrentUser>()`; register `ErrorController` as `[AllowAnonymous]` via attribute on the controller class
- [x] 5.6 Run `dotnet ef migrations add CreateIdentitySchema --project src/LifeTrack.Infrastructure --startup-project src/LifeTrack.Web --output-dir Persistence/Migrations` locally and verify schema with `dotnet ef dbcontext info`; **do not** apply migration automatically in production
- [x] 5.7 Run `dotnet build -c Release` and `dotnet test`; verify all architecture tests pass; verify `dotnet format --verify-no-changes` passes

## Phase 6: Tests

- [x] 6.1 Create `tests/LifeTrack.Application.Tests/Fakes/FakeCurrentUser.cs` — `public class FakeCurrentUser : ICurrentUser` with constructor accepting `Guid? userId`; implements `UserId` property; used in handler tests to simulate authenticated and unauthenticated contexts
- [x] 6.2 Create `tests/LifeTrack.Application.Tests/Fakes/FakeAccountService.cs` — `public class FakeAccountService : IAccountService` with constructor accepting canned `AccountOperationResult` and `LoginOperationResult` responses; implements `RegisterAsync`, `LoginAsync`, `LogoutAsync` returning the configured results; used to test handlers in isolation without Infrastructure; **no** comments
- [x] 6.3 Create `tests/LifeTrack.Application.Tests/Identity/RegisterAccountHandlerTests.cs` — send `RegisterAccountCommand` via `ISender` with `AddMediatR` over `ServiceCollection`; inject `FakeAccountService`; test: (a) valid registration returns `Succeeded=true`; (b) duplicate email returns `Succeeded=false` with `"An account with this email already exists."`; (c) weak password returns `Succeeded=false` with Identity errors from `PasswordValidator`; **no** comments; name tests: `RegisterAccountHandler_ValidInput_ReturnsSuccess`, `RegisterAccountHandler_DuplicateEmail_ReturnsDuplicateError`, `RegisterAccountHandler_WeakPassword_ReturnsIdentityErrors`
- [x] 6.4 Create `tests/LifeTrack.Application.Tests/Identity/LoginHandlerTests.cs` — send `LoginCommand` via `ISender` with `AddMediatR`; inject `FakeAccountService`; test: (a) valid credentials return `Succeeded=true`; (b) wrong password returns `Succeeded=false` with generic `"Invalid email or password."`; (c) non-existent email returns `Succeeded=false` with generic `"Invalid email or password."`; (d) locked-out account returns generic `"Invalid email or password."` (lockout status **not** revealed); **no** comments; name tests: `LoginHandler_ValidCredentials_ReturnsSuccess`, `LoginHandler_WrongPassword_ReturnsGenericError`, `LoginHandler_NonExistentEmail_ReturnsGenericError`, `LoginHandler_LockedOutAccount_ReturnsGenericError`
- [x] 6.5 Create `tests/LifeTrack.Application.Tests/Identity/LogoutHandlerTests.cs` — send `LogoutCommand` via `ISender` with `AddMediatR`; inject `FakeAccountService`; test that `LogoutAsync` is called on the service; **no** comments; name test: `LogoutHandler_CallsLogoutAsync`
- [x] 6.6 Add `FallbackPolicy_IsRequireAuthenticatedUser` test to `tests/LifeTrack.Architecture.Tests/DependencyRulesTests.cs` — read `Program.cs` source via `ReadProgramSource()` and assert that `FallbackPolicy` is set to `RequireAuthenticatedUser()`; this test verifies the auth-fallback-policy spec requirement; **no** comments
- [x] 6.7 Create `tests/LifeTrack.Application.Tests/CurrentUserTests.cs` — test `CurrentUser` (or `FakeCurrentUser`): (a) authenticated `ClaimsPrincipal` with `NameIdentifier` claim returns correct `Guid`; (b) anonymous principal returns `null`; (c) `null` `HttpContext.User` returns `null`; (d) malformed `NameIdentifier` claim value returns `null`; **no** comments; name tests: `CurrentUser_AuthenticatedPrincipal_ReturnsGuid`, `CurrentUser_AnonymousPrincipal_ReturnsNull`, `CurrentUser_NullHttpContextUser_ReturnsNull`, `CurrentUser_MalformedNameIdentifierClaim_ReturnsNull`

---

## RED Tests (Security & Safety — threat matrix items requiring verification)

These are **test tasks** that verify the system fails safely. Each maps to a specific spec scenario and should produce a RED (failing) result before the corresponding production code is implemented.

- [x] R.1 `health-endpoint anonymous access`: HTTP integration tests pass anonymously with `200 OK` and the expected JSON payload
- [x] R.2 `antiforgery on register POST`: HTTP integration test passes with `400 Bad Request` when the token is absent
- [x] R.3 `antiforgery on login POST`: HTTP integration test passes with `400 Bad Request` when the token is absent
- [x] R.4 `antiforgery on logout POST`: HTTP integration test passes with `400 Bad Request` when the token is absent
- [x] R.5 `current-user fail-closed null principal`: Fully verified by `CurrentUserTests` — `CurrentUser_AnonymousPrincipal_ReturnsNull` and `CurrentUser_MalformedNameIdentifierClaim_ReturnsNull` confirm `UserId` returns `null` (not exception) for fail-closed behavior
- [x] R.6 `error-page no leak`: HTTP integration test passes without exposing exception details
- [x] R.7 `login generic error no enumeration`: Verified by handler unit tests — `LoginHandler_WrongPassword_ReturnsGenericError`, `LoginHandler_NonExistentEmail_ReturnsGenericError`, and `LoginHandler_LockedOutAccount_ReturnsGenericError` all assert exact `"Invalid email or password."` generic message; `AccountService.LoginAsync` implements lockout → generic message translation

---

## Dependency Order

```
Phase 1 (Domain types: Entity, DomainException, NotFoundException)
    │
    ▼
Phase 2 (Application types: ICurrentUser, BusinessRuleException, ResourceNotFoundException,
         IAccountService + result types, RegisterAccountCommand/Handler, LoginCommand/Handler,
         LogoutCommand/Handler)
    │
    ▼
Phase 3 (Infrastructure: AccountService : IAccountService, ApplicationUser, AppDbContext,
         ApplicationUserConfiguration, UnitOfWork, AddInfrastructure)
    │   ← IAccountService lives in Application; AccountService (in Infrastructure) implements it
    │
    ▼
Phase 4 (Web: CurrentUser service, AccountController, ViewModels, Views, SecurityHeadersMiddleware)
    │
    ▼
Phase 5 (Program.cs: compose all above, health endpoint, auth policy, error handling)
    │
    ▼
Phase 6 (Tests: FakeCurrentUser, FakeAccountService, handler tests, architecture tests, RED tests)
```

Note: Phase 2 (Application Identity handlers) runs before Phase 3 (Infrastructure `AccountService`) because handlers depend only on `IAccountService` (an Application-owned abstraction). `AccountService : IAccountService` in Infrastructure is the implementation detail that Phase 3 provides. Web controllers call `sender.Send(command)` — they don't need the implementation to exist at compile time because MediatR resolves at runtime. The `AddMediatR` scan of the Application assembly (via `ApplicationAssemblyAnchor`) registers all handlers automatically.

## Rollback Notes

- EF Core migration: `dotnet ef migrations remove` locally; drop Identity tables in shared DB if already applied
- Revert `Program.cs` composition to original state (remove `AddInfrastructure`, `AddAuthentication`, `AddAuthorization`, `SecurityHeadersMiddleware`, health endpoint, exception handlers)
- Delete all created files listed above
- Run `dotnet build` to confirm clean state
