# Proposal: fase-5-finanzas

## Intent

Completar el módulo de finanzas con el valor añadido de Fase 5: editar y eliminar movimientos (RF-F04, S), presupuestos mensuales por categoría con avisos al 80 % y 100 % (RF-F06, S, RN-F06, HU-07) e importación CSV con vista previa (RF-F07, C). Orden de construcción: F04 → F06 → F07.

## Scope

### In Scope

- `Transaction.Update(...)` en dominio reutilizando invariantes RN-F01..RN-F03; `UpdateTransaction` y `DeleteTransaction` con verificación de propiedad
- `GetByIdAsync` + `RemoveAsync` en `ITransactionRepository`; columna de acciones en la tabla de movimientos (editar vía página `Edit`, eliminar con modal de confirmación)
- Entidad `Budget` con unicidad (`UserId`, `CategoryId`, `Year`, `Month`); `SetBudget` y `GetBudgetStatus`; avisos 80 %/100 % en la vista de movimientos; migración
- Importación CSV en dos pasos (subir + previsualizar → confirmar): parseo propio sin paquetes, validación por fila con `Transaction.Create`, límites de tamaño y tipo
- Tests de dominio, aplicación (IDOR), validación e integración siguiendo el criterio de riesgo

### Out of Scope

- Borrado de cuenta (RF-A04)
- Despliegue (Fase 4)
- Librería de gráficos o iconos (pendientes en `DESIGN.md`)

## Capabilities

### New Capabilities

- `transaction-editing`: Editar (categoría propia, monto, fecha no futura, descripción) y eliminar movimientos propios. Un id ajeno o inexistente responde "no encontrado" sin modificar nada.
- `budget-management`: Definir un presupuesto mensual por categoría (límite > 0, 2 decimales, único por periodo) y ver su consumo con aviso al 80 % y de superado al 100 %.
- `transaction-import`: Subir un CSV, previsualizar filas válidas e inválidas con errores por fila, y confirmar la importación solo de las válidas.

### Modified Capabilities

- `transaction-management`: añade `GetByIdAsync`/`RemoveAsync` al repositorio y acciones por fila en la vista; no cambia contratos existentes.

## Approach

1. **F04**: mutador `Transaction.Update(categoryId, type, amount, date, description, today)` que aplica las mismas validaciones que `Create` (sin cambiar el `Id`). Handlers `UpdateTransaction` (comando con resultado void vía `IRequest`) y `DeleteTransaction`, ambos con carga por `GetByIdAsync(id, userId)` y traducción de excepciones. Controller con `GET edit` + `POST edit` (página `Edit.cshtml`, como categorías) y `POST delete` con modal de confirmación en `Index`.
2. **F06**: entidad `Budget` con fábrica `SetLimit`-like (`Create(userId, categoryId, year, month, limit)` + `ChangeLimit`), índice único en las cuatro columnas, `Year`/`Month` validados (2000–2100, 1–12). `SetBudgetHandler` verifica propiedad de la categoría; `GetBudgetStatusHandler` calcula gastado del mes reutilizando el rango de `ListByPeriodAsync` y devuelve porcentaje y estado (`ok`/`warning`/`exceeded`). La vista pinta la barra de `DESIGN.md` §8.6 con textos preparados en el `ViewModel`.
3. **F07**: `POST import` recibe `IFormFile` (límite 1 MB, extensión `.csv`, validación de contenido no de MIME), parsea con código propio (separador `;` o `,`, comillas, máximo 500 filas), valida cada fila con `Transaction.Create` y muestra vista previa con filas válidas/inválidas; `POST import/confirm` persiste solo las válidas revalidadas. Nada se guarda antes de confirmar.

## Affected Areas

