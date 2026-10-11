# Apply Progress: fase-5-finanzas

**Fecha**: 2026-10-10
**Estado**: Implementación completa (41/41 tareas + 6 TM). Snapshot intermedio: ver `verify-report.md` para la evidencia final.

---

## Progreso por fase

| Fase | Estado | Detalle |
|---|---|---|
| Phase 1 F04 Domain+App | 6/6 | `Update`, comandos, repositorio ampliado, 15 tests |
| Phase 2 F04 Infra+Web | 6/6 | Repo real, página Edit, acciones + borrado, 5 tests |
| Phase 3 F06 entidad+casos | 7/7 | `Budget`, repositorio, 2 handlers, 19 tests |
| Phase 4 F06 persistencia+Web | 5/5 | Migración aplicada, sección presupuestos, estilos, verificación con datos |
| Phase 5 F07 parser+web | 7/7 | Parser, preview, confirm, vistas, 15 tests |
| Phase 6 Verificación | 4/4 | Build 0 warnings, 158 tests, format limpio, DoD revisado |
| Threat Matrix | 6/6 | TM-1..TM-6 verificados |

## Desviaciones respecto a tasks.md

1. **GetTransactionQuery no prevista**: necesaria para la página Edit (espejo de GetCategory). Añadida sin cambiar alcance.
2. **SetBudget upsert**: la spec se enmendó antes de codificar (duplicado actualiza en vez de fallar) para no dejar `ChangeLimit` muerto.
3. **Idempotencia de confirmación**: el MUST se sustituyó por protección PRG antes de codificar.
4. **Errores de subida CSV por TempData+redirect**: el input file no preserva estado; re-renderizar no aporta nada.
5. **Barras con JS**: `data-transactions-percent` + `transactions.js` fijan el ancho (opción que `DESIGN.md` §8.6 deja abierta) porque `style=""` está prohibido.

## Hechos de estado final (para archive)

- Tests finales: 158 (35 dominio, 86 aplicación, 11 arquitectura, 26 integración), todos en verde en Release.
- `dotnet format --verify-no-changes`: limpio.
- Migración `CreateBudget` aplicada en SQL Server local; `Transactions` sin cambios de esquema.
- Sin commits realizados; cambios pendientes de revisión.
