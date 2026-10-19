# Referencia de patrones de pruebas

Este documento muestra los patrones aprobados para tests en LifeTrack. No es un sustituto de la especificación ni de `AGENTS.md`; es un catálogo de **cómo se escribe un test** en cada capa.

## Tests de dominio

Se prueba por **criterio de riesgo**: todo invariante que, si falla, produce un número o estado incorrecto que el usuario ve sin notar (un total que no cuadra, una proyección errónea) es un fallo silencioso y por tanto tiene test.

`tests/LifeTrack.Domain.Tests/CategoryTests.cs`

```csharp
using LifeTrack.Domain.Finance;
using Xunit;

namespace LifeTrack.Domain.Tests;

public class CategoryTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidNameAndType_CreatesCategory()
    {
        var category = Category.Create("Comida", UserId, TransactionType.Expense);

        Assert.Equal("Comida", category.Name);
        Assert.Equal(TransactionType.Expense, category.Type);
        Assert.Equal(UserId, category.UserId);
    }

    [Fact]
    public void Create_WithBlankName_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Category.Create("   ", UserId, TransactionType.Expense));
    }

    [Fact]
    public void EditName_WithValidName_UpdatesName()
    {
        var category = Category.Create("Comida", UserId, TransactionType.Expense);

        category.EditName("Transporte");

        Assert.Equal("Transporte", category.Name);
    }

    [Fact]
    public void EditName_WithBlankName_ThrowsDomainException()
    {
        var category = Category.Create("Comida", UserId, TransactionType.Expense);

        Assert.Throws<DomainException>(() => category.EditName("   "));
    }

    [Fact]
    public void ChangeType_FromExpenseToIncome_UpdatesType()
    {
        var category = Category.Create("Comida", UserId, TransactionType.Expense);

        category.ChangeType(TransactionType.Income);

        Assert.Equal(TransactionType.Income, category.Type);
    }
}
```

`tests/LifeTrack.Domain.Tests/TransactionTests.cs`

```csharp
using LifeTrack.Domain.Finance;
using Xunit;

namespace LifeTrack.Domain.Tests;

public class TransactionTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 10, 2);
    private static readonly Guid CategoryId = Guid.NewGuid();

    private static Category CreateCategory(TransactionType type) =>
        Category.Create(type == TransactionType.Income ? "Sueldo" : "Comida", UserId, type);

    [Fact]
    public void Create_WithValidData_CreatesTransaction()
    {
        var category = CreateCategory(TransactionType.Expense);

        var transaction = Transaction.Create(TransactionType.Expense, category, 50.25m, Today, "Almuerzo");

        Assert.Equal(50.25m, transaction.Amount);
        Assert.Equal(Today, transaction.Date);
        Assert.Equal("Almuerzo", transaction.Description);
    }

    [Fact]
    public void Create_WithZeroAmount_ThrowsDomainException()
    {
        var category = CreateCategory(TransactionType.Expense);

        Assert.Throws<DomainException>(() => Transaction.Create(TransactionType.Expense, category, 0m, Today, null));
    }

    [Fact]
    public void Create_WithNegativeAmount_ThrowsDomainException()
    {
        var category = CreateCategory(TransactionType.Expense);

        Assert.Throws<DomainException>(() => Transaction.Create(TransactionType.Expense, category, -10m, Today, null));
    }

    [Fact]
    public void Create_WithFutureDate_ThrowsDomainException()
    {
        var category = CreateCategory(TransactionType.Expense);
        var tomorrow = Today.AddDays(1);

        Assert.Throws<DomainException>(() => Transaction.Create(TransactionType.Expense, category, 50m, tomorrow, null));
    }

    [Fact]
    public void Create_WithTooManyDecimals_ThrowsDomainException()
    {
        var category = CreateCategory(TransactionType.Expense);

        Assert.Throws<DomainException>(() => Transaction.Create(TransactionType.Expense, category, 50.123m, Today, null));
    }

    [Fact]
    public void Create_IncomeTransactionWithExpenseCategory_ThrowsDomainException()
    {
        var expenseCategory = CreateCategory(TransactionType.Expense);

        Assert.Throws<DomainException>(() => Transaction.Create(TransactionType.Income, expenseCategory, 50m, Today, null));
    }

    [Fact]
    public void Create_ExpenseTransactionWithIncomeCategory_ThrowsDomainException()
    {
        var incomeCategory = CreateCategory(TransactionType.Income);

        Assert.Throws<DomainException>(() => Transaction.Create(TransactionType.Expense, incomeCategory, 50m, Today, null));
    }
}
```

## Fakes para Application

`FixedTimeProvider` fija la zona en **UTC** a propósito: así `Today()` es determinista y el test no depende de la zona horaria de la máquina que lo ejecuta.

