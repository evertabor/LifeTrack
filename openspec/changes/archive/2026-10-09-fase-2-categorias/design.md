# Design: fase-2-categorias

## Enfoque Técnico

Implementación del vertical slice de HU-04 (crear y listar categorías) siguiendo Clean Architecture con las capas Domain → Application → Infrastructure → Web. La entidad `Category` vive en el dominio con sus invariantes (RN-F04: nombre obligatorio, máximo 50 caracteres, Trim aplicado antes de guardar). El caso de uso `CreateCategory` usa un patrón de defensa en profundidad: validación en la fábrica del dominio, pre-check de unicidad en el handler, e índice único compuesto en la base de datos como última línea de defensa. `ListCategories` filtra por `ICurrentUser.UserId` para garantizar aislamiento por usuario. El patrón de traducción de excepciones en la frontera Application/Domain se aplica explícitamente.

## Decisiones de Arquitectura

### Decisión: Nombre canónico almacenado tras Trim y validación de longitud

**Elección**: La entidad `Category` aplica `Trim()` en la fábrica `Create(...)` y rechaza nombres vacíos tras el Trim. La validación de longitud (máximo 50 caracteres) se aplica sobre el valor ya recortado.

**Alternativas consideradas**:
- Validar longitud antes de Trim: rechazado porque un nombre como "  " (50 espacios) passaría la validación de longitud pero fallaría en Trim.
- Normalizar en el repository: rechazado porque el dominio debe mantener sus invariantes de forma autónoma.
- Expresiones en el índice EF para Trim automático: rechazado porque la normalización ya ocurre en la fábrica del dominio antes de persistir, y EF Core/SQL Server no garantizan que la expresión se evalúe de forma determinista en todas las versiones.

**Justificación**: Seguir el principio de que el dominio es el primero en defender sus invariantes. Si el Trim se aplicara solo en la base de datos, el índice contendría valores no normalizados hasta el próximo INSERT, creando una ventana de inconsistencia. Al normalizar en la fábrica, toda comparación (incluyendo el pre-check del handler y el índice) trabaja con el valor canónico.

### Decisión: Aislamiento por usuario mediante filtro en el repositorio

**Elección**: `ICategoryRepository` recibe `Guid userId` como parámetro obligatorio en todas sus operaciones de lectura. `ListCategories` filtra por `UserId == userId`. El handler de creación obtiene el `UserId` de `ICurrentUser`.

**Alternativas consideradas**:
- Filtrar en el handler tras obtener todas las categorías: rechazado porque los datos de otro usuario ya estarían en memoria.
- Delegar el filtro a la capa Web: rechazado porque la autorización por propiedad es una responsabilidad de la capa Application según AGENTS.md.

**Justificación**: `ICategoryRepository` es el primer contrato de repositorio de negocio en la capa Application. Aplicar el filtro por `UserId` en el repositorio —y no en el handler ni en la capa Web— garantiza que ningún dato ajeno llega a la capa Application ni a la capa Web, haciendo imposible una brecha de IDOR por construcción. Este patrón será replicable en futuros repositorios del mismo agregado.

### Decisión: Índice único compuesto (UserId, Name) sobre la columna almacenada

**Elección**: La configuración EF `CategoryConfiguration` define un índice único en las columnas `UserId` y `Name` (ambas columnas contienen valores ya normalizados por la fábrica del dominio).

**Alternativas consideradas**:
- Índice único con expresión `Lower(Name)`: no disponible en SQL Server sin expresión indexada, y la normalización ya ocurre en la fábrica.
- Constraint de tabla UNIQUE con trigger: sobreingeniería; el índice es suficiente.
- Verificación en el handler sin índice: rechazado; la base de datos es la defensa final ante condiciones de carrera.

**Justificación**: El índice único compuesto protege la invariante RN-F04 (unicidad por usuario) en la capa de persistencia. La normalización `Trim()` ocurre en la fábrica antes de que la entidad llegue al DbContext, así que la columna `Name` contiene el valor canónico. La probabilidad de una condición de carrera entre el pre-check del handler y el INSERT real es baja en uso personal, pero el índice la cubre.

### Decisión: Traducción explícita de excepciones en la frontera Application/Domain

**Elección**: Cada handler que invoca al dominio envuelve la llamada en `try`/`catch`, captura `DomainException` y `NotFoundException`, y relanza `BusinessRuleException` y `ResourceNotFoundException` respectivamente. El controller solo captura las excepciones de Application.

