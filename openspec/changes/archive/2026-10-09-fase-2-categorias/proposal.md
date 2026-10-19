# Proposal: fase-2-categorias

## Intent

Implementar el primer vertical slice de Finance en LifeTrack: HU-04 (crear y listar categorías) con RN-F04 (nombre obligatorio, máximo 50 caracteres, unicidad por usuario con comparación trimmed). Este slice introduce la entidad `Category` y establece el patrón que seguirán las demás entidades de Finance. Rename y delete quedan fuera por especificaciones insuficientes; se retomarán cuando existan criterios de aceptación definidos.

## Scope

### In Scope
- Entidad `Category` en Domain con invariantes RN-F04
- Caso de uso `CreateCategory` con validación de dominio + pre-check de unicidad + defensa por índice EF único
- Caso de uso `ListCategories` con filtrado por usuario actual
- Repositorio `ICategoryRepository` y su implementación
- Migración EF inicial para `Category` con índice único en (`UserId`, `Name`) sobre los valores almacenados ya normalizados (nombre recortado con `Trim()` en la fábrica)
- Controller `CategoriesController` con acciones GET/POST para crear y GET para listar
- Vistas MVC: `Index.cshtml` (lista) y `_CategoryForm.cshtml` (formulario parcial)
- Tests unitarios de dominio y aplicación para `CreateCategoryHandler` y `ListCategoriesHandler`
- Tests de IDOR para verificar aislamiento por usuario

### Out of Scope
- Renombrar categoría (RN-F04 no define criterios de aceptación para esta operación)
- Eliminar categoría (RF-F01 indica que no se elimina si tiene movimientos, pero falta el flujo completo)
- Iconos o colores asignados a categorías (DESIGN.md sección 8.6 indica no presuponer estos datos)
- Cualquier pantalla de transacciones, presupuestos o resumen mensual

## Capabilities

### New Capabilities
- `category-management`: Crear categorías con nombre único por usuario y listar las categorías propias del usuario autenticado. El nombre se normaliza con `Trim()` antes de la comparación de unicidad. El mensaje de duplicado es "Ya existe una categoría con ese nombre." Esta capability no incluye rename ni delete.

### Modified Capabilities
Ninguna. `Category` es una entidad nueva; no modifica capabilities existentes de Identity ni de otras áreas.

## Approach

**Enfoque A — Dominio simplificado** (recomendado por la exploración).

1. La entidad `Category` tiene un método estático `Create(name, userId)` que valida las invariantes (nombre obligatorio tras Trim, máximo 50 caracteres) y lanza `DomainException` si no se cumplen. La normalización `Trim()` se aplica en la fábrica para que toda comparación posterior trabaje con el valor ya normalizado.

2. El handler `CreateCategoryHandler` recibe el comando, llama a `Category.Create(...)` en el dominio, consulta `ICategoryRepository.ExistsAsync(name, userId)` con el nombre ya normalizado, y si existe lanza `DomainException("Ya existe una categoría con ese nombre.")`. El handler captura la excepción de dominio y la traduce a `BusinessRuleException(ex.Message)`.

3. El handler `ListCategoriesHandler` filtra por `ICurrentUser.UserId` y devuelve `IReadOnlyList<CategorySummary>`.

4. La configuración EF `CategoryConfiguration` define un índice único en (`UserId`, `Name`) sobre la columna `Name` que almacena el valor ya recortado por `NormalizeName()`. La normalización se aplica en la fábrica del dominio antes de persistir, de modo que el índice protege valores canónicos sin recurrir a una expresión en el índice. El mapeo de `DbUpdateException` por violación del índice único a `BusinessRuleException` queda pendiente de verificar en la fase de diseño o especificación; no se inventan detalles de implementación.

5. El Controller `CategoriesController` sigue el patrón de Identity: inyecta `ISender` y `ILogger<T>`, usa Post/Redirect/Get, captura `BusinessRuleException` para mostrar el error y `ResourceNotFoundException` para casos de IDOR.

6. Las vistas siguen `DESIGN.md` y `docs/09-estandar-vistas-mvc.md`: token CSS `--app-*`, tarjetas `app-card`, clases `categories-*`, ningún icono ni color asignado a la categoría.

## Affected Areas

