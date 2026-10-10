# Design: fase-1-cuenta — Phase 1 Account (Identity & Security Foundation)

## Technical Approach

This design implements the authentication and authorization foundation for LifeTrack: ASP.NET Core Identity with a custom MVC `AccountController` (HU-01..HU-03), global fallback authorization policy, anonymous `/health` liveness endpoint, `ICurrentUser` abstraction, security headers middleware, and error page with request-ID correlation.

The approach follows Clean Architecture strictly: `AccountController` dispatches MediatR commands (`RegisterAccountCommand`, `LoginCommand`, `LogoutCommand`) via `ISender` — the controller never calls `UserManager` or `SignInManager` directly. Identity types (`UserManager`, `SignInManager`, `IdentityDbContext`, `ApplicationUser`) live in Infrastructure. The `ICurrentUser` interface lives in `Application/Abstractions`; its implementation lives in `Web/Services/CurrentUser.cs`.

**Critical fix vs. prior draft**: Application handlers do NOT inject `UserManager<ApplicationUser>` or `SignInManager<ApplicationUser>` directly. Those are ASP.NET Identity types owned by Infrastructure. Instead, Application defines `IAccountService` — an Application-owned abstraction — and Infrastructure implements it. Handlers depend only on the interface.

---

## Why Application Does Not Reference Infrastructure or ASP.NET Identity

The dependency rule (`AGENTS.md` §4) is:

```
Web ──► Infrastructure ──► Application ──► Domain
 └───────────────────────► Application
```

And `Application/AGENTS.md` explicitly states:

> "Dependencias: `LifeTrack.Domain` y **un único paquete NuGet: `MediatR`** (`13.*`). Ni EF Core, ni ASP.NET, ni logging."

Injecting `UserManager<ApplicationUser>` or `SignInManager<ApplicationUser>` into an Application handler violates this on two axes:

1. **Cross-layer reference**: `UserManager<ApplicationUser>` is an ASP.NET Identity type whose assembly (`Microsoft.AspNetCore.Identity.EntityFrameworkCore`) is not MediatR. Handlers would drag in an extra NuGet dependency that the architecture tests explicitly forbid.
2. **`ApplicationUser` is Infrastructure-owned**: The `ApplicationUser : IdentityUser<Guid>` class lives in `LifeTrack.Infrastructure/Persistence/`. Having Application reference it would invert the dependency arrow (Application → Infrastructure), which `LifeTrack.Architecture.Tests` would catch with `Application_DoesNotDependOnInfrastructureOrWeb`.

The fix is the same pattern already used for repositories: **Application defines the interface, Infrastructure implements it**. Handlers receive `IAccountService` (an abstraction) rather than `UserManager` (a concrete ASP.NET type). This keeps the dependency arrow correct and keeps the architecture tests green.

---

## Architecture Decisions

### Decision: Application handlers depend on `IAccountService` abstraction, not on `UserManager`/`SignInManager` directly

**Choice**: Create `IAccountService` in `Application/Identity/` with `RegisterAsync`, `LoginAsync`, and `LogoutAsync` methods. Handlers inject this interface. Infrastructure implements it using `UserManager<ApplicationUser>` and `SignInManager<ApplicationUser>`.

**Alternatives considered**:
1. **Handler injects `UserManager<ApplicationUser>` directly** (prior draft) — violates `Application/AGENTS.md` "Ni EF Core, ni ASP.NET" and `LifeTrack.Architecture.Tests` `Application_DoesNotDependOnInfrastructureOrWeb`. Not implementable.
2. **Handler injects `UserManager<ApplicationUser>` but test fakes it with an interface wrapper** — adds a wrapper interface that still needs to live in Application (and reference `ApplicationUser`), which is the same violation in a different shape.
3. **MediatR pipeline behavior to wrap Identity operations** — defers the problem to a pipeline component that still needs access to `UserManager`; same dependency issue.

**Rationale**: The repository pattern analogy applies directly. Just as `IUserRepository` lives in Application and `UserRepository : IUserRepository` lives in Infrastructure, `IAccountService` lives in Application and `AccountService : IAccountService` lives in Infrastructure. Handlers stay dependency-rule compliant. Infrastructure owns all ASP.NET Identity types.

