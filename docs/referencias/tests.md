# Parte F — Tests  ⚠️ sin ejecutar con xUnit

Los tests de este documento son **casos de riesgo real**, no cobertura: cada uno cubre un fallo que
sería silencioso, una brecha de seguridad o un invariante cuya violación rompería el agregado. No
son un ejemplo de "un test por rama", y no se escribe un test solo para tocar una línea.

| Test | Categoría de riesgo |
|---|---|
| Rachas (`CurrentStreak`, `BestStreak`) | Fallo silencioso: un número mal calculado se muestra y nadie lo nota |
| Fecha futura y hábito archivado | Invariante del agregado: si no se bloquea, se corrompe el histórico |
| Check-in duplicado | Invariante con índice único en base de datos: la entidad debe rechazarlo antes |
| IDOR (`SaveCount == 0`) | Brecha de seguridad: además de la excepción, comprueba que no se persistió nada |

## Tests de dominio (rachas)

`tests/LifeTrack.Domain.Tests/HabitTests.cs`

```csharp
using LifeTrack.Domain.Common;
using LifeTrack.Domain.Habits;

namespace LifeTrack.Domain.Tests;

public class HabitTests
{
    private static readonly DateOnly Today = new(2026, 10, 2);
    private static readonly Guid UserId = Guid.NewGuid();

    private static Habit NewHabit() => Habit.Create(UserId, "Leer 20 minutos");

    private static Habit HabitWithCheckIns(params int[] daysFromToday)
    {
        var habit = NewHabit();
        foreach (var offset in daysFromToday)
        {
            habit.CheckIn(Today.AddDays(offset), Today);
        }

        return habit;
    }

    [Fact]
    public void CheckIn_AddsCheckIn()
    {
        var habit = NewHabit();

        habit.CheckIn(Today, Today);

        Assert.Single(habit.CheckIns);
    }

    [Fact]
    public void CheckIn_InTheFuture_Throws()
    {
        var habit = NewHabit();

        Assert.Throws<DomainException>(() => habit.CheckIn(Today.AddDays(1), Today));
    }

    [Fact]
    public void CheckIn_SameDateTwice_Throws()
    {
        var habit = HabitWithCheckIns(0);

        Assert.Throws<DomainException>(() => habit.CheckIn(Today, Today));
    }

    [Fact]
    public void CheckIn_OnArchivedHabit_Throws()
    {
        var habit = NewHabit();
        habit.Archive();

        Assert.Throws<DomainException>(() => habit.CheckIn(Today, Today));
    }

    [Fact]
    public void Create_WithBlankName_Throws()
    {
        Assert.Throws<DomainException>(() => Habit.Create(UserId, "   "));
    }

    [Fact]
    public void CurrentStreak_CountsConsecutiveDaysEndingToday()
    {
        var habit = HabitWithCheckIns(-5, -4, -3, -1, 0);

        Assert.Equal(2, habit.CurrentStreak(Today));
    }

    [Fact]
    public void CurrentStreak_IsStillAliveWhenOnlyYesterdayIsChecked()
    {
        var habit = HabitWithCheckIns(-2, -1);

        Assert.Equal(2, habit.CurrentStreak(Today));
    }

    [Fact]
    public void CurrentStreak_IsZeroAfterAGapOfMoreThanOneDay()
    {
        var habit = HabitWithCheckIns(-4, -3);

        Assert.Equal(0, habit.CurrentStreak(Today));
    }

    [Fact]
    public void BestStreak_ReturnsTheLongestRun()
    {
        var habit = HabitWithCheckIns(-5, -4, -3, -1, 0);

        Assert.Equal(3, habit.BestStreak());
    }
}
```

## Fakes para Application

`FixedTimeProvider` fija la zona en **UTC** a propósito: así `Today()` es determinista y el test no
depende de la zona horaria de la máquina que lo ejecuta.

`InMemoryHabitRepository` **filtra por `userId` y excluye archivados**, exactamente igual que
`HabitRepository`. No es un detalle: el *fake* es una implementación más de `IHabitRepository`, así
que le aplica LSP. Si devolviera todos los hábitos, el test de IDOR **pasaría contra el *fake* y
fallaría en producción**, que es exactamente el fallo que el test de IDOR existe para detectar.

`tests/LifeTrack.Application.Tests/Fakes/Fakes.cs`

