# Exploration: fase-5-finanzas

**Fecha**: 2026-10-10
**Change**: fase-5-finanzas
**Alcance funcional**: RF-F04 (S), RF-F06 (S), RF-F07 (C) de `docs/01-especificacion.md`

---

## 1. Punto de partida

El módulo de finanzas cubre el núcleo M (categorías + registro + listado + resumen). Verificado con búsqueda en `src/`: no existe ningún tipo `Budget`, `EditTransaction`, `DeleteTransaction`, ni nada de CSV/importación (el único match de "Import" es `using System.ComponentModel.DataAnnotations`). Las tres features parten de cero salvo lo que reutilizan.

## 2. Hallazgos

### 2.1 RF-F04 — Editar y eliminar movimiento

`Transaction` no tiene métodos de edición: todas sus propiedades son `private set` sin mutadores. `ITransactionRepository` solo ofrece `AddAsync` + `ListByPeriodAsync`: faltan `GetByIdAsync(userId)` y `RemoveAsync`. `TransactionsController` solo tiene `Index` + `Create`; la tabla de la vista no tiene columna de acciones (a diferencia de categorías, que sí la tiene con modales por fila). El patrón a replicar es `EditCategory`/`DeleteCategory` (verificación por `GetByIdAsync(id, userId)`, `ResourceNotFoundException` si es ajeno, PRG).

### 2.2 RF-F06 — Presupuestos

No existe nada. RN-F06 exige unicidad por usuario + categoría + año + mes con límite > 0 y 2 decimales; HU-07 exige avisos al 80 % y al 100 % del consumo. El patrón a replicar es `CategoryConfiguration` (índice único) y `ListMonthlyTransactionsHandler` (cálculo de gasto por categoría ya existe y se puede reutilizar para el consumo). La vista de movimientos ya muestra gasto por categoría: el aviso de presupuesto encaja como extensión de esa sección. `DESIGN.md` §8.6 define barra de presupuesto con estados y exige texto siempre, sin presumir iconos.

### 2.3 RF-F07 — Importación CSV

No existe nada. Requiere subida de archivo (nuevo riesgo: tamaño, tipo MIME, codificación), parseo, validación fila a fila con las mismas invariantes RN-F01..F05, vista previa antes de confirmar y confirmación en dos pasos (previsualizar → importar). Los paquetes NuGet están prohibidos sin aprobación: el parseo debe hacerse con código propio (`string.Split` con comillas o `TextFieldParser` no disponible en .NET Core; `Microsoft.VisualBasic.FileIO` sí forma parte del runtime compartido pero introduce dependencia ajena al dominio web). Decisión diferida a proposal/design.

### 2.4 Límites transversales ya verificados

- `ICurrentUser.UserId` es `Guid?`: fail-closed con `BusinessRuleException`.
- Frontera Web sin `Domain`: comandos y DTOs con primitivos (`bool IsIncome`).
- `Range` con `typeof(decimal)` prohibido (bug de cultura documentado): usar sobrecarga con `double`.
- Vistas sin lógica; `ViewModel.From(...)`; formularios con `InputModel` bajo `ViewModels/<Area>/`; antiforgery global; JS con IIFE y bloques de constantes.
- Sin paquetes NuGet nuevos sin aprobación explícita.

## 3. Opciones

| Enfoque | Pros | Contras | Esfuerzo |
|---|---|---|---|
| A. Edición como métodos `Update(...)` en `Transaction` que reutilizan las validaciones de `Create` | Invariantes en un solo sitio; testable en dominio | Toca la entidad existente (riesgo bajo, tests la cubren) | Bajo |
| B. Edición reconstruyendo la entidad (borrar + crear) | No toca `Transaction` | Cambia el `Id`, rompe trazabilidad y FK futuras; dos operaciones no atómicas | Medio |
| C. Presupuesto como entidad `Budget` propia con índice único (`UserId`, `CategoryId`, `Year`, `Month`) | Replica el patrón `Category`; unicidad garantizada en BD | Tabla y migración nuevas | Medio |
| D. Presupuesto como campo calculado sin entidad | Sin migración | Sin persistencia no hay "definir presupuesto"; viola RF-F06 | N/A (inválido) |
| E. CSV con parseo propio en Application + subida en Web con límites | Sin paquetes; validación reutiliza `Transaction.Create` | Más código propio de parseo (comillas, líneas) | Medio |
| F. CSV con paquete (p. ej. CsvHelper) | Menos código | Requiere aprobación + auditoría de cadena de suministro (A03) | Bajo/Medio |

## 4. Recomendación

A + C + E: edición con mutadores de dominio, `Budget` como entidad con índice único, y CSV con parseo propio en dos pasos (vista previa → confirmación). Sin paquetes nuevos. Orden de construcción: F04 → F06 → F07 (de menor a mayor riesgo).

## 5. Riesgos

- Subida de CSV: archivos maliciosos o enormes (tamaño máximo, validación de contenido, nunca confiar en el MIME del cliente).
- Condición de carrera en unicidad de presupuesto: cubierta por índice único, como en categorías.
- `Transaction` gana mutadores: los tests de dominio existentes deben seguir verdes.
- Alcance: F07 es C; si complica el ciclo, se difiere sin bloquear F04/F06.

## 6. Listo para propuesta

Sí. Sin bloqueos ni preguntas abiertas que impidan proponer.