| Area | Impact | Description |
|---|---|---|
| `src/LifeTrack.Domain/Finance/Transaction.cs` | Modified | Mutador `Update(...)` con RN-F01..RN-F03 |
| `src/LifeTrack.Domain/Finance/Budget.cs` | New | Entidad con RN-F06 |
| `src/LifeTrack.Application/Abstractions/ITransactionRepository.cs` | Modified | `GetByIdAsync`, `RemoveAsync` |
| `src/LifeTrack.Application/Abstractions/IBudgetRepository.cs` | New | `GetAsync`, `ListByPeriodAsync`, `AddAsync` |
| `src/LifeTrack.Application/Finance/Commands/UpdateTransaction/` | New | Comando + handler |
| `src/LifeTrack.Application/Finance/Commands/DeleteTransaction/` | New | Comando + handler |
| `src/LifeTrack.Application/Finance/Commands/SetBudget/` | New | Comando + handler |
| `src/LifeTrack.Application/Finance/Commands/GetBudgetStatus/` | New | Query + handler + DTOs |
| `src/LifeTrack.Application/Finance/Commands/ImportTransactions/` | New | Queries de preview + comando de confirmación |
| `src/LifeTrack.Infrastructure/.../BudgetConfiguration.cs` | New | Tabla, índice único, FK |
| `src/LifeTrack.Infrastructure/.../Repositories/` | New/Modified | `BudgetRepository`, `GetByIdAsync`/`RemoveAsync` en transacciones |
| Migraciones EF | New | `CreateBudget` (transacciones no requieren cambio de esquema) |
| `TransactionsController.cs` | Modified | Edit, Delete, Import, Confirm |
| `ViewModels/Transactions/` | New/Modified | `EditTransactionInputModel`, estados de presupuesto, modelos de preview |
| `Views/Transactions/` | New/Modified | `Edit.cshtml`, acciones por fila, sección de presupuestos, vistas de importación |
| `components.css`, `transactions.js` | Modified | Estilos de presupuestos/importación; JS de preview si aplica |
| Sidebar | Evaluar | Enlace a importación si aporta (misma área, puede vivir en `/transactions`) |
| Tests (4 proyectos) | New | Dominio, aplicación con IDOR, validación, integración |

## Risks

| Risk | Likelihood | Mitigation |
|---|---|---|
| CSV malicioso o enorme | Medium | Límite 1 MB y 500 filas, validación de contenido, nada se guarda antes de confirmar |
| Mutadores en `Transaction` rompen tests existentes | Low | Tests de dominio actuales en verde antes y después; invariantes compartidas |
| Unicidad de presupuesto en carrera | Low | Índice único como en categorías |
| `Range` con cultura en nuevos `InputModels` | Medium | Solo sobrecarga con `double`; test de regresión por formulario con importes |
| F07 desborda el ciclo (es C) | Medium | F04 y F06 cierran primero; F07 se puede archivar como diferido sin bloquear |

## Rollback Plan

1. `dotnet ef database update <previa>` + `migrations remove` para `CreateBudget`.
2. Eliminar archivos New; revertir Modified (`Transaction.cs`, repositorios, controller, vistas, CSS, JS).
3. `dotnet build -c Release` + `dotnet test` en verde.

## Dependencies

- `ICategoryRepository` para verificar propiedad en edición, presupuestos e importación.
- `ListByPeriodAsync` existente para el cálculo de consumo.
- Contenedor SQL Server local; secretos en user-secrets; MediatR pineado.

## Success Criteria

- [ ] Editar con monto 0 o fecha futura falla con mensaje claro y no guarda.
- [ ] Editar/eliminar con id ajeno responde "no encontrado" y no modifica nada (IDOR con `SaveCount == 0`).
- [ ] Presupuesto duplicado (mismo periodo) se rechaza; gasto 80/100 dispara aviso/superado (HU-07).
- [ ] CSV con filas inválidas muestra preview con errores por fila y no guarda nada hasta confirmar.
- [ ] Ninguna dependencia Web→Domain nueva; arquitectura en verde.
- [ ] `dotnet build -c Release`, `dotnet test`, `dotnet format --verify-no-changes` sin warnings.