**Alternativas consideradas**:
- Pipeline de excepciones MediatR con `IRequestExceptionHandler`: no adoptado; AGENTS.md §C lo define como opción futura (ADR-0004).
- Helpers de traducción: no adoptado; la traducción explícita es más legible y no oculta el flujo de control.

**Justificación**: La decisión ya está tomada en el código existente (ver `RegisterAccountHandler`) y en la documentación de AGENTS.md. La traducción explícita mantiene el flujo de error visible y cumple la regla de que ninguna excepción de Domain sale de la capa Application.

### Decisión: Manejo de la violación del índice único mediante DbUpdateException

**Elección**: El handler `CreateCategoryHandler` captura la excepción de dominio (nombre duplicado tras pre-check) y la traduce a `BusinessRuleException`. La posible `DbUpdateException` por violación del índice único queda como última defensa y su traducción se verificará durante la implementación: se confirmará que EF Core/SQL Server produce un `DbUpdateException` con `InnerException` de tipo `SqlException` cuyo número de error corresponde a una violación de restricción única (2627 o 2601).

**Alternativas consideradas**:
- Mapear `DbUpdateException` a `BusinessRuleException` en un try/catch envolvente: viable pero requiere verificar el tipo de `InnerException` para no enmascarar otros errores.
- No atrapar `DbUpdateException`: el índice funciona como defensa última silenciosa; si el pre-check pasó y el INSERT falla por índice, algo está mal y debe producir un error 500.

**Justificación abierta**: El diseño no asume el comportamiento exacto de la traducción de `DbUpdateException` sin verificación. Durante la implementación, se verificará con un test de integración si `DbUpdateException` por violación del índice único se traduce a `BusinessRuleException` con el mensaje apropiado, o si requiere un mapeo adicional. La verificación es un paso de la tarea de implementación, no una decisión de diseño predefinida.

## Flujo de Datos

```
Navegador (POST /categories/create)
    │
    ▼
CategoriesController.Create(InputModel)
    │ Valida DataAnnotations
    │ Captura BusinessRuleException → ModelState
    │ Captura ResourceNotFoundException → LogWarning + TempData genérico
    │
    ▼
sender.Send(CreateCategoryCommand, ct)
    │
    ▼
CreateCategoryHandler.Handle(...)
    │ Obtiene ICurrentUser.UserId
    │ domainException = Category.Create(name, userId)
    │ if (await repository.ExistsAsync(name, userId)) → DomainException
    │ await repository.AddAsync(category)
    │ await unitOfWork.SaveChangesAsync()
    │
    │try { ... }
    │catch (DomainException ex) → throw BusinessRuleException(ex.Message)
    │
    ▼
RedirectToAction(nameof(Index))
    │
    ▼
CategoriesController.Index
    │
    ▼
sender.Send(ListCategoriesQuery)
    │
    ▼
ListCategoriesHandler.Handle(...)
    │ items = await repository.ListAsync(currentUser.UserId)
    │ return IReadOnlyList<CategorySummary>
    │
    ▼
ViewModel.From(categories, message)
    │
    ▼
Index.cshtml → renderiza categorías
```

**Nota sobre el flujo**: el diagrama usa `//` como marcadores de paso dentro del flujo, no como comentarios de código. Estos marcadores describen operaciones discretas en cada capa.

## Cambios en Archivos