`InMemoryCategoryRepository` **filtra por `userId`**, exactamente igual que `CategoryRepository`. No es un detalle: el *fake* es una implementación más de `ICategoryRepository`, así que le aplica LSP. Si devolviera todas las categorías, el test de IDOR **pasaría contra el *fake* y fallaría en producción**, que es exactamente el fallo que el test de IDOR existe para detectar.

`tests/LifeTrack.Application.Tests/Fakes/Fakes.cs`

```csharp
using LifeTrack.Application.Abstractions;
using LifeTrack.Domain.Finance;
using MediatR;

namespace LifeTrack.Application.Tests.Fakes;

internal sealed class FakeCurrentUser(Guid userId) : ICurrentUser
{
    public Guid UserId { get; } = userId;
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.FromResult(1);
    }
}

internal sealed class FixedTimeProvider(DateOnly today) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(today.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

internal sealed class InMemoryCategoryRepository : ICategoryRepository
{
    public List<Category> Items { get; } = [];

    public Task AddAsync(Category category, CancellationToken cancellationToken)
    {
        Items.Add(category);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Category>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Category>>(Items.Where(c => c.UserId == userId).ToList());

    public Task<bool> ExistsAsync(string normalizedName, Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Any(c => c.UserId == userId && c.Name.ToUpper() == normalizedName.ToUpper()));

    public Task<Category?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(Items.FirstOrDefault(c => c.Id == id && c.UserId == userId));

    public void Remove(Category category) => Items.Remove(category);
}

internal sealed class InMemoryTransactionRepository : ITransactionRepository
{
    public List<Transaction> Items { get; } = [];

    public Task AddAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        Items.Add(transaction);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Transaction>> ListByPeriodAsync(
        Guid userId,
        DateOnly from,
        DateOnly toExclusive,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Transaction>>(
            Items.Where(t => t.UserId == userId && t.Date >= from && t.Date < toExclusive).ToList());

    public Task<bool> ExistsForCategoryAsync(Guid categoryId, Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Any(t => t.CategoryId == categoryId && t.UserId == userId));
}

internal sealed class InMemoryBudgetRepository : IBudgetRepository
{
    public List<Budget> Items { get; } = [];

    public Task AddAsync(Budget budget, CancellationToken cancellationToken)
    {
        Items.Add(budget);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Budget>> ListByPeriodAsync(Guid userId, int year, int month, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Budget>>(
            Items.Where(b => b.UserId == userId && b.Year == year && b.Month == month).ToList());

    public Task<bool> ExistsForCategoryAsync(Guid categoryId, Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Any(b => b.CategoryId == categoryId && b.UserId == userId));

    public void Remove(Budget budget) => Items.Remove(budget);
}
```

## Test de un caso de uso enviándolo por `ISender`

**Preferido:** enviar el contrato por `ISender`, igual que un controller. El test cubre entonces el enlace contrato → handler, y un contrato mal declarado o un handler sin registrar falla aquí y no en producción. Los dobles siguen **escritos a mano**: MediatR solo resuelve el contrato → handler, y meter una librería de *mocking* solo aportaría una dependencia que auditar.

`tests/LifeTrack.Application.Tests/Finance/CreateCategoryHandlerTests.cs`

```csharp
using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using LifeTrack.Application.Finance.Commands.CreateCategory;
using LifeTrack.Application.Tests.Fakes;
using LifeTrack.Domain.Finance;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace LifeTrack.Application.Tests.Finance;

public class CreateCategoryHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private readonly InMemoryCategoryRepository _categories = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private ISender SenderFor(Guid userId)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICategoryRepository>(_categories);
        services.AddSingleton<IUnitOfWork>(_unitOfWork);
        services.AddSingleton<ICurrentUser>(new FakeCurrentUser(userId));
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyAnchor).Assembly));

        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }

    [Fact]
    public async Task Handle_ValidNameAndType_CreatesCategoryAndSaves()
    {
        var sender = SenderFor(UserId);

        var result = await sender.Send(new CreateCategoryCommand("Comida", TransactionType.Expense), CancellationToken.None);

        Assert.Equal("Comida", result.Name);
        Assert.Equal(TransactionType.Expense, result.Type);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_DuplicateName_ThrowsBusinessRuleException()
    {
        var sender = SenderFor(UserId);
        _categories.Items.Add(Category.Create("Comida", UserId, TransactionType.Expense));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            sender.Send(new CreateCategoryCommand("Comida", TransactionType.Expense), CancellationToken.None));

        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_EmptyName_ThrowsBusinessRuleException()
    {
        var sender = SenderFor(UserId);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            sender.Send(new CreateCategoryCommand("   ", TransactionType.Expense), CancellationToken.None));
    }
}
```

