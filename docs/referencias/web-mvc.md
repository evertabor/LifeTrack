# Parte E — Web  ⚠️ sin compilar (las páginas Razor sí compilaron en un proyecto de prueba sin Identity)

## CurrentUserService (falla cerrado)

Sin identidad no hay datos: si el claim no existe o no es un `Guid`, la propiedad **lanza** en lugar
de devolver `Guid.Empty`. Devolver un id vacío sería devolver los datos de un usuario que no existe,
así que el fallo es deliberadamente ruidoso (OWASP A01).

`src/LifeTrack.Web/Services/CurrentUserService.cs`

```csharp
using System.Security.Claims;
using LifeTrack.Application.Abstractions;

namespace LifeTrack.Web.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(value, out var id)
                ? id
                : throw new UnauthorizedAccessException("There is no authenticated user.");
        }
    }
}
```

## Cabeceras de seguridad y CSP

La CSP **no permite scripts en línea ni hosts de terceros**: cualquier librería JS o CSS se
auto-aloja en `wwwroot/lib`. `unsafe-inline` se conserva **solo para estilos**, porque la UI
scaffolded de Identity usa algunos estilos en línea; si esa UI se sustituye, el siguiente paso es
endurecerlo con nonces. `X-Frame-Options`, `Referrer-Policy` y `Permissions-Policy` cierran el
*clickjacking*, la fuga de URL y el acceso a cámara, micrófono y geolocalización.

`src/LifeTrack.Web/Security/SecurityHeadersExtensions.cs`

```csharp
namespace LifeTrack.Web.Security;

public static class SecurityHeadersExtensions
{
    private const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "connect-src 'self'; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'";

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers["Content-Security-Policy"] = ContentSecurityPolicy;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";

            await next();
        });
}
```

## Program.cs (Identity, cookies, autorización por defecto, límite de tasa)

`Program.cs` es la **raíz de composición**: el único fichero que conoce las cuatro capas. Es también
el único sitio donde se decide qué se escanea, qué se registra y en qué orden se aplica el middleware;
por eso la configuración se lee aquí y no en las capas.

Lo que hace cada bloque, y por qué:

| Bloque | Qué resuelve |
|---|---|
| `AddMediatR` | Registra `ISender`, `IPublisher` y el agregado *mediator* como *transient*, y escana el ensamblado indicado buscando implementaciones concretas de `IRequestHandler<,>`, `IRequestHandler<>`, `INotificationHandler<>`, `IStreamRequestHandler<>` e `IRequestExceptionHandler<,,>` |
| `cfg.LicenseKey` | La clave de licencia solo es necesaria en despliegues de **producción**: en local y en CI, MediatR escribe como mucho un warning |
| `AddDefaultIdentity` | Identidad con contraseña ≥ 12, email único, bloqueo tras 5 intentos y 15 minutos de bloqueo. **OWASP A07** |
| `ConfigureApplicationCookie` | Cookie `HttpOnly`, `Secure` siempre y `SameSite=Lax`: el *token* no lo puede leer JavaScript ni viajar en peticiones de terceros |
| `AddAuthorizationBuilder` | Política *fallback* = usuario autenticado, es decir **denegar por defecto** (OWASP A01). Toda página pública debe decirlo con `[AllowAnonymous]` |
| `AddRateLimiter` | 100 peticiones por minuto y por IP, sin cola: una protección básica contra abuso (OWASP A06) |
| `AddControllersWithViews` | Registra `AutoValidateAntiforgeryToken` como **filtro global**. En MVC el anti-forgery **no** es automático (OWASP A08): sin ese filtro cualquier POST quedaría sin validar, y los tests de arquitectura comprueban que sigue registrado |
| `AddRazorPages` | Existe **solo** porque la UI scaffolded de ASP.NET Core Identity es Razor Pages. La aplicación usa controllers y vistas MVC |
| Migraciones en `Development` | Migrar al arrancar es cómodo en local, pero **nunca en producción**: allí las migraciones las aplica el pipeline (ver la guía de [despliegue](../06-despliegue-azure.md)) |
| `UseExceptionHandler("/Error")` | Página de error genérica: nunca se muestra una traza de pila (OWASP A02/A10) |
| `public partial class Program` | Hace visible el punto de entrada a `WebApplicationFactory<Program>` en los tests de integración. Sin esta declaración parcial la clase es interna y el *factory* no puede construir la aplicación |

