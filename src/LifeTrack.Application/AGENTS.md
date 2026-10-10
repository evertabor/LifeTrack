# AGENTS.md — LifeTrack.Application

Reglas generales en `/AGENTS.md`. Esta capa orquesta **casos de uso**.

## Dependencias
- `LifeTrack.Domain` y **un único paquete NuGet: `MediatR`** (`13.*`, pineado en `Directory.Packages.props`, ADR-0004). Ni EF Core, ni ASP.NET, ni logging.
- `MediatR` arrastra `Microsoft.Extensions.DependencyInjection.Abstractions` como dependencia transitiva: el núcleo ya **no** compila sin acceso a internet. Si alguna vez eso vuelve a ser un requisito, hay que revisar ADR-0004.
- Los handlers se registran en `Web` con `AddMediatR`, que **escanea este ensamblado** (no el de `Program`) buscando implementaciones de `IRequestHandler<,>`, `IRequestHandler<>`, `INotificationHandler<>`, `IStreamRequestHandler<>` e `IRequestExceptionHandler<,,>`. Por eso esta capa publica la clase ancla `Common/ApplicationAssemblyAnchor.cs`: es el punto que `Web` apunta para el escaneo.
- Los *behaviors* (`IPipelineBehavior`) y los pre/post processors **no** se registran por escaneo: requieren un `Add…` explícito en `Program.cs`. Hoy no hay ninguno.

## Cómo se escribe un caso de uso
- Carpeta por feature o agregado (`Identity/`, `Habits/`, `Transactions/`, `Categories/`). Cada comando o caso de uso tiene su propia carpeta bajo `Commands/`; el comando y su handler correspondiente permanecen juntos en archivos separados dentro de ella. Cada tipo público vive en su propio archivo.
- Contrato: `public sealed record <Verbo><Sustantivo>Command(...) : ICommand<TResponse>` para escrituras con resultado, `IQuery<TResponse>` para lecturas, o `IRequest` para escrituras **sin** resultado.
- Handler: `public sealed class <Verbo><Sustantivo>Handler(...) : IRequestHandler<TRequest, TResponse>` con constructor primario y `Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)`. Sin resultado: `IRequestHandler<TRequest>` y `Task Handle(...)` — desde MediatR v12 `IRequestHandler<TRequest>` **no** hereda de `IRequestHandler<TRequest, Unit>`, así que no se devuelve `Unit`.
- El handler usa constructor primario de C# 12. Ejemplo: `public sealed class CreateCategoryHandler(ICategoryRepository repository, IUnitOfWork unitOfWork, ICurrentUser currentUser) : IRequestHandler<...>`
- `Web` envía esos contratos por `ISender`; nunca se inyecta un handler concreto en un controller.
- Flujo típico: obtener usuario (`ICurrentUser`) → cargar datos **filtrando por usuario** → invocar al dominio → `IUnitOfWork.SaveChangesAsync` → devolver DTO o id.
- **Autorización por propiedad:** los repositorios reciben siempre `userId`. Si el recurso no existe o es de otro usuario, el handler responde `ResourceNotFoundException` (misma respuesta en ambos casos, para que no se pueda deducir la existencia de un id ajeno).
- Fecha actual: `timeProvider.Today()` (extensión en `Common`). Nunca `DateTime.Now`.
- Los DTOs no exponen entidades de dominio. Los repositorios no devuelven `IQueryable`.
- Las interfaces (`I…Repository`, `IUnitOfWork`, `ICurrentUser`) viven en `Abstractions/` y las implementa `Infrastructure` (y `Web` para `ICurrentUser`).
- No registres logs aquí (no hay abstracción de logging): los eventos de seguridad se registran en `Web`.
- La clave de licencia de MediatR se lee en `Web` desde `MediatR:LicenseKey`. Esta capa **nunca** la lee ni la declara.
- **Cero comentarios** en el código de esta capa. Si una restricción no se puede expresar en el código, se documenta en `docs/`.

## Contenido previsto de `Common/`

| Fichero | Papel |
|---|---|
| `Exceptions/BusinessRuleException.cs` | Frontera: regla de negocio violada |
| `Exceptions/ResourceNotFoundException.cs` | Frontera: el recurso no existe o no es del usuario actual |
| `Contracts/ICommand.cs` | Contrato compartido para escrituras con resultado |
| `Contracts/IQuery.cs` | Contrato compartido para lecturas |
| `ApplicationAssemblyAnchor.cs` | Tipo marcador público que `Web` apunta para escanear este ensamblado |
| `TimeProviderExtensions.cs` | `Today()` a partir del `TimeProvider` inyectado |

## Excepciones de frontera y traducción

Esta capa declara las excepciones que el resto de la aplicación puede capturar:

| Excepción | Significado | Mensaje |
|---|---|---|
| `BusinessRuleException` | Regla de negocio violada | En español y **seguro de mostrar** |
| `ResourceNotFoundException` | El recurso no existe **o no pertenece al usuario actual** | **Genérico** |

Viven en `LifeTrack.Application.Common.Exceptions`, un tipo público por archivo. `DomainException` y
`NotFoundException` viven en `LifeTrack.Domain.Common.Exceptions` y siguen siendo las que lanza `Domain`;
son internas del núcleo. Los contratos compartidos `ICommand` e `IQuery` viven en
`LifeTrack.Application.Common.Contracts`.

**Cada handler traduce.** Envuelve su lógica en un `try`/`catch`, captura las excepciones del dominio
y relanza su excepción de frontera con el mensaje. El cuerpo del `try` es la lógica del caso de uso:
cargar filtrando por usuario, aplicar los invariantes y guardar.

```csharp
try
{
}
catch (DomainException ex)
{
    throw new BusinessRuleException(ex.Message);
}
catch (NotFoundException ex)
{
    throw new ResourceNotFoundException(ex.Message);
}
```

- Traducción **explícita**: sin *helpers*, sin pipeline de excepciones, sin decoradores. Sigue vigente aunque MediatR ofrezca `IRequestExceptionHandler<,,>`, que queda como opción futura (ADR-0004).
- Solo se captura lo que el caso de uso puede lanzar de verdad. Si un caso de uso no carga recursos por id, no necesita `catch (NotFoundException)`.
- **Nunca** dejar salir una excepción de `Domain` de un handler: `Web` no referencia `Domain` y no podría capturarla.
- Patrón y ejemplos de esta capa en `docs/referencias/application.md`.

## Pruebas
`tests/LifeTrack.Application.Tests` con *fakes* en memoria (`Fakes/`). Se prueba por **criterio de
riesgo** (sección 10 de `AGENTS.md`), no por rama: camino feliz cuando un valor mal calculado sería
un fallo silencioso, cada excepción **de frontera** que el caso de uso puede producir de verdad y, si
recibe un id, **test de IDOR** (otro usuario ⇒ `ResourceNotFoundException` **y nada se guarda**).

Un caso de uso se prueba preferiblemente **enviando su contrato por `ISender`** con un contenedor
real: es lo que hace un controller, así que el test cubre también el enlace contrato → handler.
Construir el handler a mano sigue siendo válido para tests centrados en un único handler.