**Alternativa válida** para un test centrado en un único handler: construirlo directamente y llamar a `Handle(request, cancellationToken)`. Es más rápido porque no monta el dispatcher, pero no cubre el enlace contrato → handler.

```csharp
var handler = new CreateCategoryHandler(_categories, _unitOfWork, new FakeCurrentUser(UserId));

await handler.Handle(new CreateCategoryCommand("Comida", TransactionType.Expense), CancellationToken.None);
```

Un test de Application espera **excepciones de frontera**: el handler ya tradujo las del dominio, así que `ResourceNotFoundException` y `BusinessRuleException` son lo que sale del caso de uso. `DomainException` y `NotFoundException` solo se esperan en los tests de **dominio**.

El test de IDOR no se conforma con comprobar la excepción: además mira `SaveCount == 0` y que no se guardó nada. Un test que solo mira la excepción no verifica que el sistema quedó consistente, y una brecha puede dejar un registro a medias sin que el test se entere.

## Tests de arquitectura (regla de dependencias ejecutable)

Esta clase es la **versión ejecutable de la regla de dependencias de `AGENTS.md`**: si alguien rompe el *layering*, el CI falla aquí. Es la única puerta automática de SOLID, y cubre exactamente la **D** (inversión de dependencias), porque es la única letra cuyo *sentido* se puede verificar automáticamente. SRP, OCP, ISP y LSP no se comprueban aquí: son criterio de revisión.

Los dos tests que leen `Program.cs` comprueban cosas que **no** se pueden expresar con NetArchTest: que el filtro anti-forgery global siga registrado y que el escaneo de MediatR apunte al ensamblado de `Application`. Ambos leen el código fuente porque el registro ocurre en un archivo de compositions, no en un atributo de tipo.

`tests/LifeTrack.Architecture.Tests/DependencyRulesTests.cs`

```csharp
using System.Reflection;
using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using LifeTrack.Domain.Common;
using NetArchTest.Rules;

namespace LifeTrack.Architecture.Tests;

public class DependencyRulesTests
{
    private const string Application = "LifeTrack.Application";
    private const string Infrastructure = "LifeTrack.Infrastructure";
    private const string Web = "LifeTrack.Web";

    private static readonly Assembly DomainAssembly = typeof(Entity).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(ApplicationAssemblyAnchor).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(LifeTrack.Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly WebAssembly = typeof(Program).Assembly;

    private static void AssertSuccessful(TestResult result)
    {
        var offenders = string.Join(", ", result.FailingTypeNames ?? []);
        Assert.True(result.IsSuccessful, $"Rule violated by: {offenders}");
    }

    [Fact]
    public void Domain_DoesNotDependOnAnyOtherLayer()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(Application, Infrastructure, Web)
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Domain_DoesNotDependOnFrameworks()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "System.Data")
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Application_DoesNotDependOnInfrastructureOrWeb()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(Infrastructure, Web, "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnWeb()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(Web)
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Controllers_TalkToApplication_NotToInfrastructureOrDomain()
    {
        var result = Types.InAssembly(WebAssembly)
            .That()
            .HaveNameEndingWith("Controller")
            .ShouldNot()
            .HaveDependencyOnAny(Infrastructure, Domain)
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Controllers_DoNotInjectConcreteHandlers()
    {
        var result = Types.InAssembly(WebAssembly)
            .That()
            .HaveNameEndingWith("Controller")
            .ShouldNot()
            .HaveDependencyOn("LifeTrack.Application.Finance.Commands.CreateCategory.CreateCategoryHandler")
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void ViewModels_DoNotInheritFromDomainEntities()
    {
        var result = Types.InAssembly(WebAssembly)
            .That()
            .HaveNameEndingWith("ViewModel")
            .ShouldNot()
            .InheritFrom(Types.InAssembly(DomainAssembly))
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Antiforgery_GlobalFilter_IsRegistered()
    {
        var programSource = File.ReadAllText(Path.Combine(WebProjectPath, "Program.cs"));

        Assert.Contains("AutoValidateAntiforgeryToken", programSource, StringComparison.Ordinal);
    }

    [Fact]
    public void MediatR_IsRegisteredScanningTheApplicationAssembly()
    {
        var programSource = File.ReadAllText(Path.Combine(WebProjectPath, "Program.cs"));

        Assert.Contains("AddMediatR", programSource, StringComparison.Ordinal);
        Assert.Contains("typeof(ApplicationAssemblyAnchor).Assembly", programSource, StringComparison.Ordinal);
        Assert.DoesNotContain("typeof(Program).Assembly", programSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Handlers_AreSealed()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Handler")
            .Should()
            .BeSealed()
            .GetResult();

        AssertSuccessful(result);
    }
}
```