### Decision: `/health` registered as a minimal terminal middleware before `UseRouting`

**Choice**: `app.MapGet("/health", () => Results.Ok(new { status = "ok" }))` placed before `app.UseRouting()` in `Program.cs`.

**Alternatives considered**: (1) `HealthController` with `[AllowAnonymous]` — adds a controller class and action for a 3-line response; (2) `[AllowAnonymous]` on an action inside a controller — same overhead.

**Rationale**: The health endpoint is a pure infrastructure concern (liveness probe for Azure). Registering it as a terminal delegate avoids routing overhead and keeps the design minimal. Placement before `UseRouting` guarantees it bypasses authorization middleware entirely.

### Decision: `ICurrentUser` interface in `Application/Abstractions`, implementation in `Web/Services`

**Choice**: `ICurrentUser` (with `UserId` property) lives in `LifeTrack.Application/Abstractions/ICurrentUser.cs`. `CurrentUser` implementation lives in `LifeTrack.Web/Services/CurrentUser.cs`.

**Alternatives considered**: (1) Both interface and implementation in `Web` — breaks the Application-layer rule that handlers depend on `ICurrentUser`; (2) Both in `Infrastructure` — violates the Web rule that says `ICurrentUser` implementation lives in Web.

**Rationale**: Per `Application/AGENTS.md`: "Las interfaces (`I…Repository`, `IUnitOfWork`, `ICurrentUser`) viven en `Abstractions/` y las implementa `Infrastructure` (y `Web` para `ICurrentUser`)." Web is the explicit exception for `ICurrentUser` because it reads `HttpContext.User`, which only Web can access.

### Decision: Fallback authorization as `FallbackPolicy` on `AddAuthorization`

**Choice**: `options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()` set in `Program.cs`.

**Alternatives considered**: `DefaultPolicy` — `DefaultPolicy` is only consulted when an endpoint has explicit `[Authorize]` metadata (with no policy name specified). Endpoints with **no `[Authorize]` attribute at all** are NOT covered by `DefaultPolicy`. Since the spec requires that ALL controller actions without `[AllowAnonymous]` must require authentication — including actions that never declared `[Authorize]` — `DefaultPolicy` does not satisfy the contract and would leave those endpoints accessible to anonymous users. `FallbackPolicy` is the correct ASP.NET Core mechanism: it applies to every endpoint that does not have its own explicit authorization policy, which is exactly the deny-by-default model required by OWASP A01 and the `auth-fallback-policy` spec.

**Rationale**: Per `auth-fallback-policy/spec.md`, "all controller actions that do not have an explicit `[AllowAnonymous]` attribute MUST require authentication." In ASP.NET Core authorization:
- `[Authorize]` on an action → uses `DefaultPolicy` (or the named policy if specified)
- No authorization attribute → uses `FallbackPolicy` (if configured), otherwise allows anonymous
- `[AllowAnonymous]` → skips authorization entirely

Using `FallbackPolicy = RequireAuthenticatedUser()` ensures that any controller action lacking explicit `[AllowAnonymous]` is protected, regardless of whether it also lacks `[Authorize]`. This is the ASP.NET Core canonical pattern for a deny-by-default authorization model.

### Decision: `BusinessRuleException` and `ResourceNotFoundException` as concrete classes in `Application/Common/Exceptions`

**Choice**: Both are `public class` records (with string `Message` property) in `LifeTrack.Application/Common/Exceptions/`.

**Alternatives considered**: `abstract` base class with concrete derived types — adds indirection with no benefit at this stage.

**Rationale**: The proposal explicitly calls for "base types." Concrete classes are simpler and match the existing `ApplicationAssemblyAnchor` pattern of one public type per file. Both are part of the Application layer's public API for Web to catch.

### Decision: SecurityHeadersMiddleware reads configuration from `IConfiguration`

**Choice**: `SecurityHeadersMiddleware` reads `SecurityHeaders:Csp`, `SecurityHeaders:ReferrerPolicy`, `SecurityHeaders:XFrameOptions`, `SecurityHeaders:XFrameOptions`, etc. from `IConfiguration`. Defaults are applied if keys are missing.