| Area | Impact | Description |
|---|---|---|
| `src/LifeTrack.Domain/Finance/Category.cs` | New | Entidad con fábrica `Create(name, userId)`, invariantes RN-F04, `NormalizeName` privado con `Trim()` |
| `src/LifeTrack.Application/Abstractions/ICategoryRepository.cs` | New | Interfaz del repositorio de categorías |
| `src/LifeTrack.Application/Finance/Commands/CreateCategory/` | New | Comando `CreateCategoryCommand : ICommand<CategoryResult>` y handler `CreateCategoryHandler` |
| `src/LifeTrack.Application/Finance/Commands/ListCategories/` | New | Consulta `ListCategoriesQuery : IQuery<IReadOnlyList<CategorySummary>>` y handler `ListCategoriesHandler` |
| `src/LifeTrack.Application/Finance/DTOs/CategoryResult.cs` | New | DTO de resultado para creación |
| `src/LifeTrack.Application/Finance/DTOs/CategorySummary.cs` | New | DTO de resumen para listado |
| `src/LifeTrack.Infrastructure/Persistence/Configurations/CategoryConfiguration.cs` | New | `IEntityTypeConfiguration<Category>` con índice único en (`UserId`, `Name`) |
| `src/LifeTrack.Infrastructure/Persistence/Repositories/CategoryRepository.cs` | New | Implementación de `ICategoryRepository` |
| `src/LifeTrack.Infrastructure/DependencyInjection.cs` | Modified | Registro del repositorio `ICategoryRepository` |
| `src/LifeTrack.Web/Controllers/CategoriesController.cs` | New | Controller con acciones `Index` (GET), `Create` (GET/POST) |
| `src/LifeTrack.Web/ViewModels/Categories/CategoryListViewModel.cs` | New | ViewModel para la vista de lista |
| `src/LifeTrack.Web/ViewModels/Categories/CategoryFormViewModel.cs` | New | ViewModel para el formulario de creación |
| `src/LifeTrack.Web/Views/Categories/Index.cshtml` | New | Vista de lista de categorías |
| `src/LifeTrack.Web/Views/Categories/Partials/_CategoryForm.cshtml` | New | Formulario parcial de creación |
| `src/LifeTrack.Web/wwwroot/css/components.css` | Modified | Clases CSS con prefijo `categories-*` |
| `tests/LifeTrack.Domain.Tests/Finance/CategoryTests.cs` | New | Tests de invariantes: nombre obligatorio, max 50, unicidad |
| `tests/LifeTrack.Application.Tests/Finance/CreateCategoryHandlerTests.cs` | New | Tests del handler: éxito, nombre vacío, max 50, duplicado con trim, IDOR |
| `tests/LifeTrack.Application.Tests/Finance/ListCategoriesHandlerTests.cs` | New | Tests del handler: filtrado por usuario |
| Migración EF | New | Migración inicial de `Category` con índice único |

## Risks

| Risk | Likelihood | Mitigation |
|---|---|---|
| Concurrencia: dos requests simultáneas con el mismo nombre pasan la verificación de pre-check | Low | El índice único de la base de datos funciona como último filtro. El mapeo de `DbUpdateException` por violación del índice único a `BusinessRuleException` con mensaje de RN-F04 debe verificarse en diseño/especificación (pendiente de confirmar que EF Core/SQL Server soportan el caso sin expresión en el índice). |
| Espacios en blanco: la comparación no aplica `Trim()` de forma consistente | Low | `NormalizeName` privado en la entidad y en el repository; el test de duplicado con espacios comprueba explícitamente este caso |
| Entidad nueva sin patrón previo en Domain: la primera entidad de negocio puede desviarnos de la arquitectura | Medium | Seguir ADR de arquitectura existente y el patrón de Identity; revisar con `NetArchTest.Rules` tras el primer commit |
| Inseguridad en la consulta de unicidad: `ExistsAsync` filtra por `UserId` y `Name` normalizado, pero el índice no cubre condiciones de carrera | Low | El índice único es la defensa final; la probabilidad de carrera es baja en uso personal |

## Rollback Plan

Si la migración ya fue aplicada a la base de datos y debe revertirse:

1. Ejecutar `dotnet ef migrations remove --project src/LifeTrack.Infrastructure --startup-project src/LifeTrack.Web` para eliminar la migración si no hay cambios aplicados.
2. Si la migración ya fue aplicada en producción, crear una migración de reversión que elimine el índice único y la tabla `Categories` (en orden inverso a la creación).
3. Revertir los archivos de código a la versión anterior al commit de la feature branch.
4. Descartar los tests de la feature branch.

Si el rollback es por error en producción antes de que haya datos:

1. Eliminar la migración en la base de datos: `dotnet ef database update PreviousMigrationName --project src/LifeTrack.Infrastructure --startup-project src/LifeTrack.Web`.
2. Eliminar los archivos de código añadidos (Category.cs, handlers, controller, vistas, tests).
3. Ejecutar `dotnet build` y `dotnet test` para verificar que la base compilable y los tests pasan.

## Dependencies

- `ICurrentUser` ya existe en `Application/Abstractions/ICurrentUser.cs` y está registrado como scoped en DI (definido en la fase 1).
- `AppDbContext` ya existe y hereda de `IdentityDbContext<ApplicationUser>` (fase 1).
- `BusinessRuleException` y `ResourceNotFoundException` ya existen en `Application/Common/Exceptions/`.
- `DomainException` y `NotFoundException` ya existen en `Domain/Common/Exceptions/`.
- MediatR 13.* está pineado en `Directory.Packages.props` y disponible.
- La fase 1 (Identity, cuenta) debe estar completa antes de comenzar la implementación de esta fase.

## Success Criteria

- [ ] La entidad `Category` reside en `Domain/Finance/Category.cs` sin referencias a EF, ASP.NET ni paquetes externos.
- [ ] `Category.Create("  Comida  ", userId)` crea una categoría con nombre "Comida" (Trim aplicado).
- [ ] `Category.Create("", userId)` lanza `DomainException`.
- [ ] `Category.Create(new string('a', 51), userId)` lanza `DomainException`.
- [ ] `CreateCategoryHandler` lanza `BusinessRuleException("Ya existe una categoría con ese nombre.")` cuando ya existe una categoría con ese nombre (comparación tras Trim).
- [ ] `ListCategoriesHandler` devuelve únicamente las categorías del `ICurrentUser.UserId` actual.
- [ ] El índice único en (`UserId`, `Name`) existe en la migración y rechaza duplicados a nivel de base de datos.
- [ ] `CategoriesController` usa `ISender` (no handlers concretos), sigue Post/Redirect/Get y captura solo `BusinessRuleException` y `ResourceNotFoundException`.
- [ ] Las vistas usan tokens CSS `--app-*`, clases `app-card` y `categories-*`, sin lógica ni `Html.Raw`.
- [ ] Los tests de arquitectura pasan sin modificación de reglas.
- [ ] `dotnet build -c Release`, `dotnet test` y `dotnet format --verify-no-changes` pasan sin warnings.
