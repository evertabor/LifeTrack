# Tasks: fase-2-movimientos

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~1200 (30 archivos en 5 capas) |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | Single PR con size:exception aprobada por maintainer |
| Delivery strategy | single-pr |
| Chain strategy | size:exception |

### Suggested Work Units

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|------|------|-----------|----------------------|-----------------|-------------------|
| 1 | Slice vertical de movimientos + resumen mensual | PR 1 (size:exception) | `dotnet test --filter "Transaction"` | App local con SQL Server en docker | `migrations remove` + borrado de archivos New |

### Breakdown

| Phase | Tasks | Focus |
|-------|-------|-------|
| Phase 1 | 4 | Domain |
| Phase 2 | 7 | Application |
| Phase 3 | 4 | Infrastructure |
| Phase 4 | 8 | Web |
| Phase 5 | 5 | Testing |
| Phase 6 | 3 | Migration / Styles / Bugfix |
| Threat Matrix | 4 | RED security tests |
| Total | 35 | |

## Phase 1: Domain

- [x] 1.1 Crear `src/LifeTrack.Domain/Finance/TransactionType.cs` con enum `Income = 1`, `Expense = 2`
- [x] 1.2 Crear `src/LifeTrack.Domain/Finance/Transaction.cs` con fábrica `Create(userId, categoryId, type, amount, date, description, today)`, constantes `MaxAmount = 1_000_000_000m` y `DescriptionMaxLength = 200`, invariantes RN-F01..RN-F03 con `DomainException` en español
- [x] 1.3 Crear `tests/LifeTrack.Domain.Tests/Finance/TransactionTests.cs` con 12 tests: válido, monto 0, negativo, 3 decimales, sobre el máximo, mañana falla, hoy pasa, trim de descripción, 201 caracteres falla, tipo indefinido falla, `UserId`/`CategoryId` vacíos fallan
- [x] 1.4 Ejecutar tests de dominio en verde (12/12)

## Phase 2: Application

- [x] 2.1 Crear `src/LifeTrack.Application/Abstractions/ITransactionRepository.cs` con `AddAsync` y `ListByPeriodAsync(userId, from, toExclusive, ct)`
- [x] 2.2 Crear `src/LifeTrack.Application/Common/TimeProviderExtensions.cs` con `Today()` para RN-G01
- [x] 2.3 Crear `CreateTransactionCommand : ICommand<Guid>` (con `bool IsIncome`) y `CreateTransactionHandler` sealed con verificación de propiedad de categoría y traducción `DomainException`→`BusinessRuleException`, `NotFoundException`→`ResourceNotFoundException`
- [x] 2.4 Crear `ListMonthlyTransactionsQuery(Year, Month, CategoryId?)`, DTOs `TransactionSummary` (con `IsIncome` + `TypeLabel`), `CategoryExpenseSummary`, `MonthlyTransactionsResult` (con `Balance` calculado) y handler con validación de periodo y filtro
- [x] 2.5 Crear `FakeTransactionRepository` (filtra por usuario y rango) y `FixedTimeProvider` (UTC fijo) en tests
- [x] 2.6 Crear `CreateTransactionHandlerTests.cs` con 5 tests: éxito, monto 0, fecha futura, categoría ajena (IDOR sin guardar), sin usuario
- [x] 2.7 Crear `ListMonthlyTransactionsHandlerTests.cs` con 7 tests: totales 1000/400/600, desglose que suma, mes vacío en ceros, filtro por categoría, filtro ajeno, periodo inválido, sin usuario

## Phase 3: Infrastructure

- [x] 3.1 Crear `TransactionConfiguration.cs` con tabla `Transactions`, `ValueGeneratedNever()`, `HasPrecision(18, 2)`, enum como string, `HasMaxLength(200)`, índices en `UserId` y (`UserId`, `Date`), FK Cascade a usuarios y Restrict a categorías
- [x] 3.2 Crear `TransactionRepository.cs` con constructor primario, filtro por usuario y rango, `AsNoTracking()`, sin `SaveChangesAsync`
- [x] 3.3 Modificar `AppDbContext.cs` (`DbSet<Transaction>`) y `DependencyInjection.cs` (registro scoped)
- [x] 3.4 Crear migración `CreateTransaction` y convertirla a namespace file-scoped (regla de estilo)