**Alternatives considered**: Hardcoded values — operator cannot adjust without code change; violates the OWASP A02 requirement for configurable security settings.

**Rationale**: Per `security-headers/spec.md` and OWASP A02, security headers must be configurable. Reading from `IConfiguration` satisfies the spec and aligns with the existing `AddInfrastructure` pattern that also reads from configuration.

### Decision: `ErrorController` with `UseExceptionHandler("/Error")` and `UseStatusCodePagesWithReExecute`

**Choice**: `app.UseExceptionHandler("/Error")` for unhandled exceptions; `app.UseStatusCodePagesWithReExecute("/Error", "?statusCode={0})` for non-success status codes. `ErrorController.Index(int? statusCode)` renders the error view.

**Alternatives considered**: Inline lambda endpoint for error handling — less testable and harder to integrate with views.

**Rationale**: The error controller approach is the standard ASP.NET Core pattern. `HttpContext.TraceIdentifier` provides the request-ID for correlation. `[AllowAnonymous]` on `ErrorController` ensures unauthenticated users can see the error page.

## Data Flow

```
Browser Request
        │
        ▼
SecurityHeadersMiddleware  ──→ CSP, X-Content-Type-Options, X-Frame-Options,
        │                          Referrer-Policy, Permissions-Policy headers
        ▼
UseHttpsRedirection (if not Dev)
        │
        ▼
MapGet("/health", ...)  ←── (anonymous, before UseRouting — bypasses auth)
        │
        ▼
UseRouting
        │
        ▼
UseAuthentication  ←── cookie authentication from Identity
        │
        ▼
UseAuthorization  ←── FallbackPolicy = RequireAuthenticatedUser()
        │                  (covers all endpoints without explicit policy)
        │                  [AllowAnonymous] bypasses this entirely
        ▼
Endpoint (MVC Controller Action)
        │
        ▼
AccountController action
    │   [HttpGet] → returns view
    │   [HttpPost] → validates InputModel → sender.Send(command)
    │
    ▼
MediatR Dispatcher
    │
    ▼
Handler (e.g., RegisterAccountHandler)
    │   injects IAccountService (Application abstraction)
    ▼
    IAccountService.RegisterAsync()
        │  (Infrastructure implementation)
        ▼
        UserManager.CreateAsync()
        SignInManager.SignInAsync()
        returns AccountOperationResult
    │
    ▼
Controller receives result
    │ BusinessRuleException → ModelState.AddModelError → return view
    │ ResourceNotFoundException → LogWarning → generic message
    │ success → RedirectToAction (PRG)
        │
        ▼
Razor View rendered with _Layout.cshtml
        │
        ▼
Response with security headers applied
```

For an unauthenticated request to a protected resource:
```
Request → AuthorizationMiddleware → FallbackPolicy → 401 or redirect to /Account/Login
```

For an anonymous request to `/health`:
```
Request → MapGet handler (before UseRouting) → 200 OK with {"status":"ok"}
```

## File Changes

### New Files

