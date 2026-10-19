# Estándar de vistas MVC

Norma obligatoria para `LifeTrack.Web`: controladores, `ViewModels`, vistas, parciales y layouts.
Los identificadores van en **inglés**; los textos visibles al usuario, en **español**.

## 1. Estructura de `LifeTrack.Web`

```
src/LifeTrack.Web/
├── Controllers/              # Un controller por área. Reciben la petición y devuelven un ViewModel
│   ├── DashboardController.cs
│   ├── CategoriesController.cs
│   ├── TransactionsController.cs
│   └── AccountController.cs
├── ViewModels/               # Un modelo por pantalla. Solo existen para pintar
│   ├── Dashboard/
│   ├── Categories/
│   ├── Transactions/
│   └── Shared/
├── Views/
│   ├── _ViewImports.cshtml
│   ├── _ViewStart.cshtml         # Layout público por defecto
│   ├── Dashboard/
│   │   ├── Index.cshtml
│   │   └── Partials/
│   ├── Categories/
│   │   ├── Index.cshtml
│   │   ├── Edit.cshtml
│   │   └── Partials/
│   ├── Transactions/
│   │   ├── Index.cshtml
│   │   ├── Edit.cshtml
│   │   └── Partials/
│   ├── Error/
│   └── Shared/
│       ├── _Layout.cshtml         # Layout público por defecto
│       ├── _AuthenticatedLayout.cshtml
│       ├── Partials/              # Shells y bloques compartidos
│       └── Components/       # ViewComponents reutilizables
├── Services/                 # CurrentUserService. Solo presentación
├── TagHelpers/               # Componentes Razor propios
├── Security/                 # Middleware de cabeceras
├── Composition/              # Registro de handlers y de servicios de Application
├── Extensions/               # Configuración de la aplicación
└── wwwroot/
    ├── css/
    ├── js/
    └── lib/
```

### La carpeta `Pages/`

`Pages/` **no forma parte de la aplicación**. La única UI de Identity que usa ASP.NET Core Identity
viene en Razor Pages, y hasta que no se aparezca en el repositorio no debe existir esa carpeta.
Ninguna vista, parcial o layout propio vive ahí.

## 2. El flujo de una pantalla

```
Navegador
   ↓  [Route] sobre el controller o la acción
Controller            invoca un handler de Application y mapea el resultado
   ↓
ViewModel            datos ya listos para pintar
   ↓
Vista principal      compone y delega; no calcula
   ↓
Partials             cada bloque con su fichero
   ↓
Layout               estructura común
```

## 3. Para trabajar en una pantalla, busca estas cuatro cosas

| # | Elemento | Dónde |
|---|---|---|
| 1 | **Controller** | `Controllers/<Area>Controller.cs` |
| 2 | **ViewModel** | `ViewModels/<Area>/` |
| 3 | **Vista principal** | `Views/<Area>/Index.cshtml` |
| 4 | **Parciales que usa** | `Views/<Area>/Partials/` |

URL, controller, `ViewModel` y vista forman una unidad funcional. Las cuatro cosas comparten el
mismo nombre de área, y por eso se localizan juntas.

## 4. Controllers

- **Uno por área**, no uno por acción. Editar y eliminar viven en `TransactionsController`.
- Los nombres van en inglés y en plural: `CategoriesController`, no `CategoryController`.
- Inyectan **`ISender`** (MediatR) y **`ILogger<TController>`** (más `TimeProvider` si lo necesitan).
  **Nunca** un handler concreto, ni `DbContext`, ni repositorios, ni `UserManager` para datos de negocio.
