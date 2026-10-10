# AGENTS.md — LifeTrack.Domain

Reglas generales en `/AGENTS.md`. Esta capa es el **núcleo del negocio**.

## Dependencias
- **Cero** paquetes NuGet y **cero** referencias a otros proyectos.
- Prohibido: EF Core, ASP.NET, `System.Data`, atributos de persistencia o de validación de UI, `HttpContext`, logging.

## Cómo se escribe una entidad
- Hereda de `Entity` (propiedad `Id` de tipo `Guid`).
- Constructor privado sin parámetros (para EF) + constructor privado con datos + **fábrica estática `Create(...)`** que valida.
- `private set` en todas las propiedades. Colecciones: `private readonly List<T>` expuesta como `IReadOnlyCollection<T>`.
- El id se genera aquí (`Guid.NewGuid()`).
- Los **invariantes** viven en la entidad y lanzan `DomainException` con mensaje en español, seguro de mostrar al usuario.
- Sin I/O, sin `async`, sin estado estático, sin `DateTime.Now`: la fecha actual llega como parámetro (`today`) para poder probar.
- Tipos: `decimal` para dinero (máx. 2 decimales, validado), `DateOnly` para fechas sin hora, enums explícitos.
- Un agregado se modifica solo a través de la raíz (p. ej. `Habit.CheckIn`); los hijos se crean con `internal static Create`.
- **Cero comentarios** en el código de esta capa. El nombre del método, del parámetro y de la excepción ya dicen qué valida cada invariante; si una razón no cabe en el código, se documenta en `docs/`.

## Contenido previsto
`Common/` (`Entity`, `Guard`), `Common/Exceptions/` (`DomainException`, `NotFoundException`), `Finance/` (`Category`, `Transaction`, `Budget`, `TransactionType`), `Habits/` (`Habit`, `HabitCheckIn`).

Las excepciones compartidas del dominio viven en `Domain/Common/Exceptions`. Cada tipo público vive en su propio archivo.

## Pruebas
Se prueba por **criterio de riesgo** (sección 10 de `AGENTS.md`): todo invariante que, si falla,
produce un número o un estado incorrecto que el usuario ve sin notar (una racha mal calculada, un
total que no cuadra) es un fallo silencioso, y por tanto tiene test en
`tests/LifeTrack.Domain.Tests`: caso válido, cada violación y los bordes (fecha de hoy vs. mañana,
monto 0 y negativo, 2 vs. 3 decimales). Los *getters* y *setters* sin invariantes no se prueban.
