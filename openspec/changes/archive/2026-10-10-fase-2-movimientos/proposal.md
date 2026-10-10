# Proposal: fase-2-movimientos

> Nota: documento redactado a posteriori. Describe lo construido, no lo planificado.

## Intent

Se implementó el núcleo de movimientos de finanzas en LifeTrack: HU-05 (registrar un gasto/ingreso con RN-F01..F05) y HU-06 (resumen del mes con RF-F03, RF-F05, RF-D01). El slice introdujo la entidad `Transaction` y cerró la Fase 2 del plan (finanzas núcleo, requisitos M). Editar/eliminar movimientos (RF-F04), presupuestos (RF-F06) e importación CSV (RF-F07) quedaron fuera por ser Fase 5 u opcionales.

## Scope

### In Scope

- Entidad `Transaction` en Domain con invariantes RN-F01..RN-F03 y enum `TransactionType`
- Caso de uso `CreateTransaction` con verificación de propiedad de la categoría (RN-F05) y traducción de excepciones
- Caso de uso `ListMonthlyTransactions` con filtro por mes, filtro opcional por categoría y resumen (ingresos, gastos, balance, gasto por categoría)
- Extensión `TimeProviderExtensions.Today()` para RN-G01
- Repositorio `ITransactionRepository` y su implementación
- Migración EF `CreateTransaction` y aplicación en la base de datos local
- Controller `TransactionsController` con acciones de listado (GET) y creación (POST)
- Vistas MVC: `Index.cshtml` (filtro, resumen, tablas) y `_TransactionForm.cshtml` (modal de registro)
- Enlace `Movimientos` en la navegación lateral
- Estilos `transactions-*` en `components.css` con tokens `--app-*`
- Tests de dominio, de aplicación (incluidos IDOR), de validación del `InputModel` y de integración HTTP

### Out of Scope

- Editar y eliminar movimientos (RF-F04, Fase 5)
- Presupuestos mensuales por categoría (RF-F06, Fase 5)
- Importación CSV (RF-F07, opcional)
- Correlación hábito–gasto (RF-D03, dashboard, Fase 5)
- Funcionalidad de hábitos (Fase 3)

## Capabilities

### New Capabilities

- `transaction-management`: Registrar movimientos (ingreso/gasto) en categorías propias con invariantes RN-F01..RN-F03, listar los movimientos de un mes con filtro opcional por categoría, y consultar el resumen mensual con totales y desglose por categoría. Una categoría ajena se rechaza como no válida sin revelar su existencia.

### Modified Capabilities

Ninguna. `Transaction` es una entidad nueva; `category-management` no se modifica (solo se consume su repositorio para verificar propiedad).

## Approach (lo aplicado)

1. La entidad `Transaction` expone la fábrica `Create(userId, categoryId, type, amount, date, description, today)` que valida RN-F01 (monto > 0, ≤ 1.000.000.000, 2 decimales), RN-F02 (fecha no futura contra `today` inyectado) y RN-F03 (descripción con `Trim`, ≤ 200). Lanza `DomainException` con mensaje en español seguro de mostrar.

2. El handler `CreateTransactionHandler` obtiene el usuario de `ICurrentUser`, verifica la categoría con `ICategoryRepository.GetByIdAsync(id, userId)` (nulo tanto si no existe como si es ajena → `NotFoundException` → `ResourceNotFoundException`), crea la entidad con `timeProvider.Today()` y guarda vía `IUnitOfWork`.

3. El handler `ListMonthlyTransactionsHandler` valida el periodo (año 2000–2100, mes 1–12 → `BusinessRuleException`), verifica el filtro de categoría si se indica, lee el rango `[from, from.AddMonths(1))` y devuelve totales, balance calculado y desglose por categoría ordenado. Un mes sin movimientos devuelve ceros.

4. La frontera Web–Application usa primitivos para no filtrar `Domain` a Web (los tests de arquitectura lo prohíben): el comando lleva `bool IsIncome` y el DTO de lectura lleva `IsIncome` + `TypeLabel` precalculados.

5. El controller `TransactionsController` inyecta `ISender`, `ILogger<T>` y `TimeProvider`; usa Post/Redirect/Get redirigiendo al mes del movimiento creado; captura `BusinessRuleException` → `ModelState` y `ResourceNotFoundException` → `LogWarning` + error de campo.

6. Las vistas siguen `DESIGN.md` y `docs/09-estandar-vistas-mvc.md`: métricas de resumen, importes con signo y `tabular-nums`, tablas responsive con etiquetas `data-transactions-*`, modal de registro, cero lógica.

## Affected Areas

