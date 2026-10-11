# Design: fase-5-finanzas

## Enfoque Técnico

Tres rebanadas en orden F04 → F06 → F07 sobre la base de fase-2-movimientos. F04 añade el mutador `Transaction.Update(...)` y reutiliza verificación de propiedad y PRG de categorías. F06 introduce la entidad `Budget` con índice único y reutiliza `ListByPeriodAsync` para el consumo. F07 añade subida con límites, parseo propio en dos pasos y validación por fila con `Transaction.Create`. Toda la frontera sigue con primitivos y las vistas sin lógica.

## Decisiones de Arquitectura

### Decisión: Mutador `Update` en la entidad (opción A)

**Elección**: `Transaction.Update(categoryId, type, amount, date, description, today)` aplica RN-F01..RN-F03; la categoría se valida en el handler contra `ICategoryRepository` (RN-F05), igual que en creación.

**Alternativas consideradas**:
- Reconstruir (borrar + crear): rechazado porque cambia el `Id` y duplica operaciones.
- Validar solo en el handler: rechazado porque duplica invariantes fuera del dominio.

**Justificación**: una sola fuente de invariantes; los 12 tests de dominio existentes cubren la regresión.

### Decisión: `UpdateTransaction` sin resultado, `DeleteTransaction` sin resultado

**Elección**: ambos implementan `IRequestHandler<TRequest>` (MediatR v12+: `Task`, no `Task<Unit>`), como `EditCategory`/`DeleteCategory`.

**Justificación**: coherencia con el patrón existente de escrituras sin resultado.

### Decisión: `Budget` como entidad con índice único (opción C)

**Elección**: `Budget.Create(userId, categoryId, year, month, limit)` + `ChangeLimit(limit)`; índice único (`UserId`, `CategoryId`, `Year`, `Month`); año 2000–2100 y mes 1–12 validados en dominio.

**Alternativas consideradas**:
- Sin entidad (opción D): inválida, RF-F06 exige persistir el límite.

**Justificación**: replica `Category`/`CategoryConfiguration`; la BD garantiza RN-F06 ante carreras. `SetBudget` tiene semántica upsert (segunda definición actualiza vía `ChangeLimit`) para no dejar código muerto y porque redefinir equivale a corregir.

### Decisión: Estado de presupuesto calculado en Application, pintado en Web

**Elección**: `GetBudgetStatusHandler` devuelve `Spent`, `Percent` y `State` (`ok`/`warning`/`exceeded`, umbrales 80/100); el `ViewModel` prepara textos y la barra de `DESIGN.md` §8.6.

**Justificación**: el cálculo es fallo silencioso (número visible) y lleva test exacto; la vista solo renderiza.

### Decisión: CSV con parseo propio en dos pasos (opción E)

**Elección**: parser propio (líneas, comillas, `;` o `,`), límites 1 MB / 500 filas, preview sin persistir y confirmación que revalida. Sin paquetes nuevos (A03).

**Alternativas consideradas**:
- Paquete CsvHelper (opción F): rechazado salvo aprobación explícita; no se solicita.
- Importación directa en un paso: rechazado porque RF-F07 exige vista previa.

**Justificación**: la validación por fila reutiliza `Transaction.Create`, así que el parseo es la única lógica nueva real.

### Decisión: `Range` con `double` en todo `InputModel` con importes

**Elección**: prohibición extendida de `Range(typeof(decimal))` con decimales (bug documentado en fase-2-movimientos) más test de regresión por formulario.

## Flujo de Datos

```
Editar:
Navegador (GET /transactions/{id}/edit)
    ▼ TransactionsController.Edit → GetTransactionQuery → EditViewModel.From
Navegador (POST /transactions/{id}/edit)
    ▼ sender.Send(UpdateTransactionCommand) → carga GetByIdAsync(id, userId)
      → Update(...) → SaveChanges → Redirect Index (mes del movimiento)

Eliminar:
Navegador (POST /transactions/{id}/delete, modal en Index)
    ▼ sender.Send(DeleteTransactionCommand) → carga → RemoveAsync → SaveChanges

Presupuesto:
Navegador (POST /transactions/budgets) → SetBudgetCommand → verifica categoría
    → Create/ChangeLimit → SaveChanges
Index incluye GetBudgetStatusQuery(year, month) → estados por categoría

Importar:
Navegador (POST /transactions/import, IFormFile)
    ▼ PreviewTransactionsQuery(file) → parseo + validación por fila (sin guardar)
    ▼ vista ImportPreview con válidas/inválidas
Navegador (POST /transactions/import/confirm)
    ▼ ConfirmTransactionImportCommand(rows) → revalida → AddAsync + SaveChanges
```

