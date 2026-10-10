# AGENTS.md — LifeTrack.Web

Reglas generales en `/AGENTS.md`. Esta capa es un **adaptador HTTP**: traduce peticiones a casos de uso.

## Responsabilidades
- `Program.cs` es la **raíz de composición**: único lugar que conoce todas las capas.
- `Program.cs` registra MediatR con `AddMediatR`, apuntando el escaneo al ensamblado de `Application` (vía `LifeTrack.Application.Common.ApplicationAssemblyAnchor`). **Nunca** escanees `typeof(Program).Assembly`: los handlers no viven aquí y no se registraría ninguno. La clave de licencia se lee de `MediatR:LicenseKey` y solo es necesaria en producción.
- `Services/CurrentUserService` implementa `ICurrentUser` leyendo el claim del usuario autenticado; **falla cerrado** si no hay identidad.
- `Security/` contiene el middleware de cabeceras (CSP, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`).
- `TagHelpers/` contiene componentes Razor propios reutilizables en varias vistas.
- `Extensions/` contiene la configuración de la aplicación. **No** registra rutas a mano: van por atributo.

## Controllers (delgados)
- **Uno por área**: `DashboardController`, `CategoriesController`, `TransactionsController`, `HabitsController`, `AccountController`. Nombres en inglés y en **plural**.
- Inyectan **`ISender`** (MediatR) y **`ILogger<TController>`**. **Nunca** un handler concreto, ni `DbContext`, ni repositorios, ni `UserManager` para datos de negocio. Con `ISender` el número de dependencias del controller no crece con el número de acciones.
- Su trabajo es: recibir la petición, enviar el contrato del caso de uso con `await sender.Send(new <Verbo><Sustantivo>Command/Query(...), cancellationToken)`, mapear el resultado a un `ViewModel` y devolver la vista.
- Enlazan formularios a un **`InputModel`** con DataAnnotations (`[BindProperty]`), jamás a entidades. El `InputModel` valida **presencia y formato** (`[Required]`), no invariantes de negocio: esas las valida el dominio y su mensaje vuelve ya traducido como `BusinessRuleException`.
- Los input models de screen (formularios GET/POST) **nunca** son clases anidadas dentro de un controller. Pertenecen a `ViewModels/<Area>/` bajo el namespace `LifeTrack.Web.ViewModels.<Area>`.
- Captura de errores: `BusinessRuleException` → mensaje en `ModelState` o `TempData` (es seguro mostrarlo); `ResourceNotFoundException` → `LogWarning` con plantilla estructurada + mensaje genérico (posible manipulación de ids); cualquier otra excepción sube al manejador global. **Ninguna excepción de `Domain`**: `Web` no referencia `Domain`, y el handler ya las tradujo.
- Patrón Post/Redirect/Get tras un POST correcto.
- Sin lógica de negocio ni consultas.
- **Cero comentarios** en el código de esta capa: sin XML, sin `//`, sin `/* */` y sin `@* *@` en las vistas. Si una restricción no se puede expresar en el código, va en `docs/`.
- **Rutas por atributo**, con `[Route]` en el controller o en la acción. URLs en inglés. Sin ruta convencional `{controller}/{action}`.

## ViewModels (solo para pintar)
- Viven en `ViewModels/<Area>/`, fuera de `Views/`.
- Se construyen con una **fábrica estática `From(...)`** a partir del DTO devuelto por el caso de uso, o de una proyección `Select` si el caso de uso ya devuelve un DTO.
- **No heredan** de una entidad de `Domain` ni de un DTO de Application.
- **Entregan los datos ya listos para pintar**: importes y fechas formateados (`"N2"` en `es-ES`), rótulos construidos y booleanos de visibilidad. Una vista no calcula nada.
- Sin comportamiento de negocio y sin validaciones: validan los `InputModel`, no los `ViewModel`.
- Nombre de pantalla `<Area>ViewModel`; los de parciales compartidos, descriptivos.

## Vistas Razor
Norma completa en `docs/09-estandar-vistas-mvc.md`.

- **Cero lógica en la vista.** Sin ternarios, sin declaraciones de variables, sin llamadas que calculen o formateen, sin concatenación para construir texto. `@if` y `@else` solo para elegir qué se renderizar.
- Una vista principal **compone** parciales; no pinta cada bloque a mano.
- Todo texto visible en español, identificadores en inglés. HTML semántico, `<label for>` en cada campo, mensajes de validación junto al campo, navegable por teclado, *mobile-first*.
- Codificación por defecto de Razor. **Prohibido `Html.Raw`** con datos de usuario y `<script>`/`style` en línea (la CSP bloquea scripts en línea). Librerías JS/CSS auto-alojadas en `wwwroot/lib`, no desde CDN.
- Formularios con tag helpers (`asp-for`, `asp-action`, `asp-route-*`) para que se genere el token anti-forgery. No uses `[IgnoreAntiforgeryToken]` salvo en la página de error.
- **Dos layouts con responsabilidades separadas**: `Views/Shared/_Layout.cshtml` es el layout público por defecto y `Views/Shared/_AuthenticatedLayout.cshtml` es el shell de las áreas autenticadas. Los `_ViewStart.cshtml` anidados seleccionan el segundo layout; nunca se decide por `User-Agent`.
- Clases CSS y atributos `data-*` con prefijo de feature (`app-` para componentes compartidos), según `docs/08-estandar-javascript.md`.
- Ninguna entidad de `Domain` en una vista, un parcial o un `ViewModel`.
- La carpeta `Pages/` no se usa: solo aparece cuando la UI scaffolded de Identity se añade al repositorio.

## JavaScript
Norma completa en `docs/08-estandar-javascript.md`.

- Un fichero por pantalla en `wwwroot/js/pages/`, un fichero por componente en `wwwroot/js/shared/`. Sin ficheros "generales" que mezclen features.
- Todo script es una IIFE con `"use strict"`. `_` marca funciones privadas; lo público va sin `_`.
- Bloques `SELECTORS`, `CSS_CLASSES` y `CONSTANTS` al principio del fichero, y **ningún** selector, clase, id, clave `data-*` o literal fuera de ellos.
- Un único punto de entrada `_initialize()`, solo en scripts de página. Los componentes compartidos se enganchan con listeners delegados y no lo tienen.
- Sin `var`, sin `<script>` en línea, sin CDN. Todos los scripts con `defer`.

## Seguridad HTTP (en `Program.cs`)
Identity con contraseña ≥ 12 y bloqueo, cookie `HttpOnly`/`Secure`/`SameSite`, política de autorización *fallback* = autenticado, `UseHttpsRedirection`, `UseHsts` y `UseExceptionHandler("/Error")` fuera de Development, `UseRateLimiter`, `AllowedHosts` restringido, `/health` anónimo. Migraciones automáticas **solo** en Development.

**`AutoValidateAntiforgeryToken` registrado como filtro global.** En MVC el anti-forgery no es automático: sin ese filtro cualquier POST quedaría sin token. `LifeTrack.Architecture.Tests` verifica que sigue registrado, y está prohibido desactivarlo o excluir una acción para "hacerla funcionar".

## Controllers y vistas previstas
`DashboardController` + `Views/Dashboard/Index.cshtml`, `CategoriesController` + `Views/Categories/{Index,Edit}`, `TransactionsController` + `Views/Transactions/{Index,Edit}`, `HabitsController` + `Views/Habits/Index`, `AccountController` para registro, acceso y borrado de cuenta. Compartidos: `Views/Shared/{_Layout,_AuthenticatedLayout}.cshtml`, `Views/Shared/Partials/`, `Views/Shared/Components/` y `Views/Error/`.
