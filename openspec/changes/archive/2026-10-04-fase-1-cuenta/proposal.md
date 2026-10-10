# Proposal: fase-1-cuenta — Phase 1 Account (Identity & Security Foundation)

## Intent

Establish the authentication and authorization foundation for LifeTrack: ASP.NET Core Identity with user registration, login, and logout (HU-01..HU-03) via custom MVC `AccountController` with Razor views, a fallback authorization policy requiring authentication for all controllers, `/health` liveness endpoint, security headers, and error page with request-ID correlation. This is greenfield — no Identity, DbContext, or account screens exist.

## Scope

### In Scope

- `AppDbContext` as `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>` with `IUnitOfWork` implementation
- `ApplicationUser` entity extending `IdentityUser<Guid>`
- `AddInfrastructure()` extension in Infrastructure layer registering DbContext, Identity, and repository contracts
- `ICurrentUser` abstraction in Application layer (`UserId` property; fails closed when no authenticated principal)
- `CurrentUser` service implementation in Web layer reading the authenticated `ClaimsPrincipal`
- Custom MVC `AccountController` with Razor views for HU-01 (registro), HU-02 (inicio de sesión), and HU-03 (cierre de sesión)
- Fallback authorization policy: `policy.RequireAuthenticatedUser()` applied globally as default
- `/health` liveness endpoint returning `200 OK` with `{"status":"ok"}` — anonymous-accessible, no SQL check
- Security headers middleware: CSP (with `'unsafe-inline'` for Identity UI), X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy
- `ErrorController` + `Views/Error/Index.cshtml` with request-ID correlation
- `BusinessRuleException` and `ResourceNotFoundException` base types in Application/Common/Exceptions
- `Program.cs` composition: Identity, authorization, security middleware, health endpoint, error handling
- First EF Core migration for Identity schema

### Out of Scope

- Scaffolded Identity Razor Pages UI — using custom MVC pattern for consistency
- `/health` extended with SQL connectivity check — liveness-only is sufficient for Azure App Service probes
- Business entity IDOR protection tests — `ICurrentUser` fail-closed pattern established now; IDOR verification per entity deferred to handler phases
- User-secrets or connection string configuration — noted as required but provided by environment/operator
- Integration tests for HTTP-level behavior (redirect on unauthorized, anonymous `/health`, cookie settings)

---

## Capabilities

### New Capabilities

- `account-identity`: Full user-account lifecycle — registration with password, login, logout — using ASP.NET Core Identity with custom MVC `AccountController` and Razor views. Covers HU-01 (registro), HU-02 (inicio de sesión), and HU-03 (cierre de sesión). Enforces OWASP A07 password policy and account lockout.
- `auth-fallback-policy`: Global fallback authorization policy requiring an authenticated user for all controller routes. Anonymous access permitted only on `/health` and paths covered by `AllowAnonymous`.
- `health-endpoint`: Anonymous-accessible liveness endpoint at `/health` returning `200 OK` with `{"status":"ok"}`. No SQL query; suitable for Azure App Service probes.
- `current-user`: `ICurrentUser` abstraction exposing the current authenticated user's `UserId` (Guid). Fails closed (returns null/throws) when no `ClaimsPrincipal` is present.
- `security-headers`: HTTP security header middleware applied to all responses: CSP, X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy.
- `error-page`: Application error page displaying a request-ID for correlation, without leaking internal stack traces.

### Modified Capabilities

- None — this is a greenfield change; no existing spec-level capabilities are being altered.

---

## Approach

1. **Infrastructure layer**: Create `ApplicationUser` and `AppDbContext` (IdentityDbContext), implement `AddInfrastructure()` extension registering EF Core + Identity services.
2. **Application layer**: Add `ICurrentUser`, `BusinessRuleException`, `ResourceNotFoundException` base types.
3. **Web layer**: Create `CurrentUser` service and custom `AccountController` with `RegisterViewModel`, `LoginViewModel`, and `LogoutViewModel`. Update `Program.cs` to compose `AddInfrastructure()`, `AddIdentity()`, global auth policy, security headers middleware, `/health` endpoint, and error handling.
4. **Account views**: Create Razor views for register, login, and logout under `Views/Account/`. Follow `docs/09-estandar-vistas-mvc.md` — views render only, all formatting done in ViewModels.
5. **EF Core migration**: Create and verify the initial Identity schema migration locally; do not auto-apply in production.
6. **Security headers + error page**: Implement dedicated middleware/files per the affected areas.