## Cambios en Archivos

| Archivo | Acción | Descripción |
|---|---|---|
| `Domain/Finance/Transaction.cs` | Modificar | Mutador `Update(...)` |
| `Domain/Finance/Budget.cs` | Crear | Entidad con RN-F06 |
| `Application/Abstractions/ITransactionRepository.cs` | Modificar | `GetByIdAsync`, `RemoveAsync` |
| `Application/Abstractions/IBudgetRepository.cs` | Crear | `GetAsync`, `ListByPeriodAsync`, `AddAsync` |
| `Finance/Commands/UpdateTransaction/` | Crear | Comando + handler (`IRequestHandler<>`) |
| `Finance/Commands/DeleteTransaction/` | Crear | Comando + handler |
| `Finance/Commands/SetBudget/` | Crear | Comando + handler |
| `Finance/Commands/GetBudgetStatus/` | Crear | Query + DTOs + handler |
| `Finance/Commands/ImportTransactions/` | Crear | Preview query + confirm command + parser propio |
| `Infrastructure/.../BudgetConfiguration.cs` | Crear | Tabla, índice único, FK |
| `Infrastructure/.../BudgetRepository.cs` | Crear | Filtro por usuario |
| `Infrastructure/.../TransactionRepository.cs` | Modificar | `GetByIdAsync`, `RemoveAsync` |
| `AppDbContext.cs`, `DependencyInjection.cs` | Modificar | `DbSet<Budget>` + registro |
| Migración `CreateBudget` | Crear | Tabla e índices (file-scoped) |
| `TransactionsController.cs` | Modificar | Edit, Delete, Import, Confirm |
| `ViewModels/Transactions/` | New/Modified | `EditTransactionInputModel`, presupuestos, preview |
| `Views/Transactions/Edit.cshtml` | Crear | Página de edición |
| `Views/Transactions/Index.cshtml` | Modificar | Acciones por fila, sección presupuestos |
| `Views/Transactions/Import*.cshtml` | Crear | Subida + preview |
| `components.css`, `transactions.js` | Modificar | Barras de presupuesto, preview |
| Tests | Crear | Dominio (Update, Budget), aplicación con IDOR, validación, integración |

## Estrategia de Pruebas

| Capa | Qué probar | Enfoque |
|---|---|---|
| Domain | `Update` revalida (monto 0, futura, 201 chars); `Budget` (límite 0, duplicado no aplica en dominio, periodo inválido) | Tests con fecha fija |
| Application | Update/Delete con id ajeno → `ResourceNotFoundException` + `SaveCount == 0`; presupuesto duplicado; consumo 80/100; preview marca inválidas; confirm solo guarda válidas | Handlers con fakes |
| Web/InputModel | `EditTransactionInputModel` válido/inválido (regresión `Range`) | `Validator.TryValidateObject` |
| Architecture | Sin cambios de reglas; sin dependencia Web→Domain nueva | Suite intacta |
| Integration | Anónimo en nuevas rutas → 302; subida > 1 MB → 400/redirect con error | Factory + SQLite |

## Threat Matrix

N/A separada: IDOR (TM), anónimo y subida maliciosa van como casos en tasks.md.

## Migración y Despliegue

1. `dotnet ef migrations add CreateBudget` (mismo par de proyectos, file-scoped).
2. Aplicar en local con `database update`; en producción el pipeline.
3. F04/F07 no requieren cambios de esquema.

## Preguntas Abiertas

- [ ] Formato exacto del CSV (columnas, separador, codificación): la spec asume `fecha;categoria;tipo;importe;descripcion` con `;` o `,` y UTF-8; confirmar con el usuario antes de apply si difiere.
- [ ] ¿Enlace propio a importación o integrada en `/transactions`? Propuesto: sección en `Index` + rutas `/transactions/import`.