Dos decisiones que este fichero deja escritas y que conviene no perder al copiarlo:

- El escaneo de MediatR apunta a **`typeof(ApplicationAssemblyAnchor).Assembly`**, es decir a
  `Application`, **nunca a `typeof(Program).Assembly`**. Escanear el ensamblado del punto de entrada
  compila perfectamente y no registra ningún handler, porque los handlers viven en `Application`: el
  fallo aparece en el primer `Send`, no en el build.
- `IPipelineBehavior`, los *behaviors* y los pre/post processors **no** se descubren escaneando:
  requieren su propio `Add…` explícito. Este proyecto no registra ninguno, y si se añadiera sería con
  un registro explícito, nunca como efecto secundario del escaneo.
- `RequireConfirmedAccount` está en `false` porque **no hay emisor de correo configurado**. Se activa
  en cuanto lo haya; hoy exigir la confirmación dejaría al usuario fuera de la aplicación.

`src/LifeTrack.Web/Program.cs`

```csharp
using System.Threading.RateLimiting;
using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using LifeTrack.Infrastructure;
using LifeTrack.Infrastructure.Persistence;
using LifeTrack.Web.Security;
using LifeTrack.Web.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(cfg =>
{
    cfg.LicenseKey = builder.Configuration["MediatR:LicenseKey"] ?? string.Empty;
    cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyAnchor).Assembly);
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUserService>();

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 12;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddRazorPages();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseSecurityHeaders();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();
app.MapHealthChecks("/health").AllowAnonymous();

await app.RunAsync();

public partial class Program
{
}
```

## Plantillas de vistas (obligatorias en toda pantalla)

Estas tres plantillas son la base de `Views/`. Sin ellas ninguna vista hereda los tag helpers ni el
layout, y el modelo de vista tampoco se resuelve.

`src/LifeTrack.Web/Views/_ViewImports.cshtml`

```html
@using LifeTrack.Web
@using LifeTrack.Web.ViewModels
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

Los `using` globales cubren los `ViewModel` de todas las áreas; al añadir una pantalla se amplía la
lista, y nunca se importa aquí nada del proyecto de dominio.

`src/LifeTrack.Web/Views/_ViewStart.cshtml`

```html
@{
    Layout = "_Layout";
}
```

Un solo layout para toda la aplicación, aplicado automáticamente. Ninguna vista repite `html`,
`head`, header ni footer, y **prohibido** decidir por `User-Agent`.

`src/LifeTrack.Web/Views/Shared/_Layout.cshtml`

```html
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>@ViewData["Title"] - LifeTrack</title>
    <link rel="stylesheet" href="~/css/site.css" />
</head>
<body class="app-body">
    <partial name="_Header" />

    <main id="main-content" class="app-main">
        @RenderBody()
    </main>

    <partial name="_Footer" />

    <script src="~/js/shared/toast.js" defer></script>
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

El layout es **mobile-first**: el CSS base de `wwwroot/css/site.css` está pensado para móvil y los
`min-width` amplían; aquí no hay media queries. Las clases usan el prefijo `app-` de los componentes
compartidos (`app-body`, `app-main`), de modo que ninguna vista ni script tenga que repetir literales
sueltos. El partial `_Header` es quien lleva la navegación y los scripts de cada pantalla.

La hoja de estilos es **auto-alojada** porque la CSP solo admite `script-src 'self'` y `style-src
'self'`: no hay CDN ni hojas en línea. El partial `_Header` es quien lleva la navegación y los
scripts de cada pantalla.

## Controller delgado (InputModel, captura de excepciones, log de seguridad)

El controller solo conoce `LifeTrack.Application`: no importa nada del proyecto de dominio y por eso
captura `BusinessRuleException` y `ResourceNotFoundException`. Esas dos son las excepciones de
frontera que declara `Application`; el handler ya tradujo las suyas antes de devolver el error.

Inyecta **`ISender`** y `ILogger<TController>`, y **nunca** un handler concreto: MediatR resuelve el
handler a partir del contrato en tiempo de ejecución. Así el número de dependencias del controller no
crece con el número de acciones.