| Archivo | Acción | Descripción |
|---------|--------|-------------|
| `src/LifeTrack.Domain/Finance/Category.cs` | Crear | Entidad con fábrica `Create(name, userId)`, invariantes RN-F04, método privado `NormalizeName` con `Trim()` |
| `src/LifeTrack.Application/Abstractions/ICategoryRepository.cs` | Crear | Interfaz del repositorio con `AddAsync`, `ExistsAsync`, `ListAsync` |
| `src/LifeTrack.Application/Finance/Commands/CreateCategory/CreateCategoryCommand.cs` | Crear | Comando `ICommand<CategoryResult>` |
| `src/LifeTrack.Application/Finance/Commands/CreateCategory/CreateCategoryHandler.cs` | Crear | Handler con traducción de excepciones |
| `src/LifeTrack.Application/Finance/Commands/CreateCategory/CategoryResult.cs` | Crear | DTO de resultado para creación |
| `src/LifeTrack.Application/Finance/Commands/ListCategories/ListCategoriesQuery.cs` | Crear | Consulta `IQuery<IReadOnlyList<CategorySummary>>` |
| `src/LifeTrack.Application/Finance/Commands/ListCategories/ListCategoriesHandler.cs` | Crear | Handler de listado filtrado por usuario |
| `src/LifeTrack.Application/Finance/Commands/ListCategories/CategorySummary.cs` | Crear | DTO de resumen para listado |
| `src/LifeTrack.Infrastructure/Persistence/Configurations/CategoryConfiguration.cs` | Crear | `IEntityTypeConfiguration<Category>` con índice único en (`UserId`, `Name`) |
| `src/LifeTrack.Infrastructure/Persistence/Repositories/CategoryRepository.cs` | Crear | Implementación de `ICategoryRepository` |
| `src/LifeTrack.Infrastructure/DependencyInjection.cs` | Modificar | Registrar `ICategoryRepository` como scoped |
| `src/LifeTrack.Web/Controllers/CategoriesController.cs` | Crear | Controller con acciones `Index` (GET), `Create` (GET/POST) |
| `src/LifeTrack.Web/ViewModels/Categories/CategoryListViewModel.cs` | Crear | ViewModel para la vista de lista |
| `src/LifeTrack.Web/ViewModels/Categories/CategoryFormViewModel.cs` | Crear | ViewModel para el formulario de creación |
| `src/LifeTrack.Web/Views/Categories/Index.cshtml` | Crear | Vista principal de lista de categorías |
| `src/LifeTrack.Web/Views/Categories/Partials/_CategoryForm.cshtml` | Crear | Formulario parcial de creación |
| `src/LifeTrack.Web/wwwroot/css/components.css` | Modificar | Añadir clases CSS con prefijo `categories-*` |
| `tests/LifeTrack.Domain.Tests/Finance/CategoryTests.cs` | Crear | Tests de invariantes: nombre obligatorio, max 50, Trim |
| `tests/LifeTrack.Application.Tests/Finance/CreateCategoryHandlerTests.cs` | Crear | Tests del handler: éxito, nombre vacío, max 50, duplicado, IDOR |
| `tests/LifeTrack.Application.Tests/Finance/ListCategoriesHandlerTests.cs` | Crear | Tests del handler: filtrado por usuario |
| `tests/LifeTrack.Application.Tests/Finance/Fakes/FakeCategoryRepository.cs` | Crear | Fake en memoria de ICategoryRepository |
| `tests/LifeTrack.Integration.Tests/CategoriesEndpointTests.cs` | Crear | Tests de integración HTTP: GET /categories, POST /categories/create |
| Migración EF | Crear | `CreateCategory` con índice único en (`UserId`, `Name`) |

## Interfaces y Contratos

### Domain: Category

```csharp
namespace LifeTrack.Domain.Finance;

public sealed class Category : Entity
{
    public const int NameMaxLength = 50;

    private Category(Guid id, Guid userId, string name) : base(id) { ... }

    public Guid UserId { get; }
    public string Name { get; }

    public static Category Create(string name, Guid userId) { ... }

    private static string NormalizeName(string name) => name.Trim();
}
```

### Application: ICategoryRepository

```csharp
namespace LifeTrack.Application.Abstractions;

public interface ICategoryRepository
{
    Task AddAsync(Category category, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string normalizedName, Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Category>> ListAsync(Guid userId, CancellationToken cancellationToken);
}
```

### Application: CreateCategory

**Command definition**
```csharp
public sealed record CreateCategoryCommand(string Name) : ICommand<CategoryResult>;
```

**Result definition**
```csharp
public sealed record CategoryResult(Guid Id, string Name);
```

**Handler implementation**
```csharp
public sealed class CreateCategoryHandler(
    ICategoryRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : IRequestHandler<CreateCategoryCommand, CategoryResult>
{
    public async Task<CategoryResult> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        try
        {
            var category = Category.Create(request.Name, currentUser.UserId);
            if (await repository.ExistsAsync(category.Name, category.UserId, ct))
            {
                throw new DomainException("Ya existe una categoría con ese nombre.");
            }
            await repository.AddAsync(category, ct);
            await unitOfWork.SaveChangesAsync(ct);
            return new CategoryResult(category.Id, category.Name);
        }
        catch (DomainException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }
    }
}
```

### Application: ListCategories

**Query definition**
```csharp
public sealed record ListCategoriesQuery : IQuery<IReadOnlyList<CategorySummary>>;
```

**DTO definition**
```csharp
public sealed record CategorySummary(Guid Id, string Name);
```

