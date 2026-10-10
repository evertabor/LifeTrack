# Verify Report: fase-2-movimientos

**Fecha**: 2026-10-10
**Change**: fase-2-movimientos
**Artifact Store**: openspec
**Change Root**: `LifeTrack/openspec/changes/fase-2-movimientos`

---

## Status Resumen

| Dimensión | Estado |
|-----------|--------|
| Tareas completadas | 35/35 (Phase 1–6 + TM) |
| Tareas pendientes | 0 |
| Build / Format / Test | PASS (evidencia real abajo) |
| Migración creada y aplicada | Sí (`20261010195226_CreateTransaction` en SQL Server local) |
| Bug encontrado en verificación | 1 (`Range` con cultura, corregido con regresión) |

---

## Hallazgos de Verificación

### BUILD — PASS

```
dotnet build LifeTrack.sln -c Release
```

**Resultado**: 0 errores, 0 warnings.

### FORMAT — PASS

```
dotnet format --verify-no-changes
```

**Resultado**: sin cambios pendientes.

### TESTS — PASS

```
dotnet test LifeTrack.sln -c Release
```

**Resultado**: 100 tests passed
- 22 Domain (`CategoryTests` 10 + `TransactionTests` 12)
- 52 Application (categorías 39 + `CreateTransactionHandlerTests` 5 + `ListMonthlyTransactionsHandlerTests` 7 + identidad 1)
- 11 Architecture (`DependencyRulesTests`, sin modificar)
- 15 Integration (9 previos + `TransactionsPageTests` 2 + `CreateTransactionInputModelValidationTests` 4)

**Nota**: la verificación se ejecutó en Release porque la app corría desde Visual Studio y bloqueaba los DLL de Debug (MSB3021).

---

## Verificación de Requisitos vs. Implementación

### Requisito: Monto válido (RN-F01)

| Verificación | Resultado |
|---|---|
| Monto 0 y negativo lanzan `DomainException` | **PASS** — `TransactionTests` |
| Monto > 1.000.000.000 lanza `DomainException` | **PASS** — `TransactionTests` |
| Monto con 3 decimales lanza `DomainException` | **PASS** — `TransactionTests` |
| `CreateTransactionHandler` con monto 0 → `BusinessRuleException` sin guardar | **PASS** — handler test con `SaveCount == 0` |

### Requisito: Fecha no futura (RN-F02, RN-G01)

| Verificación | Resultado |
|---|---|
| Mañana lanza `DomainException`, hoy pasa (fecha fija 2026-10-10) | **PASS** — `TransactionTests` |
| Handler usa `timeProvider.Today()`, nunca `DateTime.Now` | **PASS** — inspección + `FixedTimeProvider` en tests |

### Requisito: Descripción (RN-F03)

| Verificación | Resultado |
|---|---|
| Trim aplicado, 201 caracteres falla, nulo → vacío | **PASS** — `TransactionTests` |

### Requisito: Solo categorías propias (RN-F05, IDOR)

| Verificación | Resultado |
|---|---|
| Categoría ajena → `ResourceNotFoundException` y nada guardado | **PASS** — TM-1 con `SaveCount == 0` |
| Filtro ajeno en listado → `ResourceNotFoundException` | **PASS** — TM-2 |
| `ListByPeriodAsync` filtra por `userId` y rango | **PASS** — fake y repositorio real con la misma condición |

### Requisito: Resumen mensual (RF-F05, HU-06)

| Verificación | Resultado |
|---|---|
| 1000/400 → balance 600, desglose suma 400 | **PASS** — `ListMonthlyTransactionsHandlerTests` |
| Mes vacío → ceros sin errores | **PASS** — handler test |
| Periodo inválido → `BusinessRuleException` | **PASS** — handler test |

### Requisito: Frontera sin `Domain` en Web

| Verificación | Resultado |
|---|---|
| `Controllers_*` no dependen de `LifeTrack.Domain` | **PASS** — 11 tests de arquitectura en verde sin tocar |
| `*ViewModel` no dependen de `LifeTrack.Domain` | **PASS** — ídem |

### Requisito: Auth y vistas

| Verificación | Resultado |
|---|---|
| `GET/POST /transactions` anónimo → 302 a `/Account/Login` | **PASS** — `TransactionsPageTests` |
| `[Authorize]` en controller + fail-closed sin usuario | **PASS** — inspección + tests |
| Sin `Html.Raw`, sin ternarios, `<label>` por campo | **PASS** — inspección de ambas vistas |
| `ValidateAntiForgeryToken` en POST | **PASS** — atributo explícito + filtro global verificado por arquitectura |
| Enlace en sidebar + `DESIGN.md` actualizado | **PASS** — inspección |

---

## Bug Encontrado y Corregido

**`ArgumentException: '0.01 is not a valid value for Decimal'`** al validar el formulario bajo cultura es-ES. Causa: `[Range(typeof(decimal), "0.01", ...)]` parsea el literal con `DecimalConverter` en la cultura actual. Corregido con `[Range(0.01, 1000000000)]` (sobrecarga `double`, sin parseo) y cubierto con `CreateTransactionInputModelValidationTests` (4 tests). Regla permanente: nunca `Range(typeof(decimal))` con literales con punto decimal.

---

## Desviaciones de Diseño

| Elemento | Desviación | Impacto |
|---|---|---|
| Frontera con `bool IsIncome` en vez del enum | El ejemplo de `docs/referencias/application.md` expone el enum; la implementación usa primitivos por los tests de arquitectura | Ninguno funcional; documentado en design.md D1 |
| Migración aplicada en local | El precedente de categorías la dejaba sin aplicar; aquí se aplicó a petición del usuario | La tabla existe en el SQL Server local |

No se encontraron otras desviaciones de `design.md` ni de `AGENTS.md`.

---

## Limitaciones de la Verificación

1. **Render autenticado no probado de extremo a extremo** — solo el redirect anónimo tiene test; falta un test con sesión y datos (requiere factory con conexión SQLite compartida).
2. **Sin prueba manual en navegador** — la app local corría un build anterior desde Visual Studio durante la verificación.
3. **RF-F04, RF-F06, RF-F07** fuera de alcance por decisión de alcance "solo núcleo M", no gaps de este change.

---

## Riesgos Registrados

| Riesgo | Probabilidad | Estado |
|---|---|---|
| Brecha IDOR por manipulación de ids | Baja | **Mitigado**: verificación por `GetByIdAsync(id, userId)` + TM-1/TM-2 |
| Acceso sin autenticación | Baja | **Mitigado**: `[Authorize]` + fallback + TM-3 con tests |
| Regresión del bug de cultura en otro `InputModel` | Media | **Parcialmente mitigado**: regla documentada; sin barrido global realizado |
| Formato de importes incorrecto en otra cultura | Baja | Formato `N2` es-ES explícito en el `ViewModel`, independiente del servidor |

---

## Estado Consolidado

| Categoría | Total | Completadas | Pendientes |
|-----------|-------|-------------|------------|
| Tareas (Phase 1–6 + TM) | 35 + 4 TM | 35 + 4 TM | 0 |

---

## Nota sobre Archivo

Este reporte no certifica la funcionalidad como producción-ready ni sustituye una revisión de código. Es un diagnóstico honesto del estado actual. Con 35/35 tareas completadas y evidencia real, el change es archivable.
