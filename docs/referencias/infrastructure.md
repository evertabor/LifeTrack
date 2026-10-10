# Parte D — Infrastructure  ⚠️ sin compilar

## Configuración EF (ValueGeneratedNever, colección por campo, índices, cascada)

`src/LifeTrack.Infrastructure/Persistence/Configurations/HabitConfiguration.cs`

```csharp
using LifeTrack.Domain.Habits;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeTrack.Infrastructure.Persistence.Configurations;

internal sealed class HabitConfiguration : IEntityTypeConfiguration<Habit>
{
    public void Configure(EntityTypeBuilder<Habit> builder)
    {
        builder.ToTable("Habits");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();

        builder.Property(h => h.Name).HasMaxLength(Habit.NameMaxLength).IsRequired();
        builder.HasIndex(h => h.UserId);

        builder.HasMany(h => h.CheckIns)
            .WithOne()
            .HasForeignKey(c => c.HabitId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(h => h.CheckIns).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(h => h.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
```

## Repositorio (la propiedad del recurso forma parte de la consulta)

La consulta filtra por `h.UserId == userId`, así que el hábito de otro usuario **simplemente no se
encuentra**: la respuesta es la misma que para un id inexistente y no se filtra nada por el camino
(OWASP A01). `Include(h => h.CheckIns)` carga los check-ins en la misma consulta en lugar de disparar
una por hábito (N+1), y las lecturas usan `AsNoTracking()` porque no van a modificar la entidad.
El repositorio **no** llama a `SaveChangesAsync`: eso lo hace el handler vía `IUnitOfWork`.

`src/LifeTrack.Infrastructure/Persistence/Repositories/HabitRepository.cs`

```csharp
using LifeTrack.Application.Abstractions;
using LifeTrack.Domain.Habits;
using Microsoft.EntityFrameworkCore;

namespace LifeTrack.Infrastructure.Persistence.Repositories;

internal sealed class HabitRepository(AppDbContext db) : IHabitRepository
{
    public async Task AddAsync(Habit habit, CancellationToken cancellationToken) =>
        await db.Habits.AddAsync(habit, cancellationToken);

    public Task<Habit?> GetByIdAsync(Guid habitId, Guid userId, CancellationToken cancellationToken) =>
        db.Habits
            .Include(h => h.CheckIns)
            .FirstOrDefaultAsync(h => h.Id == habitId && h.UserId == userId && !h.IsArchived, cancellationToken);

    public async Task<IReadOnlyList<Habit>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        await db.Habits
            .AsNoTracking()
            .Include(h => h.CheckIns)
            .Where(h => h.UserId == userId && !h.IsArchived)
            .OrderBy(h => h.Name)
            .ToListAsync(cancellationToken);
}
```

## Reloj con zona horaria

Hace que **"hoy" siga la zona horaria del usuario y no la del servidor**, que en Azure App Service es
UTC. Sobrescribir `LocalTimeZone` es suficiente: todo lo demás que usa `GetLocalNow()` hereda la zona
sin tener que convertir la fecha a mano.

`src/LifeTrack.Infrastructure/Time/ZonedTimeProvider.cs`

```csharp
namespace LifeTrack.Infrastructure.Time;

public sealed class ZonedTimeProvider(TimeZoneInfo timeZone) : TimeProvider
{
    public override TimeZoneInfo LocalTimeZone { get; } = timeZone;
}
```

## Registro de servicios de Infrastructure

Azure SQL **serverless se pausa** cuando no hay actividad, y al reanudarse falla la primera
conexión: `EnableRetryOnFailure()` reintenta esos errores transitorios en lugar de devolver un 500 al
usuario. La cadena de conexión es **obligatoria** y su ausencia detiene el arranque con un mensaje
explícito, para que un despliegue sin secreto falle de forma ruidosa y no en la primera petición.

`src/LifeTrack.Infrastructure/DependencyInjection.cs`

```csharp
using LifeTrack.Application.Abstractions;
using LifeTrack.Infrastructure.Persistence;
using LifeTrack.Infrastructure.Persistence.Repositories;
using LifeTrack.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LifeTrack.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not configured. " +
                "Use 'dotnet user-secrets' locally or App Settings in Azure. Never commit it.");
        }

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(
            connectionString,
            sql => sql.EnableRetryOnFailure()));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IHabitRepository, HabitRepository>();

        var zoneId = configuration["App:TimeZoneId"] ?? "UTC";
        services.AddSingleton<TimeProvider>(new ZonedTimeProvider(TimeZoneInfo.FindSystemTimeZoneById(zoneId)));

        return services;
    }
}
```