**Handler implementation**
```csharp
public sealed class ListCategoriesHandler(
    ICategoryRepository repository,
    ICurrentUser currentUser)
    : IRequestHandler<ListCategoriesQuery, IReadOnlyList<CategorySummary>>
{
    public async Task<IReadOnlyList<CategorySummary>> Handle(ListCategoriesQuery request, CancellationToken ct)
    {
        var items = await repository.ListAsync(currentUser.UserId, ct);
        return items.Select(c => new CategorySummary(c.Id, c.Name)).ToList();
    }
}
```

### Infrastructure: CategoryConfiguration

```csharp
internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Name).HasMaxLength(Category.NameMaxLength).IsRequired();
        builder.HasIndex(c => c.UserId);
        builder.HasIndex(c => new { c.UserId, c.Name }).IsUnique();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
```

### Web: CategoriesController

```csharp
[Route("categories")]
public sealed class CategoriesController(ISender sender, ILogger<CategoriesController> logger) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var categories = await sender.Send(new ListCategoriesQuery(), ct);
        return View(CategoryListViewModel.From(categories, TempData["Message"] as string));
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [FromForm(Prefix = "Input")] CreateInputModel input,
        CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await sender.Send(new CreateCategoryCommand(input.Name), ct);
                return RedirectToAction(nameof(Index));
            }
            catch (BusinessRuleException ex)
            {
                ModelState.AddModelError("Input.Name", ex.Message);
            }
        }
        return await RenderIndexAsync(ct);
    }

    public sealed class CreateInputModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string Name { get; set; } = string.Empty;
    }
}
```

## Estrategia de Pruebas

| Capa | Qué probar | Enfoque |
|------|------------|---------|
| Domain | Invariantes: nombre vacío tras Trim, nombre > 50 chars, Trim aplicado, creación válida | `CategoryTests` con `[Theory]` para bordes (0, 1, 50, 51 caracteres, whitespace) |
| Application | CreateCategoryHandler: éxito, DomainException de nombre vacío, DomainException de max 50, DomainException de duplicado (pre-check), IDOR (nulo UserId) | `CreateCategoryHandlerTests` enviando el comando por `ISender` con `FakeCategoryRepository`, `FakeUnitOfWork`, `FakeCurrentUser` |
| Application | ListCategoriesHandler: vacío para usuario sin categorías, filtrado por usuario (dos usuarios con misma categoría devuelven solo el propio) | `ListCategoriesHandlerTests` con `FakeCategoryRepository` |
| Architecture | Domain no referencia Infrastructure/Web/Application; Application no referencia Infrastructure/Web; Controllers no referencian Domain ni repositorios concretos | `DependencyRulesTests` con NetArchTest.Rules |
| Integration | GET /categories sin auth → 401; GET /categories con auth → 200 y lista; POST /categories/create con nombre válido → redirect; POST /categories/create duplicado → 200 con error en formulario | `CategoriesEndpointTests` con `CustomWebApplicationFactory` y SQLite en memoria |

## Threat Matrix

N/A — esta feature no modifica routing, shell commands, subprocesses, VCS/PR automation, executable-file classification, ni process integration boundary.

## Migración y Despliegue

La migración EF se crea pero no se aplica durante la implementación:

1. **Crear migración**: `dotnet ef migrations add CreateCategory --project src/LifeTrack.Infrastructure --startup-project src/LifeTrack.Web --output-dir Persistence/Migrations`
2. **Verificar la migración**: revisar el archivo generado confirma que el índice único está en (`UserId`, `Name`)
3. **Aplicar en desarrollo**: `dotnet ef database update --project src/LifeTrack.Infrastructure --startup-project src/LifeTrack.Web`
4. **Aplicar en producción**: el pipeline de despliegue ejecuta `dotnet ef database update` como paso posterior al despliegue

**Pasos documentados para trazabilidad** — los números identifican cada operación discrete en el flujo de despliegue.

La tabla `Categories` se crea con:
- Columna `Id` (GUID, primary key, `ValueGeneratedNever()`)
- Columna `UserId` (GUID, FK a `AspNetUsers` con cascade delete)
- Columna `Name` (nvarchar(50), NOT NULL)
- Índice único en (`UserId`, `Name`)

No se requiere migración de datos porque `Category` es una entidad nueva.

## Preguntas Abiertas

- [ ] **DbUpdateException por índice único**: durante la implementación, verificar que `DbUpdateException` por violación del índice único se traduce correctamente a `BusinessRuleException` con el mensaje "Ya existe una categoría con ese nombre.", o si requiere mapeo adicional en el handler. El comportamiento exacto de EF Core/SQL Server debe confirmarse con un test de integración antes de asumir que funciona sin mapeo.