| File | Action | Description |
|------|--------|-------------|
| `src/LifeTrack.Domain/Common/Entity.cs` | Create | Base class with `Id : Guid` property |
| `src/LifeTrack.Domain/Common/Exceptions/DomainException.cs` | Create | Domain rule violation, Spanish message |
| `src/LifeTrack.Domain/Common/Exceptions/NotFoundException.cs` | Create | Domain resource not found |
| `src/LifeTrack.Application/Abstractions/ICurrentUser.cs` | Create | `UserId` property interface |
| `src/LifeTrack.Application/Common/Exceptions/BusinessRuleException.cs` | Create | Frontier exception for business rule violations |
| `src/LifeTrack.Application/Common/Exceptions/ResourceNotFoundException.cs` | Create | Frontier exception for missing resources |
| `src/LifeTrack.Application/Identity/IAccountService.cs` | Create | **NEW** — Application-owned abstraction for account operations; defines `RegisterAsync`, `LoginAsync`, `LogoutAsync` without referencing ASP.NET Identity types |
| `src/LifeTrack.Application/Identity/Commands/RegisterAccount/RegisterAccountCommand.cs` | Create | `ICommand<RegisterAccountResult>` record |
| `src/LifeTrack.Application/Identity/Commands/RegisterAccount/RegisterAccountHandler.cs` | Create | `sealed` handler; injects `IAccountService` (NOT `UserManager`/`SignInManager`) |
| `src/LifeTrack.Application/Identity/Commands/Login/LoginCommand.cs` | Create | `ICommand<LoginResult>` record |
| `src/LifeTrack.Application/Identity/Commands/Login/LoginHandler.cs` | Create | `sealed` handler; injects `IAccountService` |
| `src/LifeTrack.Application/Identity/Commands/Logout/LogoutCommand.cs` | Create | `IRequest` record |
| `src/LifeTrack.Application/Identity/Commands/Logout/LogoutHandler.cs` | Create | `sealed` handler; injects `IAccountService` |
| `src/LifeTrack.Infrastructure/Identity/AccountService.cs` | Create | **NEW** — implements `IAccountService` using `UserManager<ApplicationUser>` and `SignInManager<ApplicationUser>`; owns all ASP.NET Identity integration |
| `src/LifeTrack.Infrastructure/Persistence/ApplicationUser.cs` | Create | `IdentityUser<Guid>` subclass |
| `src/LifeTrack.Infrastructure/Persistence/AppDbContext.cs` | Create | `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IUnitOfWork` |
| `src/LifeTrack.Infrastructure/Persistence/Configurations/ApplicationUserConfiguration.cs` | Create | `IEntityTypeConfiguration<ApplicationUser>` |
| `src/LifeTrack.Infrastructure/Persistence/UnitOfWork.cs` | Create | `IUnitOfWork` implementation wrapping `DbContext` |
| `src/LifeTrack.Infrastructure/DependencyInjection.cs` | Create | `AddInfrastructure()` extension; registers `IAccountService` as scoped implementation |
| `src/LifeTrack.Web/Services/CurrentUser.cs` | Create | `ICurrentUser` implementation reading `NameIdentifier` claim |
| `src/LifeTrack.Web/Controllers/AccountController.cs` | Create | `[Route("Account")]`, Register/Login/Logout actions; uses `ISender` only |
| `src/LifeTrack.Web/Controllers/ErrorController.cs` | Create | `[AllowAnonymous]`, `Index(int? statusCode)` |
| `src/LifeTrack.Web/ViewModels/Account/RegisterViewModel.cs` | Create | Input model + display model with `From(...)` factory |
| `src/LifeTrack.Web/ViewModels/Account/LoginViewModel.cs` | Create | Input model + display model with `From(...)` factory |
| `src/LifeTrack.Web/ViewModels/ErrorViewModel.cs` | Create | `RequestId` property |
| `src/LifeTrack.Web/Views/Account/Register.cshtml` | Create | Registration form view |
| `src/LifeTrack.Web/Views/Account/Login.cshtml` | Create | Login form view |
| `src/LifeTrack.Web/Views/Account/Logout.cshtml` | Create | Logout confirmation view |
| `src/LifeTrack.Web/Views/Error/Index.cshtml` | Create | Error page with request-ID |
| `src/LifeTrack.Web/Security/SecurityHeadersMiddleware.cs` | Create | CSP, X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy |
| `tests/LifeTrack.Application.Tests/Fakes/FakeCurrentUser.cs` | Create | Fake for testing |
| `tests/LifeTrack.Application.Tests/Identity/RegisterAccountHandlerTests.cs` | Create | Handler tests |
| `tests/LifeTrack.Application.Tests/Identity/LoginHandlerTests.cs` | Create | Handler tests |

### Modified Files

| File | Action | Description |
|------|--------|-------------|
| `src/LifeTrack.Web/Program.cs` | Modify | Add Identity, `AddInfrastructure()`, global auth policy (FallbackPolicy), security middleware, health endpoint, error handling, register `IAccountService` via `AddInfrastructure` |
| `tests/LifeTrack.Architecture.Tests/DependencyRulesTests.cs` | Modify | No change needed — existing rules already cover the new code correctly |