- **Delgados.** Su trabajo es: recibir, invocar un handler, mapear a `ViewModel`, devolver la vista.
- Enlazan formularios a un `InputModel` dedicado con DataAnnotations, **nunca a una entidad**.
- Cada método que recibe un id filtra por `ICurrentUser` a través del handler y necesita su test de IDOR.
- Captura de errores: `BusinessRuleException` → su mensaje en `ModelState` o `TempData` (es seguro mostrarlo);
  `ResourceNotFoundException` → `LogWarning` con plantilla estructurada y mensaje genérico en la vista, porque
  puede ser manipulación de ids; cualquier otra excepción sube al manejador global. Las excepciones de `Domain`
  **no** se capturan aquí: `Web` no referencia `Domain` y el handler ya las tradujo.
- **Post/Redirect/Get** tras un POST correcto.
- **Sin lógica de negocio ni consultas.**

### Rutas

Con atributo, sobre el controller o sobre la acción. **Prohibido** registrar rutas a mano en un
fichero de extensiones, y **prohibida** la ruta convencional `{controller}/{action}` como referencia.

```csharp
[Route("transactions")]
public sealed class TransactionsController : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) { /* ... */ }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTransactionInputModel input, CancellationToken cancellationToken) { /* ... */ }
}
```

Las URLs van en **inglés** y son definitivas: `/transactions`, `/categories`, `/dashboard`.

## 5. ViewModels

> Un `ViewModel` es una clase que existe **únicamente para pintar datos en una vista**.

- Viven en `ViewModels/<Area>/`, fuera de `Views/`.
- **Prohibido** heredar de una entidad de `Domain` o de un DTO de Application. Se construyen desde el
  resultado del handler.
- **Prohibido** que una entidad de `Domain` llegue a la vista. Nunca.
- No tienen comportamiento de negocio. No validan reglas: solo exponen datos.

### Los datos llegan listos para pintar

Como las vistas no pueden calcular (sección 6), el `ViewModel` entrega los valores ya formados.

```csharp
public sealed record MonthlySummaryViewModel
{
    public required string IncomeFormatted { get; init; }
    public required string ExpensesFormatted { get; init; }
    public required string BalanceFormatted { get; init; }
    public bool HasExpenses { get; init; }

    public static MonthlySummaryViewModel From(MonthlySummaryResult result)
    {
        var culture = CultureInfo.GetCultureInfo("es-ES");

        return new MonthlySummaryViewModel
        {
            IncomeFormatted = result.Income.ToString("N2", culture),
            ExpensesFormatted = result.Expenses.ToString("N2", culture),
            BalanceFormatted = result.Balance.ToString("N2", culture),
            HasExpenses = result.Expenses > 0m
        };
    };
}
```

La vista escribe `@Model.Summary.BalanceFormatted` y nada más. Si alguna vez necesitas escribir
`@Model.Summary.Balance.ToString("N2")` en una vista, lo que falta está en el `ViewModel`.

### Fábrica estática

El mapeo vive en una fábrica estática `From(...)` en el propio `ViewModel`. Así el mapeo se lee
junto al modelo, sin ficheros de mapeo adicionales, y se puede probar sin depender del controller.

### Nombres

- Pantalla principal: `<Area>ViewModel` — `DashboardViewModel`, `TransactionsViewModel`.
- Modelo de un parcial compartido: nombre descriptivo — `CategoryBreakdownViewModel`,
  `EmptyStateViewModel`.

### `InputModel`

Los formularios se enlazan a un `InputModel` con DataAnnotations que vive en su propio archivo bajo
`ViewModels/<Area>/`, nunca como clase anidada dentro de un controller. Es la
excepción a "solo para pintar": los `InputModel` reciben datos y los validan en el borde.

```csharp
namespace LifeTrack.Web.ViewModels.Transactions;

public sealed record CreateTransactionInputModel
{
    [Required(ErrorMessage = "La categoría es obligatoria.")]
    public Guid? CategoryId { get; init; }
}
```

## 6. Vistas: cero lógica

Una vista **solo renderiza**. No calcula, no decide y no declara.

### Permitido