- [ ] **Exposición del mensaje de DomainException del pre-check**: el handler lanza `DomainException("Ya existe una categoría con ese nombre.")` cuando `ExistsAsync` devuelve `true`. Esta excepción es capturada y traducida a `BusinessRuleException`. Dado que el mensaje es idéntico al que la base de datos produciría en caso de violación del índice, el usuario recibe el mismo mensaje en ambos casos de carrera. ¿Es este el comportamiento deseado o debería haber un mensaje diferenciado para el caso de violación del índice?

- [ ] **Ubicación de ICategoryRepository**: decisión verificada — crear `ICategoryRepository.cs` bajo `src/LifeTrack.Application/Abstractions/` siguiendo la convención del proyecto para interfaces de repositorio.

---

# Alcance-Enmienda: fase-2-categorias

Fecha de emisión: 2026-10-09
Propósito: enmendar el diseño existente para cubrir refactorizaciones ortogonales de código遗留 (primary constructors, relocalización de InputModel, mejora de vistas, reglas AGENTS).

---

## 1. Constructores Primarios en C# — Alcance Completo

### Enfoque Técnico

Convertir todos los constructores tradicionales con DI a constructores primarios en las tres capas de código de producción (`Web`, `Application`, `Infrastructure`). La sintaxis de constructor primario de C# 12 permite definir los parámetros de dependencia en la declaración de la clase y eliminar el cuerpo de constructor. Ningún campo puede ser `readonly` si se asigna dentro del cuerpo del constructor — con constructores primarios, los campos asignados por parámetro son implicitly `readonly`.

Exclusiones expresas (ningún constructor primario):
- `AppDbContext` — requiere constructor `DbContextOptions<AppDbContext> options : base(options)` que no puede expresarse como constructor primario sin romper la herencia de `IdentityDbContext`.
- Entidades de dominio (`Category`) — tienen конструктор privado sin parámetros para EF y fábrica estática `Create(...)`; no reciben DI.
- Excepciones (`DomainException`, `NotFoundException`, `BusinessRuleException`, `ResourceNotFoundException`) — no reciben DI.
- Migraciones EF generadas — no se modifican.
- `ErrorController` — no tiene dependencias inyectadas; usa el constructor default.

### Decisiones de Arquitectura

#### Decisión: Constructores primarios para todos los controladores Web DI-construidos

**Elección**: `CategoriesController` y `AccountController` reciben constructores primarios con `ISender sender, ILogger<TController> logger`.

**Alternativas consideradas**:
- Aplicar constructores primarios también a `ErrorController`: rechazado porque no tiene dependencias inyectadas; usar el constructor default mantiene coherencia con la simplicity de la clase.

**Justificación**: los controladores ya siguen el patrón de dependencias mínimas (`ISender` + `ILogger<T>`). El constructor primario elimina el cuerpo de constructor redundante y alinea el código con el ejemplo canónico en `AGENTS.md`.

#### Decisión: `SecurityHeadersMiddleware` y `CurrentUserService` también reciben constructores primarios

**Elección**: `SecurityHeadersMiddleware(RequestDelegate next, IConfiguration configuration)` y `CurrentUserService(IHttpContextAccessor httpContextAccessor)` se convierten a constructores primarios.

**Alternativas consideradas**:
- Dejar `SecurityHeadersMiddleware` con constructor tradicional: rechazado porque es middleware DI-inyectado y el proyecto ya usa constructores primarios consistentemente en el resto de servicios.

**Justificación**: ambos son servicios de infraestructura de presentación con una dependencia clara; el constructor primario reduce ceremony.

#### Decisión: Handlers de MediatR en Application

**Elección**: `CreateCategoryHandler`, `ListCategoriesHandler`, `LoginHandler`, `RegisterAccountHandler`, `LogoutHandler` reciben constructores primarios.

**Justificación**: todos tienen exactamente 1-3 dependencias inyectadas que son necesarias para `Handle`. La sintaxis de constructor primario de C# 12 es compatible con `IRequestHandler<TRequest, TResponse>`.

#### Decisión: Repositorios y servicios de Infrastructure

**Elección**: `CategoryRepository`, `AccountService`, `UnitOfWork` reciben constructores primarios.

**Alternativas consideradas**:
- `AppDbContext` con constructor primario: rechazado porque `IdentityDbContext` requiere que el constructor de `DbContext` se delegue a `base(options)` explícitamente, y la sintaxis de constructor primario no permite 调用 base constructor con lógica adicional en el cuerpo.

