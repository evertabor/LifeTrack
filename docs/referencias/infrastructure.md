# Parte D — Infrastructure

## Configuración EF (ValueGeneratedNever, índices, cascada)

`src/LifeTrack.Infrastructure/Persistence/Configurations/TransactionConfiguration.cs`

```csharp
using LifeTrack.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeTrack.Infrastructure.Persistence.Configurations;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Description).HasMaxLength(Transaction.DescriptionMaxLength);
        builder.Property(t => t.Amount).HasPrecision(18, 2);
        builder.HasIndex(t => new { t.UserId, t.Date });

        builder.HasOne<Category>().WithMany().HasForeignKey(t => t.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
```

## Repositorio (la propiedad del recurso forma parte de la consulta)

La consulta filtra por `t.UserId == userId`, así que la transacción de otro usuario **simplemente no se
encuentra**: la respuesta es la misma que para un id inexistente y no se filtra nada por el camino
(OWASP A01). Las lecturas usan `AsNoTracking()` porque no van a modificar la entidad.
El repositorio **no** llama a `SaveChangesAsync`: eso lo hace el handler vía `IUnitOfWork`.

`src/LifeTrack.Infrastructure/Persistence/Repositories/TransactionRepository.cs`

```csharp
using LifeTrack.Application.Abstractions;
using LifeTrack.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace LifeTrack.Infrastructure.Persistence.Repositories;

internal sealed class TransactionRepository(AppDbContext db) : ITransactionRepository
{
    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken) =>
        await db.Transactions.AddAsync(transaction, cancellationToken);

    public Task<Transaction?> GetByIdAsync(Guid transactionId, Guid userId, CancellationToken cancellationToken) =>
        db.Transactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<Transaction>> ListByPeriodAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken) =>
        await db.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.Date >= from && t.Date < to)
            .OrderByDescending(t => t.Date)
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

        var zoneId = configuration["App:TimeZoneId"] ?? "UTC";
        services.AddSingleton<TimeProvider>(new ZonedTimeProvider(TimeZoneInfo.FindSystemTimeZoneById(zoneId)));

        return services;
    }
}
```