- HTML semántico
- `@model`, `@using`, `@inject` — declaraciones de **tipo**, no de valor
- `@foreach` y `@for` recorriendo colecciones del `ViewModel`
- Leer una propiedad: `@Model.Summary.BalanceFormatted`
- Tag Helpers: `<partial>`, `<vc:...>`, `<input asp-for="...">`
- `@if` y `@else` **exclusivamente para elegir qué se renderiza**
- `@await Component.InvokeAsync(...)`

### Prohibido

| Prohibido | Ejemplo |
|---|---|
| Ternarios | `@(Model.HasExpenses ? Model.BalanceFormatted : "—")` |
| Declarar variables | `@var total = Model.Items.Count` |
| Bloques de código | `@{ var x = 1; }` |
| Llamadas que calculan | `@Model.Expenses.Sum(e => e.Amount)` |
| Formatear en la vista | `@Model.Amount.ToString("C2")` |
| Filtrar u ordenar en la vista | `@Model.Items.Where(x => x.Selected)` |
| Asignaciones | `@Model.IsChecked = true` |
| Concatenar para construir texto | `@($"{Model.Name} — {Model.Amount}")` |
| `switch`, `try`, `lock` | — |

### `@if` solo para renderizar

`@if` está permitido para **elegir qué se pinta**, nunca para calcular. El primer bloque es
**correcto**: elige entre pintarlo y no pintarlo, y el dato (`HasExpenses`) ya viene calculado. El
segundo es **prohibido**: la vista decide y calcula.

```html
@if (Model.Summary.HasExpenses)
{
    <partial name="_ExpenseBreakdown" model="Model.Summary.Breakdown" />
}
```

```html
@if (Model.Expenses.Sum(e => e.Amount) > 1000m)
{
    <p>Has gastado mucho</p>
}
```

La segunda versión no es solo "más larga": rompe la regla de que el texto visible al usuario se
prepara en el `ViewModel`, y esconde una regla de presentación que debería estar en un test.

### Ejemplo: una vista principal compone, no pinta tarjetas

```html
@model DashboardViewModel
@{
    ViewData["Title"] = "Resumen";
}

<partial name="_MonthlySummary" model="Model.Summary" />
<partial name="_BudgetStatus" model="Model.Budgets" />
<partial name="_BalanceProjection" model="Model.Projection" />
```

Cada bloque tiene su fichero en `Partials/`. La vista principal no repite el HTML de cada tarjeta,
no abre bucles anidados y no formatea importes.

## 7. Parciales y ViewComponents

| | Parcial | ViewComponent |
|---|---|---|
| **Es** | Fragmento de HTML con un `model` explícito | Clase con lógica y plantilla |
| **Modela** con | Un `ViewModel` ya listo | Los datos que le pases |
| **Decide** | Nada | Cómo mostrar el bloque |
| **Ejemplo** | `_MonthlySummary.cshtml` | `EmptyStateViewComponent` |

- Un parcial representa una tarjeta, una sección, un formulario, un estado vacío o una parte del layout.
- **Prohibido** un parcial que reciba una entidad de `Domain`.
- Un `ViewComponent` es la salida cuando hace falta una decisión: un bloque vacío, una insignia con
  color según el porcentaje. Esa decisión vive en la clase,
  nunca en la vista.

## 8. Layouts

Hay dos layouts con responsabilidades separadas:

- `Views/Shared/_Layout.cshtml` es el layout público por defecto para Account, Error y vistas anónimas. No contiene navegación privada.
- `Views/Shared/_AuthenticatedLayout.cshtml` es el shell de las áreas autenticadas. Incluye skip link, sidebar, topbar, contenido y footer mediante parciales de `Views/Shared/Partials/`.

`Views/_ViewStart.cshtml` conserva `_Layout` como valor predeterminado. Cada área autenticada tiene un `_ViewStart.cshtml` anidado con `Layout = "_AuthenticatedLayout"`; así las vistas públicas no heredan navegación privada y no se decide por `User-Agent`.