**Justificación**: estos tres tipos tienen dependencias straightforward que se resuelven por DI sin lógica adicional en el constructor.

### Archivos Afectados — Constructores Primarios

#### Web Layer

| Archivo | Cambio | Descripción |
|---------|--------|-------------|
| `src/LifeTrack.Web/Controllers/CategoriesController.cs` | Modificar | Convertir `public CategoriesController(ISender sender, ILogger<...> logger) { _sender = sender; _logger = logger; }` a constructor primario `public CategoriesController(ISender sender, ILogger<CategoriesController> logger)` |
| `src/LifeTrack.Web/Controllers/AccountController.cs` | Modificar | Mismo patrón: constructor primario para `ISender` e `ILogger<AccountController>` |
| `src/LifeTrack.Web/Security/SecurityHeadersMiddleware.cs` | Modificar | Convertir a `public SecurityHeadersMiddleware(RequestDelegate next, IConfiguration configuration)` |
| `src/LifeTrack.Web/Services/CurrentUserService.cs` | Modificar | Convertir a `public CurrentUserService(IHttpContextAccessor httpContextAccessor)` |

#### Application Layer

| Archivo | Cambio | Descripción |
|---------|--------|-------------|
| `src/LifeTrack.Application/Finance/Commands/CreateCategory/CreateCategoryHandler.cs` | Modificar | Constructor primario `public CreateCategoryHandler(ICategoryRepository repository, IUnitOfWork unitOfWork, ICurrentUser currentUser)` |
| `src/LifeTrack.Application/Finance/Commands/ListCategories/ListCategoriesHandler.cs` | Modificar | Constructor primario `public ListCategoriesHandler(ICategoryRepository repository, ICurrentUser currentUser)` |
| `src/LifeTrack.Application/Identity/Commands/Login/LoginHandler.cs` | Modificar | Constructor primario `public LoginHandler(IAccountService accountService)` |
| `src/LifeTrack.Application/Identity/Commands/RegisterAccount/RegisterAccountHandler.cs` | Modificar | Constructor primario `public RegisterAccountHandler(IAccountService accountService)` |
| `src/LifeTrack.Application/Identity/Commands/Logout/LogoutHandler.cs` | Modificar | Constructor primario `public LogoutHandler(IAccountService accountService)` |

#### Infrastructure Layer

| Archivo | Cambio | Descripción |
|---------|--------|-------------|
| `src/LifeTrack.Infrastructure/Persistence/Repositories/CategoryRepository.cs` | Modificar | Constructor primario `public CategoryRepository(AppDbContext context)` |
| `src/LifeTrack.Infrastructure/Identity/AccountService.cs` | Modificar | Constructor primario `public AccountService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)` |
| `src/LifeTrack.Infrastructure/Persistence/UnitOfWork.cs` | Modificar | Constructor primario `public UnitOfWork(AppDbContext context)` |

### Archivos NO Modificados por esta Decisión

- `src/LifeTrack.Infrastructure/Persistence/AppDbContext.cs` — excluded (EF/Identity)
- `src/LifeTrack.Infrastructure/Persistence/ApplicationUser.cs` — domain entity pattern
- `src/LifeTrack.Domain/Finance/Category.cs` — domain entity with factory
- `src/LifeTrack.Domain/Common/Exceptions/*.cs` — exception classes, no DI
- `src/LifeTrack.Application/Common/Exceptions/*.cs` — exception classes, no DI
- `src/LifeTrack.Infrastructure/Persistence/Migrations/*.cs` — generated files

---

## 2. Relocalización de CreateInputModel

### Enfoque Técnico

El `CreateInputModel` anidado dentro de `CategoriesController` viola la regla de AGENTS.md que establece: "El `InputModel` valida **presencia y formato** (`[Required]`)". La clase está anidada para mantenerla cerca del controller que la usa, pero el proyecto ya tiene un directorio `ViewModels/Categories/` donde viven todos los modelos de presentación de esa área.

La relocation a `ViewModels/Categories/CreateCategoryInputModel.cs` cumple:
- El InputModel es un modelo de presentación, no un tipo interno del controller.
- Está bajo el mismo namespace feature (`LifeTrack.Web.ViewModels.Categories`).
- El binding `[FromForm] CreateInputModel` en el controller no requiere que el tipo esté anidado.

### Archivo Creado

