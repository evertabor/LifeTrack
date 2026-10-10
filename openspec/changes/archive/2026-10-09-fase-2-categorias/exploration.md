# Exploration: fase-2-categorias

## Resumen ejecutivo

Exploracion vertical slice de la fase 2 de LifeTrack: primer slice de Finance para crear y listar categorias aisladas por usuario actual. El alcance se limita a HU-04 (crear/listar categorias) con RN-F04 (nombre obligatorio, max 50 caracteres, unicidad por usuario, comparacion trimmed, mensaje de duplicado). Rename y delete quedan fuera por especificaciones insuficientes.

## Estado actual

El proyecto LifeTrack esta en fase 1 completada (cuenta + Identity). La capa Domain no contiene todavia entidades de Finance (Category, Transaction, Budget). La estructura existente es:

- **Domain**: solo `Entity`, `DomainException`, `NotFoundException` en Common
- **Application**: contratos `ICommand`/`IQuery`, excepciones de frontera `BusinessRuleException`/`ResourceNotFoundException`, interfaz `ICurrentUser`, comandos de Identity (Register, Login, Logout)
- **Infrastructure**: `AppDbContext` (IdentityDbContext), `DependencyInjection`, `AccountService`, `ApplicationUser`, configuraciones EF para Identity, migracion inicial
- **Web**: `AccountController`, `ErrorController`, `CurrentUserService`, `RegisterViewModel`, `LoginViewModel`, middleware de cabeceras de seguridad
- **Tests**: tests de arquitectura (DependencyRulesTests), tests de Application (handlers de Identity con fakes), tests de integracion (Health, Error, Antiforgery)

No existen patrones de Category ni de repository en las capas de dominio. El diseño de Category debe seguir el mismo flujo vertical que Identity: entidad de dominio con invariantes, caso de uso en Application, repositorio en Infrastructure, controller y vistas en Web.

## Areas afectadas

### Domain (`src/LifeTrack.Domain/`)
- `Finance/Category.cs` — entidad nueva con invariantes RN-F04 (nombre obligatorio, max 50 caracteres, unicidad por usuario)
- `Common/Guard.cs` —si existe, o nuevo archivo con metodos de guard

### Application (`src/LifeTrack.Application/`)
- `Abstractions/ICategoryRepository.cs` — interfaz de repositorio
- `Finance/Commands/CreateCategory/` — comando `CreateCategoryCommand : ICommand<CategoryResult>` y handler `CreateCategoryHandler`
- `Finance/Commands/ListCategories/` — comando `ListCategoriesQuery : IQuery<IReadOnlyList<CategorySummary>>` y handler `ListCategoriesHandler`
- `Finance/DTOs/CategoryResult.cs`, `CategorySummary.cs` — DTOs de salida

### Infrastructure (`src/LifeTrack.Infrastructure/`)
- `Persistence/Configurations/CategoryConfiguration.cs` — `IEntityTypeConfiguration<Category>` con indice unico por (UserId, Name trimmed)
- `Persistence/Repositories/CategoryRepository.cs` — implementacion de `ICategoryRepository`
- `DependencyInjection.cs` — registro del repositorio

### Web (`src/LifeTrack.Web/`)
- `Controllers/CategoriesController.cs` — acciones Create (GET/POST) e Index
- `ViewModels/Categories/CategoryListViewModel.cs`, `CategoryFormViewModel.cs`
- `Views/Categories/Index.cshtml`, `Partials/_CategoryForm.cshtml`
- `wwwroot/css/components.css` o `site.css` — clases CSS con prefijo `categories-*`

### Tests (`tests/LifeTrack.Application.Tests/`)
- `Finance/CreateCategoryHandlerTests.cs` — tests de exito, nombre vacio, max 50, duplicado con trimmed, IDOR
- `Finance/ListCategoriesHandlerTests.cs` — tests de filtrado por usuario

### Migrations
- Migracion inicial de Category con indice unico

## Requisitos funcionales verificados

