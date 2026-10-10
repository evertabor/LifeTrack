# AGENTS.md — LifeTrack.Infrastructure

Reglas generales en `/AGENTS.md`. Esta capa **implementa** lo que `Application` declara.

## Dependencias
- Referencia a `LifeTrack.Application`. Paquetes permitidos: `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`. Versiones en `Directory.Packages.props`.
- **Nunca** referencia a `LifeTrack.Web`.

## Persistencia
- `AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IUnitOfWork`. `ApplyConfigurationsFromAssembly`.
- Una `IEntityTypeConfiguration<T>` por entidad (`internal sealed`) en `Persistence/Configurations`.
- Siempre: `ValueGeneratedNever()` en el `Id`, `HasMaxLength` en textos, `HasPrecision(18, 2)` en dinero, `HasConversion<string>()` en enums, índices por `UserId`, índices únicos para invariantes, FK a `ApplicationUser` con `Cascade`.
- Las FK entre entidades de negocio (p. ej. `Transaction → Category`) usan `Restrict`.
- Navegaciones de colección: `UsePropertyAccessMode(PropertyAccessMode.Field)`.
- Migraciones en `Persistence/Migrations`; una por cambio, con nombre descriptivo; no se editan las ya aplicadas.

## Repositorios
- `internal sealed`, en `Persistence/Repositories`, implementan interfaces de `Application`.
- **Todas** las consultas incluyen `UserId`. Lecturas con `AsNoTracking()`. Sin `SaveChangesAsync` (lo hace el handler).
- LINQ parametrizado. Prohibido concatenar SQL; si hace falta SQL crudo, `FromSql($"...")` interpolado.
- Evita N+1: `Include` explícito o proyecciones.
- El repositorio usa constructor primario de C# 12. `DbContext` y `IdentityDbContext` están excluidos por requisito de `base(options)`.
- **Cero comentarios** en el código de esta capa.

## Configuración y DI
- `DependencyInjection.AddInfrastructure(IConfiguration)`: cadena `DefaultConnection` obligatoria (falla al arrancar si falta), `EnableRetryOnFailure()` (Azure SQL serverless se pausa), repositorios *scoped*, `TimeProvider` con la zona `App:TimeZoneId`.
- Secretos solo desde configuración (user-secrets / App Settings). Nada en el código.

## Reloj
`Time/ZonedTimeProvider` sobrescribe `LocalTimeZone` para que "hoy" siga la zona del usuario y no la del servidor (UTC).