| Archivo | Acción | Descripción |
|---------|--------|-------------|
| `src/LifeTrack.Web/ViewModels/Categories/CreateCategoryInputModel.cs` | Crear | Input model con `[Required(ErrorMessage = "El nombre es obligatorio.")]` para `Name` |

### Archivos Modificados

| Archivo | Acción | Descripción |
|---------|--------|-------------|
| `src/LifeTrack.Web/Controllers/CategoriesController.cs` | Modificar | Eliminar clase anidada `CreateInputModel`. Añadir `using LifeTrack.Web.ViewModels.Categories;`. Cambiar parámetro de acción de `[FromForm] CreateInputModel input` a `[FromForm] CreateCategoryInputModel input` (el binder busca por nombre de propiedad, no por nombre de tipo). |

### Contrato del Tipo

```csharp
using System.ComponentModel.DataAnnotations;

namespace LifeTrack.Web.ViewModels.Categories;

public sealed record CreateCategoryInputModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Name { get; init; } = string.Empty;
}
```

---

## 3. Html.PartialAsync → Razor `<partial>` Tag Helper

### Enfoque Técnico

`Index.cshtml` usa `@await Html.PartialAsync("Partials/_CategoryForm", ...)` para renderizar el formulario parcial. El tag helper `<partial>` de Razor es la forma canónica recomendada por ASP.NET Core para renderizar parciales y no requiere llamar a un método helper async.

### Archivo Modificado

| Archivo | Acción | Descripción |
|---------|--------|-------------|
| `src/LifeTrack.Web/Views/Categories/Index.cshtml` | Modificar | Cambiar `@await Html.PartialAsync("Partials/_CategoryForm", new ...)` a `<partial name="Partials/_CategoryForm" model="new ..." />` |

Cambio específico en `Index.cshtml` línea 17:
```csharp
// Antes:
@await Html.PartialAsync("Partials/_CategoryForm", new LifeTrack.Web.ViewModels.Categories.CategoryFormViewModel())

// Después:
<partial name="Partials/_CategoryForm" model="new LifeTrack.Web.ViewModels.Categories.CategoryFormViewModel()" />
```

---

## 4. Decisiones de Vista en CategoryListViewModel

### Enfoque Técnico

`Index.cshtml` contiene lógica de presentación:
- Línea 12: `@if (!string.IsNullOrEmpty(Model.SuccessMessage))`
- Línea 19: `@if (Model.Categories.Any())`

Esto viola la regla AGENTS: "Los importes, las fechas y los rótulos llegan **ya formateados** en el `ViewModel`" y "Una vista **solo renderiza**. Cero lógica."

Se agregan propiedades booleanas calculadas al `CategoryListViewModel` para que la vista solo haga `if`/`else` sobre valores ya decididos.

### Archivo Modificado

| Archivo | Acción | Descripción |
|---------|--------|-------------|
| `src/LifeTrack.Web/ViewModels/Categories/CategoryListViewModel.cs` | Modificar | Añadir `HasSuccessMessage`, `HasCategories`, `IsEmpty` como propiedades `bool` derivadas |

### Nuevo Contrato de CategoryListViewModel

```csharp
public sealed record CategoryListViewModel
{
    public IReadOnlyList<CategoryFormViewModel> Categories { get; init; } = [];
    public string? SuccessMessage { get; init; }
    public bool HasSuccessMessage => !string.IsNullOrEmpty(SuccessMessage);
    public bool HasCategories => Categories.Count > 0;
    public bool IsEmpty => Categories.Count == 0;

    public static CategoryListViewModel From(IReadOnlyList<CategorySummary> categories, string? message) =>
        new()
        {
            Categories = categories.Select(CategoryFormViewModel.From).ToList(),
            SuccessMessage = message
        };
}
```

### Vistas Modificadas

| Archivo | Acción | Descripción |
|---------|--------|-------------|
| `src/LifeTrack.Web/Views/Categories/Index.cshtml` | Modificar | Cambiar `!string.IsNullOrEmpty(Model.SuccessMessage)` → `Model.HasSuccessMessage`; cambiar `Model.Categories.Any()` → `Model.HasCategories` |

---

## 5. Reglas de Desarrollo Futuro en AGENTS

### Enfoque Técnico

Documentar como reglas normativas en todos los archivos AGENTS relevantes las decisiones tomadas en esta enmienda, para que orienten el desarrollo futuro y no se reviertan accidentalmente.

### Reglas a Insertar

#### En `LifeTrack/AGENTS.md` ( raíz)

Tras la sección de controllers ( §Controllersdelgados ), añadir:

