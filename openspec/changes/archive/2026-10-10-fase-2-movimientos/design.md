# Design: fase-2-movimientos

> Nota: documento redactado a posteriori. Describe lo construido, no lo planificado.

## Enfoque Técnico

Rebanada vertical HU-05/HU-06 construida con Clean Architecture. La entidad `Transaction` defiende RN-F01..RN-F03 en su fábrica con `today` inyectado para determinismo. `CreateTransaction` verifica propiedad de la categoría con el repositorio existente de categorías. `ListMonthlyTransactions` resuelve en un solo handler el listado (RF-F03) y el resumen (RF-F05, RF-D01). La frontera Web–Application usa primitivos (`bool IsIncome`, `TypeLabel`) para que ningún tipo de Web dependa de `Domain`, como exigen los tests de arquitectura.

## Decisiones de Arquitectura

### Decisión: Frontera con primitivos en lugar del enum de dominio

**Elección**: `CreateTransactionCommand` lleva `bool IsIncome`; `TransactionSummary` lleva `bool IsIncome` + `string TypeLabel` precalculados. El handler mapea el booleano a `TransactionType`.

**Alternativas consideradas**:
- Exponer `TransactionType` de `Domain` en el comando y el DTO (como sugiere el ejemplo de `docs/referencias/application.md`): rechazado porque `TransactionsController` y los `*ViewModel` pasarían a depender de `LifeTrack.Domain` y romperían `Controllers_TalkToApplication_NotToInfrastructureOrDomain` y `ViewModels_DoNotInheritFromDomainEntities`.
- Duplicar el enum en Application: rechazado por duplicar el vocabulario del dominio sin aportar nada.

**Justificación**: los tests de arquitectura son la puerta exigible de la D de SOLID; el mapeo bool↔enum en el handler es una línea y mantiene la frontera libre de `Domain`.

### Decisión: `today` como parámetro del dominio y extensión `Today()`

**Elección**: `Transaction.Create(..., DateOnly today)` y `TimeProviderExtensions.Today()` en `Application/Common`, que calcula `DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime)` para respetar `App:TimeZoneId` (RN-G01).

**Alternativas consideradas**:
- Leer el reloj dentro del dominio: rechazado porque impide tests deterministas.
- Crear un `Guard` compartido como propone la referencia: rechazado para no introducir un segundo estilo junto a `Category`, que valida en línea.

**Justificación**: la regla de fecha futura es fallo silencioso si depende del día de ejecución; con `today` inyectado los tests fijan `2026-10-10` y son deterministas.

### Decisión: Un solo handler para listado y resumen

**Elección**: `ListMonthlyTransactionsHandler` devuelve `MonthlyTransactionsResult` con transacciones, totales, balance calculado y desglose por categoría.

**Alternativas consideradas**:
- Dos queries separadas (listado + resumen): rechazado porque la tabla de casos de uso de `docs/01-especificacion.md` §8 define un único `ListMonthlyTransactionsHandler` para RF-F03, RF-F05 y RF-D01, y dos consultas duplicarían el rango y el filtro.

**Justificación**: una sola lectura del rango con dos proyecciones en memoria; el dataset personal es pequeño y se evita N+1 consultando categorías una vez.

### Decisión: FK Restrict hacia categorías, Cascade hacia usuarios

**Elección**: `Transaction → Category` con `DeleteBehavior.Restrict` (RN: no se elimina categoría con movimientos, RF-F01); `Transaction → ApplicationUser` con `Cascade` (RN-G02, derecho al olvido). Índices en `UserId` y (`UserId`, `Date`).

**Justificación**: replica el contrato de `AGENTS.md` de Infrastructure y anticipa RF-F01 sin implementarlo.

### Decisión: `InputModel` con nulables y `Range` con `double`

**Elección**: `CategoryId`, `Amount` y `Date` son nulables con `[Required]` para mensajes en español; el rango usa `[Range(0.01, 1000000000)]` con `double`; el binding del formulario usa prefijo `NewTransaction`.