### Deleted Files (vs. prior draft)

| File | Reason |
|------|--------|
| `src/LifeTrack.Application/Identity/Commands/RegisterAccount/RegisterAccountHandler.cs` (old version with `UserManager` injection) | Replaced with handler that injects `IAccountService` |
| `src/LifeTrack.Application/Identity/Commands/Login/LoginHandler.cs` (old version with `SignInManager` injection) | Replaced with handler that injects `IAccountService` |
| `src/LifeTrack.Application/Identity/Commands/Logout/LogoutHandler.cs` (old version with `SignInManager` injection) | Replaced with handler that injects `IAccountService` |

## Interfaces / Contracts

### Application Layer

```csharp
// src/LifeTrack.Application/Abstractions/ICurrentUser.cs
namespace LifeTrack.Application.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }
}
```

```csharp
// src/LifeTrack.Application/Common/Exceptions/BusinessRuleException.cs
namespace LifeTrack.Application.Common;

public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}
```

```csharp
// src/LifeTrack.Application/Common/Exceptions/ResourceNotFoundException.cs
namespace LifeTrack.Application.Common;

public class ResourceNotFoundException : Exception
{
    public ResourceNotFoundException(string message) : base(message) { }
}
```

```csharp
// src/LifeTrack.Application/Identity/IAccountService.cs
// NEW — Application-owned abstraction; Infrastructure implements it.
// NO ASP.NET Identity types appear in this file.
namespace LifeTrack.Application.Identity;

public sealed record AccountOperationResult(bool Succeeded, string[]? Errors);

public interface IAccountService
{
    Task<AccountOperationResult> RegisterAsync(
        string email,
        string password,
        CancellationToken cancellationToken);

    Task<LoginOperationResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken);

    Task LogoutAsync(CancellationToken cancellationToken);
}

public sealed record LoginOperationResult(bool Succeeded, string? ErrorMessage);
```

```csharp
// src/LifeTrack.Application/Identity/Commands/RegisterAccount/RegisterAccountCommand.cs
namespace LifeTrack.Application.Identity;

public sealed record RegisterAccountCommand(
    string Email,
    string Password
) : ICommand<RegisterAccountResult>;

public sealed record RegisterAccountResult(bool Succeeded, string[]? Errors);
```

```csharp
// src/LifeTrack.Application/Identity/Commands/RegisterAccount/RegisterAccountHandler.cs
namespace LifeTrack.Application.Identity;

public sealed class RegisterAccountHandler
    : IRequestHandler<RegisterAccountCommand, RegisterAccountResult>
{
    public RegisterAccountHandler(IAccountService accountService) { ... }
    // Injects IAccountService — NOT UserManager/SignInManager
    // No ASP.NET Identity types in this file
}
```

```csharp
// src/LifeTrack.Application/Identity/Commands/Login/LoginCommand.cs
namespace LifeTrack.Application.Identity;

public sealed record LoginCommand(
    string Email,
    string Password
) : ICommand<LoginResult>;

public sealed record LoginResult(bool Succeeded, string? ErrorMessage);
```

```csharp
// src/LifeTrack.Application/Identity/Commands/Login/LoginHandler.cs
namespace LifeTrack.Application.Identity;

public sealed class LoginHandler
    : IRequestHandler<LoginCommand, LoginResult>
{
    public LoginHandler(IAccountService accountService) { ... }
    // Injects IAccountService — NOT SignInManager
}
```

```csharp
// src/LifeTrack.Application/Identity/Commands/Logout/LogoutCommand.cs
namespace LifeTrack.Application.Identity;

public sealed record LogoutCommand : IRequest;
```

```csharp
// src/LifeTrack.Application/Identity/Commands/Logout/LogoutHandler.cs
namespace LifeTrack.Application.Identity;

public sealed class LogoutHandler
    : IRequestHandler<LogoutCommand>
{
    public LogoutHandler(IAccountService accountService) { ... }
    // Injects IAccountService — NOT SignInManager
}
```

### Infrastructure Layer