```html
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>@ViewData["Title"] - LifeTrack</title>
    <link rel="stylesheet" href="~/css/site.css" />
</head>
<body>
    <partial name="_PublicHeader" />
    <main id="main-content" class="app-main">
        @RenderBody()
    </main>
    <partial name="_AppFooter" />
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

La adaptación a móvil es `mobile-first` con media queries en `wwwroot/css/`. El shell autenticado convierte la sidebar en un cajón móvil operable por teclado; no se duplica la navegación en otra plantilla.

## 9. Formularios y anti-forgery

- Tag Helpers siempre: `asp-for`, `asp-action`, `asp-route-*`. Así se emite el token anti-forgery.
- **Prohibido** `<form>` sin tag Helpers en un POST.
- **Prohibido** `[IgnoreAntiforgeryToken]`, salvo en la página de error.
- Además del tag Helper, `Program.cs` registra **`AutoValidateAntiforgeryToken` como filtro global**.
  En MVC el anti-forgery no es automático: sin ese filtro, cualquier POST quedaría sin token.

## 10. Accesibilidad e idioma

- `<label for>` en **cada** campo. Sin excepción.
- Mensajes de validación junto al campo, y `aria-describedby` cuando el mensaje no es visible.
- HTML semántico: `header`, `nav`, `main`, `section`, `table` con `th` y `scope`.
- Navegable por teclado y con foco visible.
- Contraste suficiente y objetivo **WCAG 2.2 nivel AA**.
- `mobile-first`: el CSS base es para móvil y los `min-width` amplían.
- Todo texto visible en **español**. Todos los identificadores en **inglés**.
- Nunca `Html.Raw` con datos de usuario. Nunca `<script>` ni `style` en línea.
- **Cero comentarios** en los `.cshtml`: ni `@* *@`, ni `<!-- -->`, ni `TODO`, ni HTML comentado.

## 11. Clases CSS y atributos `data-*`

Siguen el estándar de JavaScript: prefijo de la feature y nunca un literal suelto en el código.

| Prefijo | Se usa en |
|---|---|
| `app-*` | Elementos de un componente compartido |
| `dashboard-*`, `categories-*`, `transactions-*` | Pantallas propias |

```html
<button type="button" class="transactions-edit-button" data-modal-open="confirm-delete">
    Eliminar
</button>
```

## 12. Lista antes de entregar

- [ ] La pantalla tiene sus cuatro piezas: controller, `ViewModel`, vista principal y parciales.
- [ ] El controller es delgado y solo habla con handlers de Application.
- [ ] El `ViewModel` se construye con `From(...)` y no hereda de nada de `Domain`.
- [ ] La vista no tiene ternarios, ni `var`, ni llamadas que calculen, ni formato.
- [ ] Los importes y las fechas llegan ya formateados en el `ViewModel`.
- [ ] Los `@if` solo eligen qué se renderiza.
- [ ] Los formularios usan tag Helpers y no llevan `[IgnoreAntiforgeryToken]`.
- [ ] Ninguna entidad de `Domain` llega a la vista ni a un parcial.
- [ ] Todo texto visible en español; identificadores en inglés.
- [ ] `<label for>` en cada campo, y contraste suficiente.
- [ ] Cero comentarios en la vista y en los parciales.

## 13. Prohibiciones

- Lógica de negocio, consultas o cálculos en cualquier `.cshtml`.
- Entidades de `Domain` en una vista, en un parcial o en un `ViewModel`.
- Enlazar formularios a entidades con `[BindProperty]` (usa `InputModel`).
- Saltarse `Application` desde un controller.
- `@Html.Raw` con datos de usuario, y `<script>` o `<style>` en línea.
- Rutas convencionales `{controller}/{action}`; usa `[Route]`.
- Un layout público que muestre navegación privada, aplicar el shell autenticado a Account, o decidir el layout por `User-Agent`.
- `[IgnoreAntiforgeryToken]` fuera de la página de error.
- Vistas propias dentro de `Pages/`.
- Cualquier comentario en un `.cshtml`: `@* *@`, `<!-- -->`, `TODO` o HTML comentado.