**Alternativas consideradas**:
- `[Range(typeof(decimal), "0.01", ...)]`: adoptado inicialmente y revertido tras el bug real — `DecimalConverter` parsea con la cultura del servidor y en es-ES lanza `FormatException` (ver `apply-progress.md`).
- Tipos no nulables: rechazado porque `Guid.Empty` y `default` pasarían `[Required]` con mensajes en inglés del binder.

**Justificación**: presencia y formato en el `InputModel` con mensajes españoles; invariantes de negocio en el dominio como autoridad.

### Decisión: Formato íntegro en el `ViewModel`

**Elección**: importes `N2` en es-ES con signo (`+`/`−`), fechas `dd/MM/yyyy`, `MonthLabel` capitalizado, `AmountCssClass` e `IsIncomeSelected`/`IsExpenseSelected` decididos en `From(...)`.

**Justificación**: `docs/09-estandar-vistas-mvc.md` prohíbe ternarios y formateo en Razor; `DESIGN.md` §8.5 exige signo y `tabular-nums`, y §8.2 exige clases de estado preparadas.

## Flujo de Datos

```
Navegador (POST /transactions/create)
    │
    ▼
TransactionsController.Create(NewTransaction)
    │ Valida DataAnnotations (presencia y formato)
    │ Captura BusinessRuleException → ModelState (resumen + campo)
    │ Captura ResourceNotFoundException → LogWarning + error de campo
    │
    ▼
sender.Send(CreateTransactionCommand, ct)
    │
    ▼
CreateTransactionHandler.Handle(...)
    │ UserId nulo → BusinessRuleException
    │ category = GetByIdAsync(id, userId) ?? NotFoundException
    │ transaction = Transaction.Create(..., timeProvider.Today())
    │ AddAsync + SaveChangesAsync → Guid
    │ catch DomainException → BusinessRuleException
    │ catch NotFoundException → ResourceNotFoundException
    │
    ▼
RedirectToAction(Index, año/mes del movimiento)
    │
    ▼
sender.Send(ListMonthlyTransactionsQuery) → MonthlyTransactionsResult
    │
    ▼
TransactionListViewModel.From(result, categories, message)
    │
    ▼
Index.cshtml → filtro, métricas, tablas, modal
```

## Cambios en Archivos

| Archivo | Acción | Descripción |
|---|---|---|
| `src/LifeTrack.Domain/Finance/TransactionType.cs` | Crear | Enum `Income = 1`, `Expense = 2` |
| `src/LifeTrack.Domain/Finance/Transaction.cs` | Crear | Entidad, fábrica con RN-F01..RN-F03, `today` inyectado |
| `src/LifeTrack.Application/Common/TimeProviderExtensions.cs` | Crear | `Today()` sobre `TimeProvider` |
| `src/LifeTrack.Application/Abstractions/ITransactionRepository.cs` | Crear | `AddAsync`, `ListByPeriodAsync` |
| `.../CreateTransaction/CreateTransactionCommand.cs` | Crear | `record` con `CategoryId`, `IsIncome`, `Amount`, `Date`, `Description` |
| `.../CreateTransaction/CreateTransactionHandler.cs` | Crear | Verificación de propiedad + traducción de excepciones |
| `.../ListMonthlyTransactions/ListMonthlyTransactionsQuery.cs` | Crear | `Year`, `Month`, `CategoryId?` opcional |
| `.../ListMonthlyTransactions/TransactionSummary.cs` | Crear | DTO con `IsIncome` + `TypeLabel` |
| `.../ListMonthlyTransactions/CategoryExpenseSummary.cs` | Crear | DTO de desglose |
| `.../ListMonthlyTransactions/MonthlyTransactionsResult.cs` | Crear | Totales + `Balance` calculado |
| `.../ListMonthlyTransactions/ListMonthlyTransactionsHandler.cs` | Crear | Validación de periodo, filtro, totales |
| `.../Configurations/TransactionConfiguration.cs` | Crear | Tabla, precisión, enum string, índices, FK |
| `.../Repositories/TransactionRepository.cs` | Crear | Filtro por usuario y rango, `AsNoTracking()` |
| `Persistence/AppDbContext.cs` | Modificar | `DbSet<Transaction> Transactions` |
| `DependencyInjection.cs` | Modificar | Registro scoped del repositorio |
| `Controllers/TransactionsController.cs` | Crear | `Index` GET, `Create` POST, `TimeProvider` inyectado |
| `ViewModels/Transactions/CreateTransactionInputModel.cs` | Crear | Nulables + `Range` con `double` |
| `ViewModels/Transactions/TransactionRowViewModel.cs` | Crear | Signo, clase CSS, textos listos |
| `ViewModels/Transactions/TransactionListViewModel.cs` | Crear | Métricas, opciones, booleanos de visibilidad |
| `Views/Transactions/Index.cshtml` | Crear | Filtro, resumen, tablas, vacíos |
| `Views/Transactions/Partials/_TransactionForm.cshtml` | Crear | Modal con validación por campo |
| `_AuthenticatedSidebar.cshtml` | Modificar | Enlace `Movimientos` |
| `wwwroot/css/components.css` | Modificar | `transactions-*`, `app-amount` |
| `wwwroot/js/pages/transactions.js` | Crear | Reapertura del modal con errores |
| `DESIGN.md` | Modificar | Navegación incluye `Transactions` |
| `tests/.../TransactionTests.cs` | Crear | 12 tests de invariantes |
| `tests/.../Fakes/FakeTransactionRepository.cs` | Crear | Fake con filtro por usuario y rango |
| `tests/.../Fakes/FixedTimeProvider.cs` | Crear | Reloj fijo en UTC |
| `tests/.../CreateTransactionHandlerTests.cs` | Crear | 5 tests con IDOR |
| `tests/.../ListMonthlyTransactionsHandlerTests.cs` | Crear | 7 tests de totales, filtro y periodo |
| `tests/.../TransactionsPageTests.cs` | Crear | Redirect a login sin auth |
| `tests/.../CreateTransactionInputModelValidationTests.cs` | Crear | Regresión del bug de `Range` |
| Migración `CreateTransaction` | Crear | Tabla, índices, FK; aplicada en local |

