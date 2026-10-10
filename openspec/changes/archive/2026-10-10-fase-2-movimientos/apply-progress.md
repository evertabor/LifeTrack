# Apply Progress: fase-2-movimientos

**Fecha**: 2026-10-10
**Estado**: Implementación completa (35/35 tareas). Snapshot intermedio: ver `verify-report.md` para la evidencia final.

---

## Progreso por fase

| Fase | Estado | Detalle |
|---|---|---|
| Phase 1 Domain | 4/4 | `TransactionType`, `Transaction`, 12 tests en verde al primer intento |
| Phase 2 Application | 7/7 | Interfaces, extensión de reloj, 2 casos de uso, fakes, 12 tests en verde |
| Phase 3 Infrastructure | 4/4 | Configuración, repositorio, `DbSet` + DI, migración creada (con fix de namespace) |
| Phase 4 Web | 8/8 | `InputModel`, 2 `ViewModels`, controller, 2 vistas, JS, sidebar, `DESIGN.md` |
| Phase 5 Testing | 5/5 | Build 0 warnings, 100 tests, format limpio, regresión + integración |
| Phase 6 Migration/Styles/Bugfix | 3/3 | Migración aplicada, estilos, fix de `Range` |
| Threat Matrix | 4/4 | TM-1..TM-3 verificados con tests; TM-4 N/A documentado |

## Desviaciones respecto al plan inicial

1. **Frontera con primitivos (no estaba en proposal)**: al compilar se detectó que exponer `TransactionType` en el comando/DTO rompería los tests de arquitectura. Se cambió a `bool IsIncome` + `TypeLabel` antes de escribir los tests. Sin impacto en alcance.
2. **Bug de `Range` con cultura (no estaba en proposal)**: la vista falló en tiempo de ejecución con `ArgumentException` bajo es-ES. Se corrigió a sobrecarga con `double` y se añadió test de regresión. Ver `verify-report.md`.
3. **`app-card` inexistente**: las vistas iniciales usaban `app-card` de `DESIGN.md`, clase que no existe en el CSS. Se reescribieron con clases `transactions-*` espejando categorías. Sin impacto funcional.
4. **Migración aplicada en local**: a diferencia del precedente de categorías, se ejecutó `database update` a petición del usuario.

## Hechos de estado final (para archive)

- Tests finales: 100 (22 dominio, 52 aplicación, 11 arquitectura, 15 integración), todos en verde en Release.
- `dotnet format --verify-no-changes`: limpio.
- Migración `20261010195226_CreateTransaction`: aplicada en SQL Server local.
- Sin commits realizados; cambios pendientes de revisión.