---

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/LifeTrack.Infrastructure/Persistence/AppDbContext.cs` | New | `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`, `IUnitOfWork` |
| `src/LifeTrack.Infrastructure/Persistence/ApplicationUser.cs` | New | `IdentityUser<Guid>` subclass |
| `src/LifeTrack.Infrastructure/DependencyInjection.cs` | New | `AddInfrastructure()` extension method |
| `src/LifeTrack.Application/Abstractions/ICurrentUser.cs` | New | `UserId` property, fail-closed |
| `src/LifeTrack.Application/Common/Exceptions/BusinessRuleException.cs` | New | Base exception for domain rule violations |
| `src/LifeTrack.Application/Common/Exceptions/ResourceNotFoundException.cs` | New | Base exception for resource-not-found |
| `src/LifeTrack.Web/Services/CurrentUser.cs` | New | `ICurrentUser` implementation |
| `src/LifeTrack.Web/Controllers/AccountController.cs` | New | Custom MVC controller for register, login, logout (HU-01..HU-03) |
| `src/LifeTrack.Web/ViewModels/Account/` | New | RegisterViewModel, LoginViewModel, LogoutViewModel with `From(...)` factories |
| `src/LifeTrack.Web/Views/Account/` | New | Register.cshtml, Login.cshtml, Logout.cshtml Razor views (logic-free rendering) |
| `src/LifeTrack.Web/Program.cs` | Modified | Identity, auth policy, middleware, health, error handling |
| `src/LifeTrack.Web/Security/SecurityHeadersMiddleware.cs` | New | CSP, X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy |
| `src/LifeTrack.Web/Controllers/ErrorController.cs` | New | Generic error with request-ID |
| `src/LifeTrack.Web/Views/Error/Index.cshtml` | New | Error view |
| `tests/LifeTrack.Architecture.Tests/DependencyRulesTests.cs` | Modified | May need test for fallback policy string |

---

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| First Identity migration blast radius | Medium | Create migration locally; verify schema before any subsequent migration; never auto-apply in prod |
| CSP `'unsafe-inline'` required for Identity UI | High | Document as hardening debt; ADR-0002 already captures the tradeoff; plan CSP refinement in a later hardening phase |
| IDOR pattern deferred to subsequent phases | Low | Establish `ICurrentUser` fail-closed now; document that per-entity IDOR tests are a later phase deliverable |
| No integration tests for HTTP-level behavior | Low | Document as gap; manual verification required at apply time; integration tests can be added in a follow-up |
| Custom MVC implementation effort | Medium | Follow existing controller patterns from AGENTS.md; use MediatR `ISender` for all use-case calls |

---

## Rollback Plan

1. Revert or delete the EF Core migration that added Identity tables (`dotnet ef migrations remove` locally; drop Identity tables in prod if already applied).
2. Remove Identity from `Program.cs`: delete `AddInfrastructure()`, `AddIdentity()` calls and the global `RequireAuthenticatedUser()` policy.
3. Delete the created files: `AppDbContext.cs`, `ApplicationUser.cs`, `DependencyInjection.cs`, `ICurrentUser.cs`, `CurrentUser.cs`, `BusinessRuleException.cs`, `ResourceNotFoundException.cs`, `SecurityHeadersMiddleware.cs`, `ErrorController.cs`, `Error/Index.cshtml`, `AccountController.cs`, `ViewModels/Account/*.cs`, and `Views/Account/*.cshtml`.
4. Re-run `dotnet build` to confirm clean state.
5. If a migration was applied to a shared dev/staging DB, coordinate with the team to roll back the migration before reverting code.

---

## Dependencies

- .NET 10 SDK
- SQL Server or Azure SQL database accessible via `ConnectionStrings:DefaultConnection`
- Package references already declared: `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.AspNetCore.Identity.UI`

---

## Success Criteria

- [ ] `dotnet build -c Release` succeeds with no warnings in `LifeTrack.Web` and `LifeTrack.Infrastructure`
- [ ] `dotnet test` passes all architecture and unit tests
- [ ] `/health` returns `200 OK` with `{"status":"ok"}` without authentication
- [ ] Unauthenticated requests to any controller action return `401` or redirect to login
- [ ] A user can register with email + password and receive a `401` when accessing a protected resource before login
- [ ] A registered user can log in and subsequently access a protected resource
- [ ] A logged-in user can log out and subsequently receive `401` on protected resources
- [ ] Error page displays a unique request-ID without leaking stack trace
- [ ] First EF Core migration created and verified locally; Identity schema confirmed via `dotnet ef dbcontext info`
- [ ] Custom MVC `AccountController` uses `ISender` (MediatR) for all use-case calls; no direct repository access in controller
