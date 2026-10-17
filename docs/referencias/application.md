# Parte C — Application

## Interfaz de repositorio (recibe siempre userId)

`GetByIdAsync` devuelve la categoría **solo si pertenece al `userId`**; si no,
devuelve `null`, de modo que un id de otro usuario es indistinguible de uno inexistente.
`ListAsync` devuelve únicamente las categorías del usuario.

El parámetro `userId` es **obligatorio en la interfaz**, no opcional: el filtro por usuario va
**dentro de la consulta**, no en el handler. Si el handler filtrara, el recurso ajeno ya estaría en
memoria y la brecha habría ocurrido. Por eso el repositorio real y el *fake* en memoria filtran
ambos (ver **L — Sustitutibilidad** en [SOLID aplicado a LifeTrack](../04-calidad-y-testing.md#solid-aplicado-a-lifetrack)).

`src/LifeTrack.Application/Abstractions/ICategoryRepository.cs`

```csharp
using LifeTrack.Domain.Finance;

namespace LifeTrack.Application.Abstractions;

public interface ICategoryRepository
{
    Task AddAsync(Category category, CancellationToken cancellationToken);

    Task<Category?> GetByIdAsync(Guid categoryId, Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Category>> ListAsync(Guid userId, CancellationToken cancellationToken);
}
```

## TimeProvider.Today()

Devuelve **la fecha actual en la zona horaria configurada para la aplicación**, no la del servidor:
Azure App Service corre en UTC, así que sin esta extensión un usuario de otra zona vería el día
equivocado cerca de medianoche. Al ser una extensión de `TimeProvider`, en los tests se sustituye
por un reloj fijo y el resultado es determinista.

`src/LifeTrack.Application/Common/TimeProviderExtensions.cs`

```csharp
namespace LifeTrack.Application.Common;

public static class TimeProviderExtensions
{
    public static DateOnly Today(this TimeProvider timeProvider) =>
        DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
}
```

## Traducción de excepciones en la frontera (Application)

`Domain` lanza `DomainException` y `NotFoundException` desde `LifeTrack.Domain.Common`. Esas excepciones
son internas del núcleo: **quien está fuera de `Application` no las ve**. Por eso `Application` declara
sus propias excepciones de frontera en `LifeTrack.Application.Common` y **cada handler traduce** las
del dominio a las suyas antes de que el error salga de la capa.

MediatR v13 ofrece `IRequestExceptionHandler<TRequest, TResponse, TException>` para resolver esta
traducción con un *behavior*. **No se adopta:** la traducción por `try`/`catch` explícito sigue siendo
el modelo vigente, porque deja el flujo de error a la vista. Esa vía queda como opción futura
(ADR-0004).

| Excepción | Capa que la declara | Significado | Mensaje |
|---|---|---|---|
| `DomainException` | `Domain` | Regla de negocio violada | En español y **seguro de mostrar** |
| `NotFoundException` | `Domain` | El recurso no existe **o no es del usuario actual** | Genérico; no revela propiedad |
| `BusinessRuleException` | `Application` | Frontera: regla de negocio violada | En español y **seguro de mostrar** |
| `ResourceNotFoundException` | `Application` | Frontera: el recurso no existe **o no es tuyo** | **Genérico** |

La traducción es **explícita**: un `try`/`catch` en el handler, sin *helpers*, sin pipeline de
excepciones y sin decoradores. Así el flujo es legible y el CI no depende de una convención oculta.

### `src/LifeTrack.Application/Common/Exceptions/BusinessRuleException.cs`

Regla de negocio violada. El mensaje es **el que escribió el dominio**, ya en español y seguro de
mostrar, así que `Web` puede ponerlo en `ModelState` o `TempData` sin transformarlo.

```csharp
namespace LifeTrack.Application.Common;

public sealed class BusinessRuleException(string message) : Exception(message);
```

### `src/LifeTrack.Application/Common/Exceptions/ResourceNotFoundException.cs`

El recurso no existe **o no pertenece al usuario actual**, y el mensaje es **siempre genérico**: un
id inexistente y el id de otro usuario tienen que ser indistinguibles para quien llama, o la
respuesta se convierte en un oráculo de existencia.

```csharp
namespace LifeTrack.Application.Common;

public sealed class ResourceNotFoundException : Exception
{
    public const string DefaultMessage = "No se encontró el recurso solicitado.";

    public ResourceNotFoundException()
        : base(DefaultMessage)
    {
    }

    public ResourceNotFoundException(string message)
        : base(message)
    {
    }
}
```

### Clase ancla del ensamblado (la usa MediatR para el escaneo)

`src/LifeTrack.Application/Common/ApplicationAssemblyAnchor.cs`

Los handlers viven en este proyecto, no en `Web`. `AddMediatR` escanea un ensamblado, así que hace
falta un tipo público y estable al que `Web` pueda apuntar sin acoplarse a un caso de uso concreto.
Este es ese tipo, y la razón de existir.

```csharp
namespace LifeTrack.Application.Common;

public sealed class ApplicationAssemblyAnchor
{
}
```

## Contratos de los casos de uso

Los contratos compartidos `ICommand` e `IQuery` viven en `src/LifeTrack.Application/Common/Contracts/`.

MediatR v13 define tres contratos base que se heredan de `IRequest`:

| Contrato | Uso | El handler devuelve |
|---|---|---|
| `ICommand<TResponse>` | Escritura con resultado (crear, actualizar) | `TResponse` |
| `IQuery<TResponse>` | Lectura (listar, resumen) | `TResponse` |
| `IRequest` | Escritura **sin** resultado (eliminar) | `void` |

## Patrón canónico del handler

`src/LifeTrack.Application/Categories/Commands/CreateCategory/`

```csharp
using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using LifeTrack.Domain.Common;
using LifeTrack.Domain.Finance;
using MediatR;

namespace LifeTrack.Application.Categories;

public sealed record CreateCategoryCommand(string Name, TransactionType Type) : ICommand<Guid>;

public sealed class CreateCategoryHandler(
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : IRequestHandler<CreateCategoryCommand, Guid>
{
    public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var category = Category.Create(currentUser.UserId, request.Name, request.Type);

            await categories.AddAsync(category, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return category.Id;
        }
        catch (DomainException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }
        catch (NotFoundException ex)
        {
            throw new ResourceNotFoundException(ex.Message);
        }
    }
}
```

El patrón es idéntico en todos los handlers: `sealed`, constructor primario, `Handle(request,
cancellationToken)` y traducción explícita de las excepciones del dominio. Aquí aparecen las dos
capturas porque es la forma completa; si un caso de uso no puede producir una de ellas —porque no
carga recursos por id, por ejemplo— se omite ese `catch`. Lo que **no** se permite en ningún caso es
dejar salir una excepción de `Domain` sin traducir: `Web` no referencia `Domain` y no podría
capturarla.

## Caso de uso con control de propiedad (anti-IDOR)

`src/LifeTrack.Application/Transactions/Commands/CreateTransaction/`

Este caso de uso produce las dos excepciones del dominio, así que es el ejemplo completo de la
traducción.

El repositorio **filtra por usuario**, de modo que una categoría de otro usuario es indistinguible de una
inexistente: el `??` que lanza `NotFoundException` solo se alcanza si de verdad no hay nada que
devolver.

```csharp
using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using LifeTrack.Domain.Common;
using LifeTrack.Domain.Finance;
using MediatR;

namespace LifeTrack.Application.Transactions;

public sealed record CreateTransactionCommand(
    Guid CategoryId,
    decimal Amount,
    DateOnly Date,
    string? Description = null) : ICommand<Guid>;

public sealed class CreateTransactionHandler(
    ITransactionRepository transactions,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<CreateTransactionCommand, Guid>
{
    public async Task<Guid> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var category = await categories.GetByIdAsync(request.CategoryId, currentUser.UserId, cancellationToken)
                ?? throw new NotFoundException("Category not found.");

            var today = timeProvider.Today();
            var transaction = Transaction.Create(
                currentUser.UserId,
                category.Id,
                category.Type,
                request.Amount,
                request.Date,
                request.Description,
                today);

            await transactions.AddAsync(transaction, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return transaction.Id;
        }
        catch (DomainException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }
        catch (NotFoundException ex)
        {
            throw new ResourceNotFoundException(ex.Message);
        }
    }
}
```

Ojo con esta firma: desde MediatR **v12**, `IRequestHandler<TRequest>` **no** hereda de
`IRequestHandler<TRequest, Unit>` y su método devuelve `Task`, no `Task<Unit>`. Un command de escritura
sin resultado se implementa así, no devolviendo `Unit`.

## Caso de uso con consulta y DTOs

Una consulta no toca el dominio, así que no hay nada que traducir: si el periodo no es válido, el
propio caso de uso lanza `BusinessRuleException`, porque `Application` ya habla el lenguaje de
frontera y **ninguna excepción de `Domain` sale de esta capa**.

`src/LifeTrack.Application/Transactions/ListMonthlyTransactions.cs`

```csharp
using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using LifeTrack.Domain.Finance;
using MediatR;

namespace LifeTrack.Application.Transactions;

public sealed record ListMonthlyTransactionsQuery(int Year, int Month) : IQuery<MonthlySummaryDto>;

public sealed record TransactionDto(
    Guid Id,
    string CategoryName,
    TransactionType Type,
    decimal Amount,
    DateOnly Date,
    string Description);

public sealed record MonthlySummaryDto(
    decimal TotalIncome,
    decimal TotalExpense,
    IReadOnlyList<TransactionDto> Transactions)
{
    public static MonthlySummaryDto Empty { get; } = new(0m, 0m, []);

    public decimal Balance => TotalIncome - TotalExpense;
}

public sealed class ListMonthlyTransactionsHandler(
    ITransactionRepository transactions,
    ICategoryRepository categories,
    ICurrentUser currentUser)
    : IRequestHandler<ListMonthlyTransactionsQuery, MonthlySummaryDto>
{
    public async Task<MonthlySummaryDto> Handle(
        ListMonthlyTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Year is < 2000 or > 2100 || request.Month is < 1 or > 12)
        {
            throw new BusinessRuleException("El periodo solicitado no es válido.");
        }

        var userId = currentUser.UserId;
        var from = new DateOnly(request.Year, request.Month, 1);

        var items = await transactions.ListByPeriodAsync(userId, from, from.AddMonths(1), cancellationToken);
        var names = (await categories.ListAsync(userId, cancellationToken)).ToDictionary(c => c.Id, c => c.Name);

        var dtos = items
            .Select(t => new TransactionDto(
                t.Id,
                names.GetValueOrDefault(t.CategoryId, "Sin categoría"),
                t.Type,
                t.Amount,
                t.Date,
                t.Description))
            .ToList();

        return new MonthlySummaryDto(
            items.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
            items.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount),
            dtos);
    }
}
```

## Caso de uso de lectura sin parámetros

`src/LifeTrack.Application/Categories/Queries/ListCategories/`

Un `Query` sin filtros es un `record` **sin propiedades**: el mensaje se identifica por su tipo. El DTO
lo consume el `ViewModel` de la vista, así que el mapeo ocurre aquí y la vista no calcula nada.

```csharp
using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using LifeTrack.Domain.Finance;
using MediatR;

namespace LifeTrack.Application.Categories;

public sealed record ListCategoriesQuery : IQuery<IReadOnlyList<CategoryDto>>;

public sealed record CategoryDto(
    Guid Id,
    string Name,
    TransactionType Type);

public sealed class ListCategoriesHandler(
    ICategoryRepository categories,
    ICurrentUser currentUser)
    : IRequestHandler<ListCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public async Task<IReadOnlyList<CategoryDto>> Handle(
        ListCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var items = await categories.ListAsync(currentUser.UserId, cancellationToken);

        return items
            .Select(category => new CategoryDto(
                category.Id,
                category.Name,
                category.Type))
            .ToList();
    }
}
```

Fíjate en el contrato: `ListCategoriesQuery` no lleva propiedades, así que el handler recibe **el
contrato**, no un `CancellationToken` suelto. El `CancellationToken` es el segundo parámetro de
`Handle`, como en todos los handlers. El repositorio filtra por usuario, así que las categorías de otro
usuario nunca llegan a esta lista: es el control de acceso por propiedad (OWASP A01) aplicado antes
de que exista cualquier dato en memoria.