| Area | Impact | Description |
|---|---|---|
| `src/LifeTrack.Domain/Finance/TransactionType.cs` | New | Enum `Income = 1`, `Expense = 2` |
| `src/LifeTrack.Domain/Finance/Transaction.cs` | New | Entidad con fábrica `Create(...)`, constantes `MaxAmount` y `DescriptionMaxLength` |
| `src/LifeTrack.Application/Common/TimeProviderExtensions.cs` | New | Extensión `Today()` sobre `TimeProvider` |
| `src/LifeTrack.Application/Abstractions/ITransactionRepository.cs` | New | `AddAsync`, `ListByPeriodAsync(userId, from, toExclusive, ct)` |
| `src/LifeTrack.Application/Finance/Commands/CreateTransaction/` | New | `CreateTransactionCommand : ICommand<Guid>` y `CreateTransactionHandler` |
| `src/LifeTrack.Application/Finance/Commands/ListMonthlyTransactions/` | New | Query, `TransactionSummary`, `CategoryExpenseSummary`, `MonthlyTransactionsResult`, handler |
| `src/LifeTrack.Infrastructure/Persistence/Configurations/TransactionConfiguration.cs` | New | Tabla `Transactions`, precisión (18,2), enum como string, índices, FK Cascade/Restrict |
| `src/LifeTrack.Infrastructure/Persistence/Repositories/TransactionRepository.cs` | New | Implementación con filtro por usuario y rango, `AsNoTracking()` |
| `src/LifeTrack.Infrastructure/Persistence/AppDbContext.cs` | Modified | `DbSet<Transaction> Transactions` |
| `src/LifeTrack.Infrastructure/DependencyInjection.cs` | Modified | Registro de `ITransactionRepository` |
| `src/LifeTrack.Web/Controllers/TransactionsController.cs` | New | Rutas `GET /transactions`, `POST /transactions/create` |
| `src/LifeTrack.Web/ViewModels/Transactions/` | New | `CreateTransactionInputModel`, `TransactionRowViewModel`, `TransactionListViewModel` |
| `src/LifeTrack.Web/Views/Transactions/Index.cshtml` | New | Filtro, resumen, gasto por categoría, tabla del mes, estados vacíos |
| `src/LifeTrack.Web/Views/Transactions/Partials/_TransactionForm.cshtml` | New | Modal de registro con validación por campo |
| `src/LifeTrack.Web/Views/Shared/Partials/_AuthenticatedSidebar.cshtml` | Modified | Enlace `Movimientos` |
| `src/LifeTrack.Web/wwwroot/css/components.css` | Modified | Clases `transactions-*` y compartida `app-amount` |
| `src/LifeTrack.Web/wwwroot/js/pages/transactions.js` | New | Reapertura del modal ante errores de servidor |
| `DESIGN.md` | Modified | La navegación enlaza `Transactions` |
| `tests/LifeTrack.Domain.Tests/Finance/TransactionTests.cs` | New | 12 tests de invariantes |
| `tests/LifeTrack.Application.Tests/Finance/` | New | `FakeTransactionRepository`, `FixedTimeProvider`, 12 tests de handlers con IDOR |
| `tests/LifeTrack.Integration.Tests/TransactionsPageTests.cs` | New | Redirect a login sin autenticación |
| `tests/LifeTrack.Integration.Tests/CreateTransactionInputModelValidationTests.cs` | New | Regresión del bug de `Range` con cultura |
| Migración EF | New | `CreateTransaction`, aplicada en local |

## Risks

| Risk | Likelihood | Mitigation |
|---|---|---|
| `TransactionType` de `Domain` filtrado a Web vía DTOs | Low | Frontera con primitivos desde el diseño; los tests de arquitectura lo verifican |
| `Range(typeof(decimal))` con la cultura del servidor | Medium | Sobrecarga con `double` + test de regresión (bug real encontrado y corregido) |
| Categoría ajena aceptada por error (IDOR) | Low | Verificación con `GetByIdAsync(id, userId)` + tests IDOR que comprueban `SaveCount == 0` |
| Vista con lógica o sin estilos | Low | `ViewModel` con booleanos y textos listos; estilos espejando la pantalla aprobada de categorías |

## Rollback Plan

1. Revertir la migración aplicada: `dotnet ef database update <PreviousMigration> --project src/LifeTrack.Infrastructure --startup-project src/LifeTrack.Web` y `dotnet ef migrations remove` con el mismo par de proyectos.
2. Eliminar los archivos listados como New en la tabla de áreas afectadas.
3. Revertir los archivos Modified (`AppDbContext.cs`, `DependencyInjection.cs`, `_AuthenticatedSidebar.cshtml`, `components.css`, `DESIGN.md`).
4. Ejecutar `dotnet build -c Release` y `dotnet test` para verificar la base.

## Dependencies

- `ICategoryRepository.GetByIdAsync` y `ListAsync` existen (fase-2-categorias) para verificar propiedad y poblar desplegables.
- `IUnitOfWork`, `ICurrentUser`, `BusinessRuleException`, `ResourceNotFoundException`, `DomainException`, `NotFoundException` existen (fase 1 y fase-2-categorias).
- MediatR 13.* pineado; contenedor SQL Server local en ejecución; secretos en user-secrets.

## Success Criteria (todos cumplidos, ver `verify-report.md`)

- [x] `Transaction.Create` rechaza monto 0, negativo, > 1.000.000.000 y 3 decimales con `DomainException`.
- [x] `Transaction.Create` rechaza fecha de mañana y acepta la de hoy (con `today` inyectado).
- [x] `CreateTransactionHandler` con categoría ajena lanza `ResourceNotFoundException` y no guarda nada.
- [x] `ListMonthlyTransactionsHandler` con ingresos 1000 y gastos 400 devuelve balance 600 y desglose que suma 400.
- [x] Un mes sin movimientos devuelve ceros sin errores.
- [x] Ningún tipo de `LifeTrack.Web` depende de `LifeTrack.Domain` (tests de arquitectura en verde).
- [x] `GET /transactions` sin autenticación redirige a login (test de integración).
- [x] `dotnet build -c Release`, `dotnet test` y `dotnet format --verify-no-changes` pasan sin warnings.