```csharp
// src/LifeTrack.Infrastructure/Identity/AccountService.cs
// Implements IAccountService from Application.
// This is the ONLY place UserManager and SignInManager are referenced.
namespace LifeTrack.Infrastructure.Identity;

public sealed class AccountService : IAccountService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<AccountOperationResult> RegisterAsync(
        string email, string password, CancellationToken ct)
    {
        var user = ApplicationUser.Create(email);
        var result = await _userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(user, isPersistent: false);
        }
        return new AccountOperationResult(
            result.Succeeded,
            result.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<LoginOperationResult> LoginAsync(
        string email, string password, CancellationToken ct)
    {
        var result = await _signInManager.PasswordSignInAsync(
            email, password, isPersistent: false, lockoutOnFailure: true);
        if (result.IsLockedOut)
            return new LoginOperationResult(false, "Invalid email or password.");
        return new LoginOperationResult(result.Succeeded,
            result.Succeeded ? null : "Invalid email or password.");
    }

    public async Task LogoutAsync(CancellationToken ct)
    {
        await _signInManager.SignOutAsync();
    }
}
```

```csharp
// src/LifeTrack.Infrastructure/Persistence/ApplicationUser.cs
namespace LifeTrack.Infrastructure.Persistence;

public class ApplicationUser : IdentityUser<Guid>
{
    private ApplicationUser() { }
    public static ApplicationUser Create(string email) => throw null!;
}
```

```csharp
// src/LifeTrack.Infrastructure/Persistence/AppDbContext.cs
namespace LifeTrack.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IUnitOfWork
{
    public DbSet<ApplicationUser> Users => throw null!;
    public Task<int> SaveChangesAsync(CancellationToken ct) => throw null!;
}
```

```csharp
// src/LifeTrack.Infrastructure/DependencyInjection.cs
namespace LifeTrack.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Registers DbContext, UserManager<ApplicationUser>, SignInManager<ApplicationUser>,
        // IUnitOfWork, and IAccountService (as AccountService)
    }
}
```

### Web Layer

```csharp
// src/LifeTrack.Web/Services/CurrentUser.cs
namespace LifeTrack.Web.Services;

public class CurrentUser : ICurrentUser
{
    public Guid? UserId { get; }
}
```

```csharp
// src/LifeTrack.Web/Controllers/AccountController.cs
namespace LifeTrack.Web.Controllers;

[Route("Account")]
public class AccountController : Controller
{
    [HttpGet]
    public IActionResult Register();
    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct);
    [HttpGet]
    public IActionResult Login([FromQuery] string? returnUrl);
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct);
    [HttpPost]
    public async Task<IActionResult> Logout(CancellationToken ct);
    // Injects ISender — never UserManager, SignInManager, or IAccountService
}
```

```csharp
// src/LifeTrack.Web/Controllers/ErrorController.cs
namespace LifeTrack.Web.Controllers;

[AllowAnonymous]
public class ErrorController : Controller
{
    [Route("Error")]
    public IActionResult Index(int? statusCode);
}
```