## Phase 4: Web

- [x] 4.1 Crear `CreateTransactionInputModel.cs` con `CategoryId`/`Amount`/`Date` nulables y `[Required]`, `Range` con `double`, `StringLength(200)`
- [x] 4.2 Crear `TransactionRowViewModel.cs` con `From(...)`: importe con signo, `AmountCssClass`, fecha `dd/MM/yyyy`
- [x] 4.3 Crear `TransactionListViewModel.cs` con `From(...)`: métricas formateadas, `MonthLabel`, opciones de categoría, booleanos `Has*`/`Is*Selected`
- [x] 4.4 Crear `TransactionsController.cs` con `[Route("transactions")]`, `[Authorize]`, `ISender` + `ILogger` + `TimeProvider`, `Index` GET y `Create` POST con PRG al mes del movimiento
- [x] 4.5 Crear `Views/Transactions/Index.cshtml` sin lógica: filtro, métricas, gasto por categoría, tabla con `data-transactions-label`, vacíos, `<label>` por campo
- [x] 4.6 Crear `Views/Transactions/Partials/_TransactionForm.cshtml`: modal con validación por campo y textos de ayuda
- [x] 4.7 Crear `wwwroot/js/pages/transactions.js`: IIFE con bloques `SELECTORS`/`CSS_CLASSES`/`CONSTANTS` y reapertura del modal con errores
- [x] 4.8 Añadir enlace `Movimientos` al sidebar y actualizar `DESIGN.md`

## Phase 5: Testing

- [x] 5.1 Ejecutar `dotnet build LifeTrack.sln` sin warnings (0/0)
- [x] 5.2 Ejecutar `dotnet test LifeTrack.sln -c Release`: 100 tests en verde (22 dominio, 52 aplicación, 11 arquitectura, 15 integración)
- [x] 5.3 Ejecutar `dotnet format --verify-no-changes` limpio
- [x] 5.4 Crear `CreateTransactionInputModelValidationTests.cs`: regresión del bug de `Range` con cultura (4 tests)
- [x] 5.5 Crear `TransactionsPageTests.cs`: `GET` y `POST /transactions` anónimos redirigen a login (2 tests)

## Phase 6: Migration / Styles / Bugfix

- [x] 6.1 Aplicar `dotnet ef database update` en SQL Server local: tabla `Transactions` creada con FK e índices
- [x] 6.2 Añadir estilos `transactions-*` y `app-amount` en `components.css` solo con tokens `--app-*`, espejando categorías y `DESIGN.md` §8.5
- [x] 6.3 Corregir `[Range(typeof(decimal), "0.01", ...)]` por `[Range(0.01, 1000000000)]` tras el `ArgumentException` en la vista con cultura es-ES

## Threat Matrix Cases — RED Tests

- [x] TM-1 **IDOR en CreateTransaction**: `Handle_CategoryOfAnotherUser_ThrowsResourceNotFoundAndDoesNotSave` — excepción más `SaveCount == 0`
- [x] TM-2 **IDOR en listado/filtro**: `Handle_CategoryOfAnotherUser_ThrowsResourceNotFound` y aislamiento por `userId` en `ListByPeriodAsync`
- [x] TM-3 **Unauthenticated Access**: `TransactionsPageTests` verifica 302 a `/Account/Login` en GET y POST; handlers fallan cerrado sin usuario
- [x] TM-4 **Concurrencia**: N/A documentado — `Transaction` no tiene invariante de unicidad, no hay índice único en juego ni condición de carrera sensible

## Implementation Notes

- La frontera usa `bool IsIncome` porque los tests de arquitectura prohíben `Domain` en Web; el ejemplo de `docs/referencias/application.md` que expone el enum no se siguió en este punto.
- `Range` con `typeof(decimal)` y literales con punto decimal está prohibido por el bug de cultura (ver 6.3 y `verify-report.md`).
- La migración se aplicó en local a diferencia de fase-2-categorias (tarea 5.3 de aquel change), porque el usuario lo pidió explícitamente.
- Debug local queda bloqueado si la app corre desde Visual Studio (MSB3021); la verificación se ejecutó en Release.