```csharp
using LifeTrack.Application.Abstractions;
using LifeTrack.Domain.Finance;
using LifeTrack.Domain.Habits;

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

internal sealed class InMemoryHabitRepository : IHabitRepository
{
    public List<Habit> Items { get; } = [];

    public Task AddAsync(Habit habit, CancellationToken cancellationToken)
    {
        Items.Add(habit);
        return Task.CompletedTask;
    }

    public Task<Habit?> GetByIdAsync(Guid habitId, Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(Items.FirstOrDefault(h => h.Id == habitId && h.UserId == userId && !h.IsArchived));

    public Task<IReadOnlyList<Habit>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Habit>>(Items.Where(h => h.UserId == userId && !h.IsArchived).ToList());
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

    public Task<bool> ExistsAsync(Guid categoryId, Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Any(c => c.Id == categoryId && c.UserId == userId));

    public Task<bool> NameExistsAsync(Guid userId, string name, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Any(c => c.UserId == userId && c.Name == name));
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
}
```

## Test de un caso de uso enviándolo por `ISender`

`tests/LifeTrack.Application.Tests/CheckInHabitTests.cs`

**Preferido:** enviar el contrato por `ISender`, igual que un controller. El test cubre entonces el
enlace contrato → handler, y un contrato mal declarado o un handler sin registrar falla aquí y no en
producción. Los dobles siguen **escritos a mano**: MediatR solo resuelve el contrato → handler, y meter
una librería de *mocking* solo aportaría una dependencia que auditar.

```csharp
using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using LifeTrack.Application.Habits;
using LifeTrack.Application.Tests.Fakes;
using LifeTrack.Domain.Habits;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace LifeTrack.Application.Tests;

public class CheckInHabitTests
{
    private static readonly DateOnly Today = new(2026, 10, 2);
    private readonly Guid _owner = Guid.NewGuid();
    private readonly InMemoryHabitRepository _habits = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private ISender SenderFor(Guid userId)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHabitRepository>(_habits);
        services.AddSingleton<IUnitOfWork>(_unitOfWork);
        services.AddSingleton<ICurrentUser>(new FakeCurrentUser(userId));
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(Today));
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyAnchor).Assembly));

        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }

    private Habit ExistingHabit()
    {
        var habit = Habit.Create(_owner, "Meditar");
        _habits.Items.Add(habit);
        return habit;
    }

    [Fact]
    public async Task Handle_RegistersTodayAndSaves()
    {
        var habit = ExistingHabit();

        await SenderFor(_owner).Send(new CheckInHabitCommand(habit.Id), CancellationToken.None);

        Assert.Equal(1, habit.CurrentStreak(Today));
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_HabitOfAnotherUser_ThrowsResourceNotFoundAndDoesNotSave()
    {
        var habit = ExistingHabit();
        var attacker = Guid.NewGuid();

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            SenderFor(attacker).Send(new CheckInHabitCommand(habit.Id), CancellationToken.None));

        Assert.Empty(habit.CheckIns);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_TwiceOnTheSameDay_ThrowsBusinessRule()
    {
        var habit = ExistingHabit();
        var sender = SenderFor(_owner);
        await sender.Send(new CheckInHabitCommand(habit.Id), CancellationToken.None);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            sender.Send(new CheckInHabitCommand(habit.Id), CancellationToken.None));
    }
}
```

**Alternativa válida** para un test centrado en un único handler: construirlo directamente y llamar a
`Handle(request, cancellationToken)`. Es más rápido porque no monta el dispatcher, pero no cubre el
enlace contrato → handler.

```csharp
var handler = new CheckInHabitHandler(_habits, _unitOfWork, new FakeCurrentUser(_owner), new FixedTimeProvider(Today));

await handler.Handle(new CheckInHabitCommand(habit.Id), CancellationToken.None);
```

Un test de Application espera **excepciones de frontera**: el handler ya tradujo las del dominio, así
que `ResourceNotFoundException` y `BusinessRuleException` son lo que sale del caso de uso.
`DomainException` y `NotFoundException` solo se esperan en los tests de **dominio**.

El test de IDOR no se conforma con comprobar la excepción: además mira `SaveCount == 0` y que no se
añadiera ningún *check-in*. Un test que solo mira la excepción no verifica que el sistema quedó
consistente, y una brecha puede dejar un registro a medias sin que el test se entere.

## Tests de arquitectura (regla de dependencias ejecutable)

Esta clase es la **versión ejecutable de la regla de dependencias de `AGENTS.md`**: si alguien rompe
el *layering*, el CI falla aquí. Es la única puerta automática de SOLID, y cubre exactamente la
**D** (inversión de dependencias), porque es la única letra cuyo *sentido* se puede verificar
automáticamente. SRP, OCP, ISP y LSP no se comprueban aquí: son criterio de revisión.

Los dos tests que leen `Program.cs` comprueban cosas que **no** se pueden expresar con NetArchTest:
que el filtro anti-forgery global siga registrado y que el escaneo de MediatR apunte al ensamblado de
`Application`. Ambos leen el código fuente porque el registro ocurre en un archivo de compositions,
no en un atributo de tipo.

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
            .HaveDependencyOn("LifeTrack.Application.Habits.CreateHabitHandler")
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

