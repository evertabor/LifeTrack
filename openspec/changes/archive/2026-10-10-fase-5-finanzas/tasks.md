# Tasks: fase-5-finanzas

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~1800 (40 archivos en 5 capas + 3 specs) |
| 400-line budget risk | High |
| Chained PRs recommended | Yes — F04, F06, F07 como unidades separadas |
| Delivery strategy | staged-prs (F04 → F06 → F07) |
| Chain strategy | size:exception |

### Suggested Work Units

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|------|------|-----------|----------------------|-----------------|-------------------|
| 1 | F04 editar/eliminar | PR 1 | `dotnet test --filter "Transaction"` | App local + login | Revertir archivos F04 |
| 2 | F06 presupuestos | PR 2 | `dotnet test --filter "Budget"` | App local + mes con gastos | `migrations remove` CreateBudget |
| 3 | F07 CSV | PR 3 | `dotnet test --filter "Import"` | Subida real de CSV | Revertir archivos F07 |

### Breakdown

| Phase | Tasks | Focus |
|-------|-------|-------|
| Phase 1 | 6 | F04 Domain + Application |
| Phase 2 | 6 | F04 Infrastructure + Web + tests |
| Phase 3 | 7 | F06 entidad + casos de uso + tests |
| Phase 4 | 5 | F06 persistencia + Web + estilos |
| Phase 5 | 7 | F07 parser + preview + confirm + tests |
| Phase 6 | 4 | Verificación global |
| Threat Matrix | 6 | RED security tests |
| Total | 41 | |

## Phase 1: F04 Domain + Application

- [ ] 1.1 Añadir `Transaction.Update(categoryId, type, amount, date, description, today)` con RN-F01..RN-F03 y `Id` inmutable
- [ ] 1.2 Crear `UpdateTransactionCommand` + handler (`IRequestHandler<>`) con carga `GetByIdAsync(id, userId)` y traducción de excepciones
- [ ] 1.3 Crear `DeleteTransactionCommand` + handler con `RemoveAsync` + `SaveChangesAsync`
- [ ] 1.4 Ampliar `ITransactionRepository` con `GetByIdAsync` + `RemoveAsync`
- [ ] 1.5 Tests de dominio de `Update`: válido, monto 0, fecha futura, descripción 201, `Id` inalterado
- [ ] 1.6 Tests de handlers: edición válida, id ajeno → `ResourceNotFoundException` + `SaveCount == 0` (update y delete), sin usuario

## Phase 2: F04 Infrastructure + Web + tests

- [ ] 2.1 Implementar `GetByIdAsync` + `RemoveAsync` en `TransactionRepository` (+ fake)
- [ ] 2.2 Crear `EditTransactionInputModel` (nulables + `Range` con `double`) y `TransactionEditViewModel.From`
- [ ] 2.3 Añadir acciones `Edit` GET/POST y `Delete` POST en `TransactionsController` con PRG
- [ ] 2.4 Crear `Views/Transactions/Edit.cshtml` y columna de acciones + modal de borrado en `Index`
- [ ] 2.5 Tests de validación del `InputModel` (regresión `Range`) e integración (anónimo → 302 en nuevas rutas)
- [ ] 2.6 Build + tests F04 en verde

## Phase 3: F06 entidad + casos de uso + tests

- [ ] 3.1 Crear `Budget` con `Create` + `ChangeLimit`, RN-F06, periodo 2000–2100/1–12
- [ ] 3.2 Crear `IBudgetRepository` (`GetAsync`, `ListByPeriodAsync`, `AddAsync`)
- [ ] 3.3 Crear `SetBudgetCommand` + handler con semántica upsert (crea o actualiza el límite; verifica categoría propia)
- [ ] 3.4 Crear `GetBudgetStatusQuery` + DTOs (`Spent`, `Percent`, `State`) + handler con umbrales 80/100
- [ ] 3.5 Tests de dominio de `Budget`: límite 0, 3 decimales, periodo inválido, cambio válido
- [ ] 3.6 Tests de handlers: creación, segunda definición actualiza el límite, inválidos, categoría ajena, estados `ok`/`warning`/`exceeded` exactos, IDOR
- [ ] 3.7 Fakes de `IBudgetRepository` con mismo filtrado por usuario

## Phase 4: F06 persistencia + Web + estilos

- [ ] 4.1 Crear `BudgetConfiguration` (índice único 4 columnas, FK Cascade/Restrict) + `BudgetRepository` + `DbSet` + DI + migración `CreateBudget` file-scoped
- [ ] 4.2 `ViewModels` de presupuesto con textos y barra de `DESIGN.md` §8.6; sección en `Index` + formulario de definición
- [ ] 4.3 Acciones `SetBudget` en controller; estilos de presupuestos en `components.css`
- [ ] 4.4 Aplicar migración en local; verificar avisos 80/100 con datos reales
- [ ] 4.5 Build + tests F06 en verde

## Phase 5: F07 parser + preview + confirm + tests

- [ ] 5.1 Parser propio en Application (líneas, comillas, `;`/`,`, UTF-8, máx 500 filas) sin paquetes
- [ ] 5.2 `PreviewTransactionsQuery(file)` con límites 1 MB/`.csv` y validación por fila vía `Transaction.Create`; categorías por nombre (trim, insensitive)
- [ ] 5.3 `ConfirmTransactionImportCommand` que revalida y guarda solo válidas
- [ ] 5.4 Vistas de subida + preview (válidas/inválidas con error por fila) + confirmación
- [ ] 5.5 Controller `Import`/`Confirm` con validación de archivo (contenido, no MIME)
- [ ] 5.6 Tests: parser (comillas, separadores), preview mixto, confirm solo-válidas, archivo enorme, IDOR por nombre de categoría ajena
- [ ] 5.7 Build + tests F07 en verde

## Phase 6: Verificación global

- [ ] 6.1 `dotnet build -c Release` 0 warnings
- [ ] 6.2 `dotnet test -c Release` todo en verde
- [ ] 6.3 `dotnet format --verify-no-changes` limpio
- [ ] 6.4 Revisar checklist `Definition of Done` de `AGENTS.md` §11 para el change

## Threat Matrix Cases — RED Tests

- [ ] TM-1 **IDOR update/delete**: id ajeno → `ResourceNotFoundException` + `SaveCount == 0` + dato intacto
- [ ] TM-2 **IDOR presupuestos**: categoría ajena en set/status → `ResourceNotFoundException`
- [ ] TM-3 **IDOR importación**: categoría ajena por nombre → fila inválida, nada guardado
- [ ] TM-4 **Anónimo**: nuevas rutas (edit, delete, budgets, import) → 302 a login
- [ ] TM-5 **CSV hostil**: > 1 MB, extensión falsa, 501 filas → rechazo sin parsear ni guardar
- [ ] TM-6 **Sin paquetes nuevos**: `Directory.Packages.props` intacto salvo aprobación explícita

## Implementation Notes

- Orden estricto F04 → F06 → F07; cada unidad termina con build + test + format.
- `Transaction` existente queda cubierto por sus 12 tests durante 1.1.
- Formato CSV propuesto: `fecha;categoria;tipo;importe;descripcion`, UTF-8 (a confirmar pregunta abierta de design.md).