## Estrategia de Pruebas

| Capa | Qué probar | Enfoque |
|---|---|---|
| Domain | RN-F01 (0, negativo, >máx, 3 decimales), RN-F02 (mañana falla, hoy pasa), RN-F03 (trim, 201 falla), enum inválido, guids vacíos | `TransactionTests`, fecha fija |
| Application | Creación válida, monto 0 y fecha futura → `BusinessRuleException` sin guardar, categoría ajena → `ResourceNotFoundException` sin guardar, sin usuario → `BusinessRuleException` | Handlers directos con fakes |
| Application | Totales 1000/400/600, desglose que suma, mes vacío en ceros, filtro por categoría, filtro ajeno → `ResourceNotFoundException`, periodo inválido, sin usuario | `ListMonthlyTransactionsHandlerTests` |
| Web/InputModel | Entrada válida sin errores; regresión: `Range` no lanza con cultura es-ES | `Validator.TryValidateObject` |
| Architecture | Sin dependencia Web→Domain; controllers sin handlers concretos; antiforgery y MediatR registrados | `DependencyRulesTests` sin tocar |
| Integration | `GET/POST /transactions` anónimo → 302 a login | `WebApplicationFactory` + SQLite |

## Threat Matrix

N/A como matriz separada: los casos van en tasks.md (TM-1..TM-4) porque la feature no modifica routing global, shell commands ni automation.

## Migración y Despliegue

1. Crear: `dotnet ef migrations add CreateTransaction --project src/LifeTrack.Infrastructure --startup-project src/LifeTrack.Web --output-dir Persistence/Migrations`.
2. La migración generada usa namespace de bloque: convertir a file-scoped (regla `csharp_style_namespace_declarations`, warnings como errores).
3. Aplicar en local: `dotnet ef database update` con el mismo par de proyectos (aplicada el 2026-10-10; crea `Transactions` con FK e índices).
4. En producción la aplica el pipeline, nunca el arranque (salvo Development).

## Preguntas Abiertas

- [ ] Render autenticado de `/transactions` de extremo a extremo: cubierto solo el redirect anónimo; falta un test con usuario autenticado y datos (requiere factory con conexión SQLite compartida).
- [ ] RF-F04 (editar/eliminar) y RF-F06 (presupuestos): fuera de alcance, pendientes de Fase 5.
