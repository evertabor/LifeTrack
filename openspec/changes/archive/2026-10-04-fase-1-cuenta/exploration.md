# Exploration: fase-1-cuenta — Phase 1 Account

## Current State

The codebase is in a **pre-Identity bootstrap state**. Every piece required by HU-01..HU-03, the fallback authorization policy, and `/health` is **absent**:

| Component | Status |
|---|---|
| `Program.cs` | Minimal (20 lines): MVC + MediatR + anti-forgery filter only |
| `AppDbContext` | Does not exist (`Infrastructure/Persistence/` contains only `.gitkeep`) |
| `ApplicationUser` | Does not exist |
| `ICurrentUser` | Does not exist (`Application/Abstractions/` is `.gitkeep`) |
| Account screens (register/login/logout) | None; no `AccountController`, no views |
| Authorization policy (fallback = authenticated) | Not configured |
| `/health` endpoint | Not configured |
| Security headers middleware | Not implemented (`Security/` is `.gitkeep`) |
| Error page with request ID | Not implemented (`Views/Error/` is `.gitkeep`) |
| `BusinessRuleException` / `ResourceNotFoundException` | Do not exist (`Application/Common/` has `ApplicationAssemblyAnchor` only) |

**What exists:** solution structure, layer project files with declared package references, architecture test skeletons, AGENTS.md conventions, and a completed `proposal.md` for this change.

### Package readiness
The packages required for Identity are **already declared** in the `.csproj` files:
- `Infrastructure.csproj`: `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Web.csproj`: `Microsoft.AspNetCore.Identity.UI`
- `Directory.Packages.props`: all versions centrally managed

No new packages are needed for Phase 1.

---

## Affected Areas

### Must create (greenfield)
- `src/LifeTrack.Infrastructure/Persistence/AppDbContext.cs` — `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`, implements `IUnitOfWork`
- `src/LifeTrack.Infrastructure/Persistence/ApplicationUser.cs` — `IdentityUser<Guid>` subclass
- `src/LifeTrack.Infrastructure/DependencyInjection.cs` — `AddInfrastructure()` extension registering DbContext, Identity, repositories
- `src/LifeTrack.Application/Abstractions/ICurrentUser.cs` — `UserId` property, fails closed without identity
- `src/LifeTrack.Web/Services/CurrentUser.cs` — `ICurrentUser` implementation reading authenticated principal
- `src/LifeTrack.Web/Program.cs` — compose Identity, fallback authorization policy, security headers, `/health`, error handling, route auth attributes
- `src/LifeTrack.Web/Security/` — security headers middleware (CSP, X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy)
- `src/LifeTrack.Web/Controllers/ErrorController.cs` — generic error page with request ID
- `src/LifeTrack.Web/Views/Error/Index.cshtml` — error view
- Account screens: `AccountController` + views **OR** scaffolded Identity Razor Pages

### Will be modified
- `src/LifeTrack.Web/Program.cs` — adds Identity, authorization, middleware, health endpoint, route auth
- `tests/LifeTrack.Architecture.Tests/DependencyRulesTests.cs` — already has the right structure; may need a new test for the fallback policy string in source
- `appsettings.json` / user-secrets — needs `ConnectionStrings:DefaultConnection`

### Will trigger
- First EF Core migration (Identity schema)
- Tests for `ICurrentUser` fail-closed behavior

---

## Approaches

### 1. Scaffolded Identity UI (Identity Razor Pages)
Uses the built-in ASP.NET Core Identity UI via `AddDefaultIdentity` + `AddRazorPages()` + `MapRazorPages()`. Account screens live in `Pages/Identity/` (scaffolded, not in `Controllers/`).

**Pros:**
- Zero custom controller code for auth flows
- Built-in lockout, password validation, account confirmation scaffolding
- OWASP A07 compliant out of the box
- ADR-0002 already documents this as an accepted option

**Cons:**
- Mixes Razor Pages (Identity) with MVC controllers/views for the rest of the app
- CSP must allow `'unsafe-inline'` for Identity's inline styles (documented debt in ADR-0002)
- Less control over UI; harder to match project CSS conventions

**Effort:** Medium

### 2. Custom MVC AccountController
Register, login, and logout implemented as `AccountController` with Razor views in `Views/Account/`. Full control over UI and behavior.

**Pros:**
- Consistent MVC pattern across the entire application
- UI matches project CSS conventions from day one
- No Razor Pages mixture

**Cons:**
- Must implement password validation, lockout, generic error messages manually
- Higher implementation and security review burden
- More code to write and maintain

**Effort:** High

### Recommendation
**Approach 1 (Scaffolded Identity UI)** for Phase 1. It satisfies HU-01..HU-03 with the least implementation risk, the built-in lockout and password policies are OWASP A07 compliant, and the CSP inline-style debt is a known and documented trade-off (ADR-0002). The project can migrate to custom MVC accounts later if desired.

---

## `/health` Endpoint Scope

| Option | Description | Risk |
|---|---|---|
| **Liveness only** | Returns `200 OK` if the process is running | Low complexity; no state leakage; suitable for Azure App Service health probes |
| **Liveness + SQL** | Also checks SQL connection | Risk of marking serverless SQL as unhealthy when paused; potential info leakage in error messages |

**Recommendation:** Liveness only (`200 OK` with minimal JSON `{"status":"ok"}`). SQL connectivity is a deployment concern, not an application health concern at this stage. Azure App Service's health probe should hit this endpoint.

---

## Risks

1. **Open decision: AccountController vs. Identity UI.** The proposal leaves this open. Choosing Approach 2 (custom MVC) significantly increases effort and security review surface for Phase 1.

2. **First migration.** Identity schema migration will be applied before any business entities exist. Must verify migration locally and never auto-apply in production.

3. **CSP inline-style debt.** The scaffolded Identity UI uses inline styles, requiring `'unsafe-inline'` in CSP. This is a documented tradeoff (ADR-0002) but represents a hardening gap.

4. **HU-03 partial scope.** HU-03 requires that IDOR attempts return "not found" without persisting changes. Phase 1 has no business resources or handlers yet. The IDOR test pattern must be established now, but full IDOR verification awaits the business entity handlers of subsequent phases.

5. **No integration tests.** The project has only unit and architecture tests. Verifying HTTP-level behavior (redirect on unauthorized, `/health` anonymous access, cookie settings) requires either new integration tests or manual verification.

---

## Ready for Proposal

**Yes — with resolved decisions:**
1. Confirm scaffolded Identity UI (Approach 1) as the account UI strategy
2. Confirm `/health` is liveness-only with `{"status":"ok"}` payload
3. Agree that IDOR verification is deferred to per-handler tests in subsequent phases, with Phase 1 establishing the `ICurrentUser` abstraction and fail-closed behavior

The `proposal.md` already captures these; the open decisions section needs to be resolved before `sdd-spec` runs.