```csharp
// src/LifeTrack.Web/Security/SecurityHeadersMiddleware.cs
namespace LifeTrack.Web.Security;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    public SecurityHeadersMiddleware(RequestDelegate next, IConfiguration configuration);
    public Task InvokeAsync(HttpContext context);
}
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit (Application) | `RegisterAccountHandler`: happy path calls `IAccountService.RegisterAsync` → success; duplicate email returns errors; weak password returns Identity errors | Send command via `ISender` with fake `IAccountService` |
| Unit (Application) | `LoginHandler`: valid credentials → success; wrong password/lockout/non-existent → generic error | Send command via `ISender` with fake `IAccountService` |
| Unit (Application) | `LogoutHandler`: calls `IAccountService.LogoutAsync` | Send `LogoutCommand` via `ISender` with fake |
| Unit (Application) | `IAccountService` implementation in Infrastructure | Test `AccountService` directly with `UserManager`/`SignInManager` fakes |
| Unit (Web) | `RegisterViewModel.From(...)` factory: maps result to display-ready model | Direct unit test of `From(...)` |
| Unit (Web) | `CurrentUser`: returns correct `Guid` for authenticated principal; returns `null` for anonymous/unauthenticated | Test `CurrentUser` directly with mocked `HttpContext` |
| Architecture | `FallbackPolicy` is configured as `RequireAuthenticatedUser()` | Verify `Program.cs` authorization configuration |
| Architecture | Controllers do not reference `Infrastructure` or `Domain` | `Controllers_TalkToApplication_NotToInfrastructureOrDomain` already passes |
| Architecture | Controllers do not inject concrete handlers | `Controllers_DoNotInjectConcreteHandlers` already passes |
| Architecture | `AutoValidateAntiforgeryToken` still registered | `Antiforgery_GlobalFilter_IsRegistered` already passes |
| Architecture | Application has no Infrastructure/Web/EF/ASP.NET dependencies | `Application_DoesNotDependOnInfrastructureOrWeb` already passes |

**Not tested** (plumbing/framework coverage criterion):
- That MediatR resolves contracts to handlers — architecture tests cover registration
- View rendering — covered by manual review against `docs/09-estandar-vistas-mvc.md`
- HTTP-level redirect/401 behavior — deferred as noted in proposal risks

## Threat Matrix

N/A — no routing, shell, subprocess, VCS/PR automation, executable-file classification, or process-integration boundary in this design. The `/health` endpoint is a simple anonymous MapGet with no shell command, subprocess, or external process invocation.

## Migration / Rollout

1. **Domain/Common types** are added first — `Entity`, `DomainException`, `NotFoundException`. These are pure types with no dependencies and no migration needed.
2. **Application/Common** types — `BusinessRuleException`, `ResourceNotFoundException`, `ICurrentUser`. No database migration needed.
3. **Infrastructure** — `ApplicationUser`, `AppDbContext`, `AccountService`, `DependencyInjection.AddInfrastructure()`. EF Core migration `CreateIdentitySchema` will be generated. Apply locally and verify with `dotnet ef dbcontext info`. **Never auto-apply in production.**
4. **Web** — compose everything in `Program.cs`. The security headers middleware, health endpoint, error handler, and authorization policy are additive.
5. **Migration command**: `dotnet ef migrations add CreateIdentitySchema --project src/LifeTrack.Infrastructure --startup-project src/LifeTrack.Web --output-dir Persistence/Migrations`

Rollback: `dotnet ef migrations remove` locally; drop Identity tables in shared databases if already applied; revert `Program.cs` composition to previous state.

## Open Questions

- [ ] **Password policy configuration**: The spec says "OWASP A07 password policy" with >=12 chars, lockout 5 attempts / 5 min. Should these values come from `appsettings.json` (`Identity:Password`) or hardcoded defaults? Recommendation: read from `IConfiguration` under `Identity:Password:`.
- [ ] **Return URL after login**: The `LoginViewModel` needs a `ReturnUrl` property. Should this be preserved across failed login attempts, and what is the maximum length? Recommendation: string property, validated max 500 chars.
- [ ] **Login lockout message**: When a locked-out user tries to log in, Identity returns a lockout result. The spec says generic "Invalid email or password." — but should the error message differ if the account is locked out (to avoid user enumeration on lockout status)? Recommendation: keep generic for A07 compliance.
- [ ] **Existing `_Layout.cshtml` uses Bootstrap**: The layout references `Home` controller which doesn't exist yet. Should the layout be updated now to remove `Home` links, or left as-is for a future dashboard phase? Recommendation: leave as-is; the Account views will have their own minimal layout or the layout will be updated in the dashboard phase.
- [ ] **Error page `/Error` route conflicts**: If `UseExceptionHandler("/Error")` and `UseStatusCodePagesWithReExecute("/Error")` both use the same route, does ASP.NET Core handle the distinction correctly? Verified: `UseExceptionHandler` catches unhandled exceptions; `UseStatusCodePagesWithReExecute` catches HTTP status codes. They coexist on the same endpoint.
- [ ] **`Logout.cshtml` view**: The spec says `/Account/Logout` POST action (no view, just redirect after SignOut). Should there be a GET handler that redirects to home for direct navigation, or is POST-only acceptable? Recommendation: `[HttpGet]` returns redirect to home to avoid orphaned session if user navigates directly.