| Requisito | Detalle |
|---|---|
| HU-04 | Crear categoria con nombre unico por usuario; duplicado con trimmed whitespace muestra "Ya existe una categoria con ese nombre" |
| RN-F04 | Nombre obligatorio, max 50 caracteres, unico por (UserId, Name) con comparacion Trim() |
| RF-F01 | Crear y listar categorias (renombrar y eliminar fuera de alcance) |
| RNF-01 / A01 | Toda consulta filtra por ICurrentUser.UserId; IDOR imposible |
| DISENO | Vistas siguen `DESIGN.md`: tokens CSS `--app-*`, tarjetas `app-card`, clases `categories-*` |

## Enfoques comparados

### Enfoque A — Dominio simplicado (recomendado)
La entidad `Category` tiene un metodo estatico `Create(name, userId)` que valida las invariantes (nombre obligatorio, max 50) y lanza `DomainException` si no se cumplen. La unicidad se valida en el handler antes de guardar.

- **Pros**: validacion centralizada, testeable, coincide con el patron de Identity
- **Cons**: requiere consultar antes de insertar para validar unicidad
- **Esfuerzo**: bajo

### Enfoque B — Indice EF con excepcion
La entidad `Category` tiene un indice unico en EF (UserId, Name) con `IsUnicode(false)` y `HasAnnotation("Collation", "NOCASE")` para que la base de datos rechace duplicados. El handler captura `DbUpdateException` y la traduce a `BusinessRuleException` con el mensaje de RN-F04.

- **Pros**: validacion en la capa de datos, no necesita consulta previa
- **Cons**: mapeo de excepcion especifico de EF, fuga de Infrastructure hacia Application
- **Esfuerzo**: medio

### Enfoque C — Doble validacion
Combina A y B: el dominio valida formatos y el handler valida unicidad explicitamente antes de guardar.

- **Pros**: maxima seguridad, mensajes precisos
- **Cons**: doble esfuerzo, potencialmente duplicacion de logica
- **Esfuerzo**: alto

## Recomendacion

**Enfoque A** — Dominio simplicado. Las razones son:

1. Coincide con el patron de Identity existente: el handler consulta antes de actuar
2. La excepcion de dominio `DomainException` con mensaje en espanol es el mecanismo canonico del proyecto
3. El indice unico en EF funciona como defensa adicional, pero la logica de negocio esta en el dominio
4. `ICurrentUser.UserId` esta disponible en Application para filtrar consultas

El flujo seria:
1. El handler recibe `CreateCategoryCommand(name, userId)`
2. Llama a `Category.Create(name, userId)` en el dominio — lanza `DomainException` si nombre invalido
3. Consulta `ICategoryRepository.ExistsAsync(name, userId)` (con Trim)
4. Si existe, lanza `DomainException("Ya existe una categoria con ese nombre")`
5. El handler captura `DomainException` y relanza `BusinessRuleException(ex.Message)`

## Riesgos identificados

1. **Riesgo de concurrencia**: dos requests simultaneas con el mismo nombre podrian pasar la verificacion de unicidad. Mitigacion: el indice unico de EF funciona como ultimo filtro. Probabilidad: baja. Impacto: medio.
2. **Riesgo de espacos en blanco**: `Trim()` en la comparacion requiere que tanto la creacion como la consulta apliquen el mismo Trim. Se resuelve con un metodo privado `NormalizeName` en la entidad.
3. **Riesgo de alcance**: rename/delete no tienen criterios de aceptacion que deben especificarse antes de incluirlos en la fase 2
4. **Riesgo de UI**: DISENO.md y docs/09-estandar-vistas-mvc.md definen patrones visuales y de accesibilidad que deben respetarse. El equipo de diseño aun no ha definido el mecanismo de barras de progreso sin `style=""`.
5. **Riesgo de primera entidad de dominio de negocio**: la entidad Category aun no existe fisicamente en el proyecto. El primer commit de este slice creara la entidad desde cero siguiendo el ADR de arquitectura del proyecto.

## Listo para propuesta

**Si** — el alcance es claro para HU-04, RN-F04, RF-F01, y los requisitos de la interfaz grafica definidos en DISENO.md. La documentacion de DISENO.md sobre categories (seccion 8.6) define la representacion visual pero no requiere iconos ni colores.

**No** — se necesita clarificacion antes de propuesta si:
- Rename/delete de categorias tienen criterios de aceptacion que deben especificarse antes de incluirlos en la fase 2
- La entidad Category debe tener alguna propiedad adicional (icono, color) mas alla del nombre — la especificacion dice que no se presupone, pero podria ser necesaria
