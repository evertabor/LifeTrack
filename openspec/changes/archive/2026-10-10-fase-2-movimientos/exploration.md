# Exploration: fase-2-movimientos

> Nota: documento redactado a posteriori. Describe lo encontrado y lo construido.

**Fecha**: 2026-10-10
**Change**: fase-2-movimientos
**Alcance funcional**: RF-F02, RF-F03, RF-F05, HU-05, HU-06 (requisitos M del módulo de finanzas)

---

## 1. Punto de partida

La fase-2-categorias dejó el módulo de finanzas a medias: solo existe el catálogo (`Category`), sin ningún movimiento. La especificación (`docs/01-especificacion.md` §12) exige para cerrar la Fase 2 el registro de movimientos, el listado mensual con filtro y el resumen mensual. El usuario confirmó alcance "solo núcleo M": RF-F04 (editar/eliminar), RF-F06 (presupuestos) y RF-F07 (CSV) quedan fuera.

## 2. Hallazgos

### 2.1 La entidad no existe

En `src/LifeTrack.Domain/` solo había `Finance/Category.cs`. No existía `Transaction`, ni `TransactionType`, ni `Guard`. El documento `docs/referencias/domain.md` propone un `Transaction.Create(...)` canónico con parámetro `today` inyectado y un `Guard` interno, pero `Category` no usa `Guard`: valida en línea. Se siguió el estilo de `Category` (validación en línea, sin `Guard`) para no introducir un segundo estilo en la misma carpeta.

### 2.2 `ICurrentUser.UserId` es nulable

`docs/referencias/application.md` muestra `ICurrentUser` con `Guid` no nulable que lanza al no haber identidad. El código real declara `Guid? UserId`. Los handlers existentes comprueban `is null` y lanzan `BusinessRuleException`. Los handlers nuevos siguen el código real, no la referencia.

### 2.3 No existe `TimeProviderExtensions`

La referencia propone `Common/TimeProviderExtensions.cs` con `Today()`, pero el archivo no existía en el repositorio. La regla RN-G01 ("hoy" en la zona `App:TimeZoneId`) obliga a no usar `DateTime.Now`. Se creó la extensión.

### 2.4 La frontera Web no puede tocar el enum de dominio

`TransactionType` vive en `Domain`. Los tests `Controllers_TalkToApplication_NotToInfrastructureOrDomain` y `ViewModels_DoNotInheritFromDomainEntities` (este último prohíbe cualquier dependencia de `Domain` en tipos `*ViewModel`) impiden que el controller, el `InputModel` o los `ViewModels` usen el enum. El propio ejemplo de `docs/referencias/application.md` expone el enum en el DTO, lo que filtraría la dependencia a Web. La frontera construida usa primitivos: `bool IsIncome` en el comando y `IsIncome` + `TypeLabel` precalculados en el DTO de lectura.

### 2.5 `Range(typeof(decimal), ...)` es frágil ante la cultura

Hallazgo posterior a la implementación inicial (ver `apply-progress.md`): `[Range(typeof(decimal), "0.01", ...)]` convierte el texto con la cultura del servidor y en es-ES lanza `FormatException` dentro de `DecimalConverter`. La sobrecarga con `double` no parsea texto y es segura.

### 2.6 Vistas y navegación

Existía el esqueleto `Views/Transactions/` (`_ViewStart` con layout autenticado, carpeta `Partials`, `.gitkeep`) y `ViewModels/Transactions/` con `.gitkeep`. La clase `app-card` que propone `DESIGN.md` no existe en el CSS: se espejó el patrón `categories-*` ya aprobado. El sidebar solo enlazaba `Categories`; se añadió el enlace `Movimientos` que `DESIGN.md` ordena al existir la pantalla.

## 3. Decisiones derivadas

| # | Decisión | Origen |
|---|---|---|
| D1 | Validación en línea en `Transaction`, sin crear `Guard` | 2.1 |
| D2 | Handlers comprueban `currentUser.UserId is null` | 2.2 |
| D3 | Crear `TimeProviderExtensions.Today()` | 2.3 |
| D4 | Frontera con primitivos (`bool IsIncome`, `TypeLabel`) | 2.4 |
| D5 | `Range` con `double`, nunca `Range(typeof(decimal), ...)` con decimales | 2.5 |
| D6 | Estilos `transactions-*` espejando `categories-*`; enlace en sidebar | 2.6 |