> **Constructores primarios para DI**
> - Toda clase inyectada por DI en las capas Web, Application o Infrastructure que no tenga restricciones de framework (ver abajo) **debe** usar constructor primario de C# 12.
> - Excepciones expresas que **no** deben recibir constructor primario:
>   - `DbContext` y tipos que heredan de `IdentityDbContext` — requieren `base(options)` explícito que no es compatible con la sintaxis de constructor primario.
>   - Entidades de dominio — tienen fábrica estática `Create(...)` y constructor privado sin parámetros para EF.
>   - Excepciones de dominio y aplicación (`DomainException`, `NotFoundException`, `BusinessRuleException`, `ResourceNotFoundException`) — no reciben DI.
>   - Migraciones EF generadas.
> - Los input models de screen (formularios) viven siempre bajo `ViewModels/<Area>/` y nunca como clases anidadas dentro de un controller.

#### En `LifeTrack/src/LifeTrack.Web/AGENTS.md`

En la sección **Controllers (delgados)**, tras la regla del `InputModel`:

> - Los input models de screen (formularios GET/POST) **nunca** son clases anidadas dentro de un controller. Pertenecen a `ViewModels/<Area>/` bajo el namespace `LifeTrack.Web.ViewModels.<Area>`. Razón: los input models son modelos de presentación, no lógica interna del controller.

#### En `LifeTrack/src/LifeTrack.Application/AGENTS.md`

En la sección **Cómo se escribe un caso de uso**, al hablar del handler:

> - El handler usa constructor primario de C# 12. Ejemplo: `public sealed class CreateCategoryHandler(ICategoryRepository repository, IUnitOfWork unitOfWork, ICurrentUser currentUser) : IRequestHandler<...>`

#### En `LifeTrack/src/LifeTrack.Infrastructure/AGENTS.md`

En la sección **Repositorios**, al hablar del constructor:

> - El repositorio usa constructor primario de C# 12. `DbContext` y `IdentityDbContext` están excluidos por requisito de `base(options)`.

### Archivos Modificados

| Archivo | Acción | Descripción |
|---------|--------|-------------|
| `LifeTrack/AGENTS.md` | Modificar | Añadir regla de constructores primarios y ubicación de input models |
| `LifeTrack/src/LifeTrack.Web/AGENTS.md` | Modificar | Añadir prohibición de input models anidados en controllers |
| `LifeTrack/src/LifeTrack.Application/AGENTS.md` | Modificar | Añadir ejemplo de constructor primario en handler |
| `LifeTrack/src/LifeTrack.Infrastructure/AGENTS.md` | Modificar | Añadir nota de constructor primario en repositorios |

---

## 6. Informe de Verificación — Estatus

El informe de verificación existente en `openspec/changes/fase-2-categorias/verify-report.md` fue generado antes de esta enmienda y constituye evidencia histórica del estado pre-ajuste. **Se requiere una nueva verificación tras la aplicación de las tareas derivadas de esta enmienda** para confirmar que:
- Todos los constructores primarios se aplicaron correctamente y el proyecto compila.
- `CreateCategoryInputModel` se relocalizó y el binding de formulario sigue funcionando.
- `Index.cshtml` usa `<partial>` y las propiedades booleanas del ViewModel.
- Los archivos AGENTS reflejan las nuevas reglas.

---

## Resumen de Archivos por Acción

| Acción | Count | Archivos |
|--------|-------|----------|
| Crear | 1 | `src/LifeTrack.Web/ViewModels/Categories/CreateCategoryInputModel.cs` |
| Modificar | 13 | `CategoriesController.cs`, `AccountController.cs`, `SecurityHeadersMiddleware.cs`, `CurrentUserService.cs`, `CreateCategoryHandler.cs`, `ListCategoriesHandler.cs`, `LoginHandler.cs`, `RegisterAccountHandler.cs`, `LogoutHandler.cs`, `CategoryRepository.cs`, `AccountService.cs`, `UnitOfWork.cs`, `CategoryListViewModel.cs` |
| Modificar vistas | 1 | `Views/Categories/Index.cshtml` |
| Modificar AGENTS | 4 | `LifeTrack/AGENTS.md`, `LifeTrack.Web/AGENTS.md`, `LifeTrack.Application/AGENTS.md`, `LifeTrack.Infrastructure/AGENTS.md` |

**Total: 1 crear, 18 modificar, 0 eliminar.**

---

## Preguntas Abiertas — Enmienda

- Ninguna adicional. Las preguntas abiertas del diseño original siguen vigentes.
