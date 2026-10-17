# LifeTrack — Sistema visual

> Este documento define el **diseño visual objetivo** de LifeTrack. No afirma que las pantallas o los componentes descritos ya estén implementados. Antes de crear o cambiar una vista, comprueba el alcance funcional y las convenciones del proyecto. Si una decisión visual no está definida aquí o en una fuente de mayor prioridad, no la inventes: reutiliza lo existente o pregunta.

**Estado:** shell visual autenticada y pantalla de categorías aprobadas como composición de dashboard; las pantallas funcionales restantes siguen al final.

**Base técnica comprobada:** ASP.NET Core MVC con Razor; Bootstrap **5.3.3** estático y local en `wwwroot/lib`; CSS propio con variables. El JavaScript nuevo debe seguir `docs/08-estandar-javascript.md`.

**Idioma:** identificadores, clases CSS y código en inglés. Textos visibles y documentación en español.

### Estado de implementación comprobado

- Existen layouts separados: `_Layout.cshtml` para páginas públicas y `_AuthenticatedLayout.cshtml` para áreas autenticadas. `_ViewStart.cshtml` conserva el layout público por defecto y los `_ViewStart.cshtml` anidados seleccionan el shell autenticado.
- El shell autenticado incluye skip link, navegación lateral fija en escritorio, cajón móvil, topbar, contenido principal y pie. Sus bloques viven en `Views/Shared/Partials/`.
- La navegación enlaza a `Transactions` y `Categories`, las áreas que disponen actualmente de controller y vista. Dashboard permanece fuera de la navegación hasta que su pantalla exista.
- `tokens.css`, `bootstrap-overrides.css`, `components.css` y `site.css` existen y se cargan después de Bootstrap. No se han añadido assets externos, fuentes, iconos ni paquetes.
- `site.js` contiene el comportamiento accesible del cajón móvil y el selector persistente de tema; Bootstrap y jQuery siguen siendo assets locales existentes.
- La especificación establece una sola moneda y formato numérico de dos decimales, pero no identifica el símbolo. Los ejemplos de importes de esta guía no fijan una moneda ni un símbolo.

Actualiza este resumen cuando la interfaz implementada cambie de forma significativa.

---

## 0. Cómo usar este documento

### Alcance y precedencia

Aplica cada fuente en su ámbito; este documento solo decide aspectos visuales que no contradicen las fuentes siguientes:

1. `docs/01-especificacion.md` define el alcance funcional: qué pantallas y capacidades se pueden construir.
2. `AGENTS.md` de LifeTrack y de la capa correspondiente define las reglas obligatorias del proyecto.
3. `docs/09-estandar-vistas-mvc.md` define la estructura y las reglas de las vistas; `docs/08-estandar-javascript.md` define JavaScript.
4. Este documento define la apariencia visual objetivo.

Si dos fuentes aplicables entran en conflicto, no uses esta lista para ignorar una regla del proyecto: detente y aclara el conflicto antes de implementar.

### Flujo para crear o cambiar una pantalla

1. Comprueba en `docs/01-especificacion.md` que la pantalla y la capacidad estén dentro del alcance.
2. Reutiliza los componentes de la sección 8 cuando cubran la necesidad.
3. Usa tokens de la sección 3; no introduzcas colores, espaciados, radios o sombras sueltos.
4. Recibe en el `ViewModel` los valores, textos y estados ya preparados para renderizar. La vista no los calcula ni los formatea.
5. Define los estados aplicables de carga, vacío, error y éxito, según la sección 10.
6. Comprueba la lista de la sección 14 y las normas de `docs/09-estandar-vistas-mvc.md` y `docs/08-estandar-javascript.md`.

---

## 1. Principios de diseño

| Principio | Qué significa en la práctica |
|---|---|
| Claro y fiable | El dinero, los presupuestos y la actividad se muestran de forma serena y predecible. |
| Enfocado, no saturado | Los valores importantes tienen espacio. Nada decorativo compite con los datos. |
| Alentador, nunca culpabilizador | Se muestra el progreso y el siguiente paso útil. Un gasto alto no se presenta como fracaso. |
| Consistente | Mismos colores, espaciados, tipografía e interacciones en todas las áreas. |
| Accesible de serie | Teclado, táctil, lector de pantalla y contraste suficiente desde el primer cambio de interfaz. |

**Identidad:** el azul representa estabilidad, confianza y control financiero. El naranja representa acción, energía y avance. El naranja es un acento: se usa de forma controlada y nunca domina la interfaz.

---

## 2. Reglas rápidas

### Haz

- Usa variables CSS `--app-*` para colores, espaciados, radios y sombras.
- Usa `app-*` para componentes propios compartidos y el prefijo de la funcionalidad (`dashboard-*`, `categories-*`, `transactions-*`) para clases y atributos `data-*` específicos de una pantalla.
- Usa utilidades de Bootstrap para espaciado, `display`, flex y grid, sin cambiar la misma propiedad con una regla propia en el mismo elemento.
- Acompaña todo estado (éxito, aviso, error, información) con texto o icono, además del color.
- Usa `font-variant-numeric: tabular-nums` y alineación a la derecha en importes.
- Escribe los textos visibles en español, con mayúscula inicial y el resto en minúsculas salvo nombres propios.
- Respeta `prefers-reduced-motion`.

### No hagas

- No escribas valores de color hexadecimales en el CSS propio fuera de `wwwroot/css/tokens.css`.
- No uses `style="..."` ni bloques `<style>` en vistas Razor.
- No uses `!important`, salvo en el bloque de movimiento reducido de la sección 12.
- No uses naranja para errores, advertencias ni presupuestos superados.
- No comuniques un estado solo con color.
- No añadas paquetes, fuentes remotas, frameworks CSS, librerías de iconos ni de gráficos sin aprobación explícita.
- No pongas lógica de formato ni de negocio en la vista: fechas, importes, porcentajes y estados llegan preparados desde el `ViewModel`.
- No enganches JavaScript a clases de estilo. Sigue los bloques de constantes de `docs/08-estandar-javascript.md` para selectores, clases y claves `data-*`.
- No derives el tema de `prefers-color-scheme`: el valor inicial es claro y solo cambia por la elección persistida del usuario.

---

## 3. Tokens

La ubicación objetivo de los tokens es `src/LifeTrack.Web/wwwroot/css/tokens.css`. El siguiente bloque es el contenido propuesto para ese archivo; no copies sus valores de color a otros archivos CSS. En el código del proyecto, `tokens.css` será el único archivo que contendrá valores hexadecimales.

```css
:root {
  color-scheme: light;

  --app-color-primary-900: #102A43;
  --app-color-primary-900-rgb: 16, 42, 67;
  --app-color-primary-800: #163D63;
  --app-color-primary-700: #1F5A91;
  --app-color-primary-700-rgb: 31, 90, 145;
  --app-color-primary-600: #2878B5;
  --app-color-primary-100: #DCEEFF;
  --app-color-primary-050: #F2F8FD;

  --app-color-accent-800: #9A3412;
  --app-color-accent-700: #C2410C;
  --app-color-accent-600: #EA580C;
  --app-color-accent-500: #F97316;
  --app-color-accent-100: #FFEDD5;
  --app-color-accent-050: #FFF7ED;

  --app-color-success-700: #15803D;
  --app-color-success-100: #DCFCE7;
  --app-color-warning-700: #A16207;
  --app-color-warning-100: #FEF9C3;
  --app-color-danger-800: #991B1B;
  --app-color-danger-700: #B91C1C;
  --app-color-danger-100: #FEE2E2;
  --app-color-info-700: #0369A1;
  --app-color-info-100: #E0F2FE;

  --app-color-surface-page: #F5F8FC;
  --app-color-surface: #FFFFFF;
  --app-color-surface-muted: #EEF3F8;
  --app-color-border: #D9E2EC;
  --app-color-control-boundary: var(--app-color-text-secondary);
  --app-color-text-primary: #243B53;
  --app-color-text-secondary: #486581;
  --app-color-text-muted: #5B7691;
  --app-color-on-primary: var(--app-color-surface);
  --app-color-on-accent: var(--app-color-surface);
  --app-color-on-danger: var(--app-color-surface);

  --app-color-focus: var(--app-color-primary-600);
  --app-color-focus-ring: rgba(40, 120, 181, 0.25);
  --app-color-focus-on-dark: var(--app-color-on-primary);

  --app-font-family: system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
  --app-font-size-base: 1rem;
  --app-font-size-small: 0.875rem;
  --app-font-size-h1: clamp(1.75rem, 1.5rem + 1vw, 2rem);
  --app-font-size-h2: clamp(1.375rem, 1.25rem + 0.5vw, 1.5rem);
  --app-font-size-h3: 1.25rem;
  --app-font-weight-regular: 400;
  --app-font-weight-medium: 500;
  --app-font-weight-bold: 600;
  --app-line-height-body: 1.6;
  --app-line-height-heading: 1.25;

  --app-space-1: 0.25rem;
  --app-space-2: 0.5rem;
  --app-space-3: 0.75rem;
  --app-space-4: 1rem;
  --app-space-5: 1.5rem;
  --app-space-6: 2rem;
  --app-space-7: 3rem;
  --app-space-8: 4rem;

  --app-radius-sm: 6px;
  --app-radius-md: 8px;
  --app-radius-lg: 12px;
  --app-radius-pill: 999px;

  --app-shadow-card: 0 1px 2px rgba(16, 42, 67, 0.06);
  --app-shadow-raised: 0 4px 12px rgba(16, 42, 67, 0.1);

  --app-container-max: 75rem;
  --app-sidebar-width: 16rem;
  --app-control-min-size: 2.75rem;
  --app-focus-outline-width: 3px;
  --app-focus-outline-offset: 2px;

  --app-transition-fast: 120ms ease-out;
}
```

Los valores de `text-secondary` y `text-muted` se oscurecieron respecto de la propuesta inicial para cumplir contraste AA en sus fondos permitidos. Con la fórmula de contraste WCAG, `text-secondary` alcanza 6,08:1 y `text-muted` 4,72:1 sobre `surface` blanca; los valores iniciales alcanzaban 4,28:1 y 2,91:1, respectivamente. `danger-800` se reserva para el estado de la acción destructiva al pasar el cursor.

Los ratios son verificaciones de estas parejas concretas, no una garantía para combinaciones nuevas. Antes de aprobar otra combinación, mídela con una herramienta de contraste. Para texto normal, el objetivo AA es 4,5:1; para texto grande, 3:1.

---

## 3.1 Temas claro y oscuro

LifeTrack ofrece dos temas explícitos: claro por defecto y oscuro cuando el usuario lo selecciona. El tema no cambia la identidad cromática: conserva el azul como color de confianza y el naranja como acento, pero adapta superficies, bordes y texto para mantener jerarquía y contraste.

| Decisión | Regla |
|---|---|
| Fuente de verdad | `tokens.css` contiene los valores base y el bloque `[data-theme="dark"]` contiene las sustituciones oscuras. |
| Activación | `site.js` escribe `data-theme` en `<html>` y actualiza la etiqueta accesible del control. |
| Persistencia | La preferencia se guarda en `localStorage` con una clave propia de LifeTrack; cualquier valor desconocido vuelve a claro. |
| Contraste | Cada pareja de texto, superficie, control y foco se valida contra WCAG 2.2 AA en ambos temas. El color nunca es el único indicador. |
| Preferencia del sistema | No se consulta `prefers-color-scheme`, porque la elección explícita y persistida del usuario tiene prioridad. |
| Alcance | El mismo conjunto de tokens sirve para sidebar, topbar, tarjetas, tablas, formularios y estados. No se escriben colores de tema en componentes. |

El cambio de tema es una mejora de presentación y no altera rutas, datos, formularios ni reglas de negocio. Los controles visuales sin comportamiento de producto tienen una etiqueta honesta y no simulan una operación de backend.

---

## 4. Combinaciones de color permitidas

Usa solo estas parejas texto/fondo. Valida cualquier combinación nueva antes de incorporarla.

| Fondo | Texto | Uso |
|---|---|---|
| `surface` | `text-primary`, `text-secondary`, `text-muted` | Texto general, metadatos y ayuda auxiliar |
| `surface-page` | `text-primary`, `text-secondary` | Texto sobre el fondo de la aplicación |
| `surface-muted` | `text-primary`, `text-secondary` | Superficies neutras, como el hover de una acción secundaria |
| `primary-700` | `on-primary` | Botón principal |
| `primary-800` | `on-primary`, `primary-100` | Navegación profunda y encabezado |
| `primary-100` | `primary-900` | Resaltados y selección |
| `accent-700` | `on-accent` | Botón de acento con texto |
| `accent-100`, `accent-050` | `accent-800` | Progreso y bloques destacados |
| `success-100` | `success-700` | Estado positivo |
| `warning-100` | `warning-700` | Estado de atención |
| `danger-100` | `danger-700` | Error y estado destructivo informativo |
| `danger-700`, `danger-800` | `on-danger` | Botón destructivo |
| `info-100` | `info-700` | Información contextual |

### Prohibido

- Texto blanco sobre `accent-600` o `accent-500`: no alcanza AA para texto normal.
- Texto `accent-500` o `accent-600` sobre fondo claro.
- `text-muted` sobre `surface-page`, `surface-muted` o cualquier fondo distinto de `surface`.
- Texto semántico sobre fondos que no sean su token `-100` o `surface`.

---

## 5. Tipografía

- Usa la pila de fuentes del sistema definida por `--app-font-family`. No cargues fuentes remotas.
- Cuerpo: 16 px con interlineado 1,6.
- Mantén el tamaño raíz de `html` en el valor predeterminado del navegador o `100 %` en todos los puntos de corte; no lo reduzcas a 14 px en móvil ni lo cambies con una media query. `body` usa `--app-font-size-base` (`1rem`), que equivale a 16 px con el tamaño raíz estándar del navegador. Con esa misma base, `--app-control-min-size: 2.75rem` equivale a 44 px. Así se conservan los objetivos sin anular las preferencias de escala del usuario.
- Etiquetas auxiliares y metadatos: 14 px. Evita tamaños menores.
- Título de página (`h1`): 28–32 px. Secciones (`h2`): 22–24 px. Subsecciones (`h3`): 20 px.
- Crea jerarquía con tamaño y peso, no solo con color.
- Importes y cifras: `font-variant-numeric: tabular-nums`.
- Usa un solo `h1` por página y no saltes niveles de encabezado. Un rótulo de métrica que no sea encabezado usa un elemento de texto, no un `h3` por defecto.
- El `ViewModel` entrega importes con el formato numérico establecido por el proyecto (`N2` en `es-ES`) y fechas preparadas para pintar. No formatees en Razor ni en JavaScript. No añadas un símbolo de moneda que la especificación no haya definido.

---

## 6. Disposición y superficies

### Estructura general

- Mobile-first, con la rejilla y los puntos de corte de Bootstrap 5.3.3 que use el proyecto.
- Contenido con ancho máximo `--app-container-max` y márgenes laterales constantes.
- Espaciado entre secciones: `--app-space-5` o `--app-space-6`. Espaciado interno de tarjetas: `--app-space-4` o `--app-space-5`.
- Títulos de página, filtros, acción principal y contenido se alinean igual en todas las pantallas.

### Marco de navegación adoptado

| Ventana gráfica | Comportamiento objetivo |
|---|---|
| Escritorio (`lg` y superior) | Barra lateral fija a la izquierda (`primary-800`, ancho `--app-sidebar-width`) y contenido a la derecha. |
| Móvil y tableta | Cabecera superior con botón de menú que abre navegación accesible mediante `offcanvas` de Bootstrap. |

- Separa la navegación de producto (Resumen (`Dashboard`), movimientos (`Transactions`), categorías (`Categories`)) de las acciones de cuenta que estén implementadas, como cerrar sesión. La eliminación de cuenta solo aparece si se implementa RF-A04.
- Todo destino esencial debe ser alcanzable en móvil.
- Incluye un enlace “Saltar al contenido” como primer elemento enfocable.
- La estructura está inspirada en la distribución de Gentelella, no en su código, assets, colores, fuentes o dependencias. LifeTrack conserva sus tokens y su identidad visual.
- La shell autenticada combina sidebar oscura, marca y navegación agrupada; topbar clara con menú, migas, búsqueda visual, ayuda, tema, notificaciones y avatar; fondo gris suave y contenedor compacto.
- Las pantallas de datos priorizan un encabezado de página con eyebrow, un único `h1`, acción primaria y una tarjeta blanca elevada con subtítulo, toolbar, tabla semántica densa y paginación solo cuando el caso de uso la soporte.
- El layout público no muestra navegación privada. Login, registro, logout y error usan ese layout; las áreas autenticadas usan el shell privado.
- Los parciales de `Views/Shared/Partials/` se limitan a estructura compartida: skip link, navegación, topbar y footer. No contienen reglas de negocio ni consultas.
- El shell ofrece hooks estructurales para breadcrumb y título de página mediante `ViewData["Breadcrumb"]` y `ViewData["Title"]`; si una pantalla no los establece, no se inventa contenido.
- El cajón móvil se puede abrir y cerrar con teclado, mantiene un control visible de cierre y respeta `prefers-reduced-motion`. El contenido conserva un único `main` y el foco es visible.

### Tarjetas

```css
.app-card {
  background: var(--app-color-surface);
  border: 1px solid var(--app-color-border);
  border-radius: var(--app-radius-lg);
  box-shadow: var(--app-shadow-card);
  padding: var(--app-space-5);
}
```

- Sin sombras pesadas, degradados ni bordes decorativos.
- No anides tarjetas dentro de tarjetas.

---

## 7. Integración con Bootstrap

La integración siguiente es una **propuesta para Bootstrap 5.3.3**, la versión del CSS local inspeccionado. Bootstrap 5.3 ofrece variables CSS en tiempo de ejecución para cuerpo, bordes, enlaces, foco y componentes como botones y alertas. Recomprueba los nombres y su uso en los archivos locales si cambia la versión.

### Archivos objetivo

Estos archivos forman la estructura visual implementada:

```
src/LifeTrack.Web/wwwroot/css/
  tokens.css               variables --app-* y únicos valores hex del CSS propio
  bootstrap-overrides.css  puente de variables Bootstrap a --app-*
  components.css           componentes app-* y estilos de páginas
  site.css                 layout global y utilidades mínimas
```

Cuando se añadan, el orden objetivo en `Views/Shared/_Layout.cshtml` es: Bootstrap → `tokens.css` → `bootstrap-overrides.css` → `components.css` → `site.css` → `LifeTrack.Web.styles.css` generado por Razor. Mantén el CSS aislado generado; no lo uses como reemplazo de los tokens ni de los estilos globales.

### Puente de variables

```css
:root {
  --bs-primary: var(--app-color-primary-700);
  --bs-primary-rgb: var(--app-color-primary-700-rgb);
  --bs-body-font-family: var(--app-font-family);
  --bs-body-font-size: var(--app-font-size-base);
  --bs-body-line-height: var(--app-line-height-body);
  --bs-body-color: var(--app-color-text-primary);
  --bs-body-bg: var(--app-color-surface-page);
  --bs-border-color: var(--app-color-border);
  --bs-border-radius: var(--app-radius-md);
  --bs-border-radius-lg: var(--app-radius-lg);
  --bs-link-color-rgb: var(--app-color-primary-700-rgb);
  --bs-link-hover-color-rgb: var(--app-color-primary-900-rgb);
  --bs-focus-ring-color: var(--app-color-focus-ring);
  --bs-form-valid-color: var(--app-color-success-700);
  --bs-form-valid-border-color: var(--app-color-success-700);
  --bs-form-invalid-color: var(--app-color-danger-700);
  --bs-form-invalid-border-color: var(--app-color-danger-700);
}

.btn-primary {
  --bs-btn-bg: var(--app-color-primary-700);
  --bs-btn-border-color: var(--app-color-primary-700);
  --bs-btn-color: var(--app-color-on-primary);
  --bs-btn-hover-bg: var(--app-color-primary-800);
  --bs-btn-hover-border-color: var(--app-color-primary-800);
  --bs-btn-hover-color: var(--app-color-on-primary);
  --bs-btn-active-bg: var(--app-color-primary-900);
  --bs-btn-active-border-color: var(--app-color-primary-900);
  --bs-btn-active-color: var(--app-color-on-primary);
}

.btn-outline-secondary {
  --bs-btn-color: var(--app-color-text-secondary);
  --bs-btn-border-color: var(--app-color-control-boundary);
  --bs-btn-hover-bg: var(--app-color-surface-muted);
  --bs-btn-hover-border-color: var(--app-color-control-boundary);
  --bs-btn-hover-color: var(--app-color-text-primary);
  --bs-btn-active-bg: var(--app-color-surface-muted);
  --bs-btn-active-border-color: var(--app-color-control-boundary);
  --bs-btn-active-color: var(--app-color-text-primary);
}

.form-control,
.form-select,
.form-check-input,
.form-control:focus,
.form-select:focus,
.form-check-input:focus {
  border-color: var(--app-color-control-boundary);
}

.btn-danger {
  --bs-btn-bg: var(--app-color-danger-700);
  --bs-btn-border-color: var(--app-color-danger-700);
  --bs-btn-color: var(--app-color-on-danger);
  --bs-btn-hover-bg: var(--app-color-danger-800);
  --bs-btn-hover-border-color: var(--app-color-danger-800);
  --bs-btn-hover-color: var(--app-color-on-danger);
  --bs-btn-active-bg: var(--app-color-danger-800);
  --bs-btn-active-border-color: var(--app-color-danger-800);
  --bs-btn-active-color: var(--app-color-on-danger);
}

.app-btn-accent {
  --bs-btn-bg: var(--app-color-accent-700);
  --bs-btn-border-color: var(--app-color-accent-700);
  --bs-btn-color: var(--app-color-on-accent);
  --bs-btn-hover-bg: var(--app-color-accent-800);
  --bs-btn-hover-border-color: var(--app-color-accent-800);
  --bs-btn-hover-color: var(--app-color-on-accent);
  --bs-btn-active-bg: var(--app-color-accent-800);
  --bs-btn-active-border-color: var(--app-color-accent-800);
  --bs-btn-active-color: var(--app-color-on-accent);
}

.alert-success {
  --bs-alert-color: var(--app-color-success-700);
  --bs-alert-bg: var(--app-color-success-100);
  --bs-alert-border-color: var(--app-color-success-700);
  --bs-alert-link-color: var(--app-color-success-700);
}

.alert-warning {
  --bs-alert-color: var(--app-color-warning-700);
  --bs-alert-bg: var(--app-color-warning-100);
  --bs-alert-border-color: var(--app-color-warning-700);
  --bs-alert-link-color: var(--app-color-warning-700);
}

.alert-danger {
  --bs-alert-color: var(--app-color-danger-700);
  --bs-alert-bg: var(--app-color-danger-100);
  --bs-alert-border-color: var(--app-color-danger-700);
  --bs-alert-link-color: var(--app-color-danger-700);
}

.alert-info {
  --bs-alert-color: var(--app-color-info-700);
  --bs-alert-bg: var(--app-color-info-100);
  --bs-alert-border-color: var(--app-color-info-700);
  --bs-alert-link-color: var(--app-color-info-700);
}

.btn {
  min-height: var(--app-control-min-size);
}
```

Bootstrap 5.3 utiliza `--bs-link-color-rgb` y `--bs-link-hover-color-rgb` para los enlaces; sobrescribir solo las propiedades de color sólido `--bs-link-color` no los recolorea de forma fiable. La variable `--bs-focus-ring-color` la usa la utilidad `.focus-ring`; no sustituye los estilos de foco de todos los controles de Bootstrap. La sección 12 define el foco de los componentes.

`--bs-border-color` conserva el token claro `--app-color-border` para tarjetas y divisores no interactivos. Los bordes que identifican controles usan `--app-color-control-boundary`; no cambies el valor global de `--bs-border-color` para resolver el contraste de controles.

### Reglas

- Reutiliza clases de Bootstrap para botones, formularios, modales, `offcanvas`, tablas y alertas; los colores de las variantes que usa esta guía deben mapearse explícitamente a tokens, como en el puente anterior.
- Para elementos sin equivalente en Bootstrap (métricas, badges de estado, progreso de presupuesto) usa componentes `app-*`.
- No mezcles una utilidad de Bootstrap y una regla propia que cambien la misma propiedad del mismo elemento.
- No uses `!important`. La única excepción es el bloque de movimiento reducido de la sección 12.

---

## 8. Componentes

Los ejemplos asumen que el `ViewModel` ya trae textos, importes, clases de estado y valores de accesibilidad preparados. La vista solo renderiza; `@if` y `@foreach` se usan únicamente para elegir qué renderizar o recorrer datos, conforme a `docs/09-estandar-vistas-mvc.md`.

### 8.1 Botones y enlaces

| Tipo | Clase | Cuándo |
|---|---|---|
| Principal | `btn btn-primary` | Una por pantalla: la acción más importante. |
| Secundario | `btn btn-outline-secondary` | Resto de acciones. |
| Acento | `btn app-btn-accent` | Acción deliberada y de alto énfasis; máximo uno por vista y nunca con el mismo peso que el principal. |
| Destructivo | `btn btn-danger` | Elimina o descarta datos. Siempre con confirmación. |
| Enlace | `<a>` | Usa un subrayado visible dentro de texto corrido. |

- Objetivo táctil mínimo: 44 px.
- Texto del botón: verbo + objeto, en español (“Guardar movimiento”, “Crear categoría”). Evita “Aceptar” y “Enviar” genéricos.
- Las acciones destructivas explican la consecuencia antes de confirmar.

```cshtml
<div class="modal-body">
  <p>@Model.ConsequenceText</p>
</div>
<div class="modal-footer">
  <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Cancelar</button>
  <button type="submit" class="btn btn-danger">@Model.ConfirmLabel</button>
</div>
```

### 8.2 Tarjeta de métrica

Cada métrica tiene etiqueta, valor y periodo o contexto. Las comparaciones se expresan con texto; una flecha o un color no bastan.

```cshtml
<article class="app-card app-metric">
  <p class="app-metric__label">@Model.Label</p>
  <p class="app-metric__value">@Model.ValueFormatted</p>
  <p class="app-metric__context">@Model.PeriodLabel</p>
  @if (Model.Comparison is not null)
  {
    <p class="app-metric__comparison @Model.Comparison.CssClass">
      <span aria-hidden="true">@Model.Comparison.Symbol</span>
      @Model.Comparison.Text
    </p>
  }
</article>
```

```css
.app-metric__label {
  color: var(--app-color-text-secondary);
  font-size: var(--app-font-size-small);
  font-weight: var(--app-font-weight-medium);
  margin: 0;
}

.app-metric__value {
  font-size: var(--app-font-size-h2);
  font-variant-numeric: tabular-nums;
  font-weight: var(--app-font-weight-bold);
  margin: var(--app-space-1) 0;
}

.app-metric__context,
.app-metric__comparison {
  color: var(--app-color-text-secondary);
  font-size: var(--app-font-size-small);
  margin: 0;
}
```

- `Comparison.CssClass` llega preparado en el `ViewModel`; no codifiques una regla de presentación como ternario en Razor.
- No uses rojo para representar “más gasto”. Un gasto mayor no es un error.
- Destaca una o dos métricas clave. En el Dashboard, agrupa en este orden: resumen financiero, presupuesto si RF-F06 está implementado y proyección de saldo si RF-D02 está implementado.

### 8.3 Insignia de estado

Siempre comunica el estado con icono, texto y color.

```cshtml
<span class="app-badge @Model.Status.CssClass">
  <svg class="app-badge__icon" aria-hidden="true" focusable="false"><use href="@Model.Status.IconHref" /></svg>
  @Model.Status.Label
</span>
```

| Estado | Fondo | Texto | Etiqueta de ejemplo |
|---|---|---|---|
| `success` | `success-100` | `success-700` | “Completado” |
| `warning` | `warning-100` | `warning-700` | “Cerca del límite” |
| `danger` | `danger-100` | `danger-700` | “Límite superado” |
| `info` | `info-100` | `info-700` | “Pendiente de revisar” |
| `neutral` | `surface-muted` | `text-secondary` | “Sin actividad” |
| `progress` | `accent-100` | `accent-800` | “En racha” |

```css
.app-badge {
  align-items: center;
  border-radius: var(--app-radius-sm);
  display: inline-flex;
  font-size: var(--app-font-size-small);
  font-weight: var(--app-font-weight-medium);
  gap: var(--app-space-1);
  padding: var(--app-space-1) var(--app-space-2);
}

.app-badge--success { background: var(--app-color-success-100); color: var(--app-color-success-700); }
.app-badge--warning { background: var(--app-color-warning-100); color: var(--app-color-warning-700); }
.app-badge--danger { background: var(--app-color-danger-100); color: var(--app-color-danger-700); }
.app-badge--info { background: var(--app-color-info-100); color: var(--app-color-info-700); }
.app-badge--neutral { background: var(--app-color-surface-muted); color: var(--app-color-text-secondary); }
.app-badge--progress { background: var(--app-color-accent-100); color: var(--app-color-accent-800); }

```

### 8.4 Formularios

- Cada campo tiene una etiqueta visible y permanente. El `placeholder` nunca sustituye a la etiqueta.
- Distingue los estados normal, foco, error, deshabilitado y válido.
- El error aparece junto al campo, está asociado con `aria-describedby` y explica cómo corregirlo.
- Si la validación falla, conserva los valores introducidos.
- Importes: `inputmode="decimal"`. Fechas: `type="date"` cuando corresponda al requisito.
- Deshabilitar un botón no sustituye explicar por qué algo no se puede hacer.

```cshtml
<div class="mb-3">
  <label asp-for="Amount" class="form-label"></label>
  <input asp-for="Amount" class="form-control" inputmode="decimal" autocomplete="off"
         aria-describedby="transactions-amount-help transactions-amount-error" />
  <div id="transactions-amount-help" class="form-text">Introduce el importe sin símbolo de moneda.</div>
  <span asp-validation-for="Amount" id="transactions-amount-error" class="invalid-feedback d-block"></span>
</div>
```

ASP.NET añade `input-validation-error`, `field-validation-error` y `validation-summary-errors`. Estílalos en `components.css` con los tokens semánticos; el error incluye texto o icono además del borde.

```css
.input-validation-error { border-color: var(--app-color-danger-700); }
.field-validation-error { color: var(--app-color-danger-700); font-size: var(--app-font-size-small); }
```

Los mensajes de error indican qué ocurrió y cómo corregirlo. Ejemplo: “El importe debe ser mayor que 0. Introduce una cantidad válida.”

### 8.5 Tablas y movimientos

- Usa encabezados claros (`<th scope="col">`) y un `<caption>` visible u oculto con `visually-hidden` de Bootstrap.
- Alinea importes a la derecha y usa `tabular-nums`.
- Muestra ordenación, filtros y paginación cuando existan; por ejemplo, `aria-sort` en la columna activa.
- Incluye el signo en el texto del importe (por ejemplo, “+1.200,00” o “−45,90”). Los ingresos pueden usar `success-700`; un gasto normal usa `text-primary`, no rojo ni naranja.
- En pantallas estrechas (menos de `md`), cada fila pasa a bloque con pares etiqueta-valor, en lugar de forzar desplazamiento horizontal.

```cshtml
<div class="app-table-wrapper">
  <table class="table app-table">
    <caption class="visually-hidden">Lista de movimientos</caption>
    <thead>
      <tr>
        <th scope="col">Fecha</th>
        <th scope="col">Categoría</th>
        <th scope="col" class="text-end">Importe</th>
      </tr>
    </thead>
    <tbody>
      @foreach (var row in Model.Rows)
      {
        <tr>
          <td data-transactions-label="Fecha">@row.DateFormatted</td>
          <td data-transactions-label="Categoría">@row.CategoryName</td>
          <td data-transactions-label="Importe" class="text-end app-amount @row.AmountCssClass">@row.AmountFormatted</td>
        </tr>
      }
    </tbody>
  </table>
</div>
```

```css
.app-amount {
  font-variant-numeric: tabular-nums;
  font-weight: var(--app-font-weight-medium);
}

.app-amount--income { color: var(--app-color-success-700); }
.app-amount--expense { color: var(--app-color-text-primary); }

@media (max-width: 767.98px) {
  .app-table thead { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); }
  .app-table tr { border-bottom: 1px solid var(--app-color-border); display: block; padding: var(--app-space-3) 0; }
  .app-table td { border: 0; display: flex; gap: var(--app-space-3); justify-content: space-between; padding: var(--app-space-1) 0; }
  .app-table td::before { color: var(--app-color-text-secondary); content: attr(data-transactions-label); }
}
```

### 8.6 Categorías y presupuestos

- Una categoría muestra su nombre. No presupongas un icono o color asignado por el usuario: ese dato no aparece en el modelo funcional de `docs/01-especificacion.md`.
- La barra de presupuesto muestra el uso y su estado, siempre con texto.

| Estado | Barra | Badge | Texto de ejemplo |
|---|---|---|---|
| Dentro del límite | `primary-600` | `info` o `success` | “Te quedan 120 este mes” |
| Cerca del límite | `warning-700` | `warning` | “Cerca del límite: 85 % usado” |
| Límite superado | `danger-700` | `danger` | “Has superado el límite en 30” |

Los umbrales los define `docs/01-especificacion.md` (RF-F06: aviso al 80 % y al 100 %), no este documento. Aquí solo se define cómo representarlos.

```cshtml
<div class="app-progress @Model.State.CssClass">
  <div class="app-progress__header">
    <span class="app-progress__label">@Model.CategoryName</span>
    <span class="app-progress__value">@Model.UsageText</span>
  </div>
  <div class="app-progress__track" role="progressbar"
       aria-label="@Model.AriaLabel"
       aria-valuemin="0" aria-valuemax="100" aria-valuenow="@Model.ProgressBarValue"
       aria-valuetext="@Model.ProgressAccessibleValue">
    <div class="app-progress__fill" data-app-percent="@Model.FillPercent"></div>
  </div>
  <p class="app-progress__status">@Model.State.Label</p>
</div>
```

`ProgressBarValue` y `FillPercent` llegan limitados al rango 0–100 desde el `ViewModel`, incluso si el gasto supera el presupuesto. `UsageText` y `ProgressAccessibleValue` comunican el consumo real, que puede ser superior al 100 %. No calcules ni limites esos valores en Razor.

El mecanismo para asignar el ancho sin `style="..."` sigue pendiente de decisión: clases discretas preparadas por el `ViewModel` o JavaScript. Si se elige JavaScript, respeta la IIFE y declara el selector y la clave de `data-app-percent` en el bloque `SELECTORS` de `docs/08-estandar-javascript.md`, no dentro de una función.

### 8.8 Navegación

- Elemento activo: fondo `primary-700` sobre la sidebar, peso mayor e indicador visible; incluye `aria-current="page"`. El naranja nunca indica selección.
- Texto sobre la sidebar: `on-primary` o `primary-100`.
- Foco sobre fondo oscuro: usa `--app-color-focus-on-dark` (sección 12).

```cshtml
<nav aria-label="Navegación principal" class="app-nav">
  <ul class="app-nav__list">
    @foreach (var item in Model.Items)
    {
      <li>
        <a class="@item.LinkCssClass"
           asp-controller="@item.Controller" asp-action="@item.Action"
           aria-current="@item.AriaCurrent">@item.Label</a>
      </li>
    }
  </ul>
</nav>
```

`LinkCssClass` y `AriaCurrent` llegan preparados en el `ViewModel`; la vista no usa ternarios para construir atributos.

### 8.9 Alertas y mensajes

Usa alertas de Bootstrap con los colores semánticos puenteados en la sección 7. Cada alerta incluye icono, texto, el `role` adecuado y una acción útil cuando exista.

| Tipo | `role` | Cuándo |
|---|---|---|
| Éxito | `status` | Operación completada (“Movimiento guardado”) |
| Información | `status` | Contexto no urgente |
| Advertencia | `status` | Atención necesaria, sin bloqueo |
| Error | `alert` | Algo falló y requiere acción |

Los mensajes de éxito no usan “con éxito” ni exclamaciones excesivas. Ejemplo: “Categoría creada”.

---

## 9. Gráficos

- Si una pantalla dentro del alcance muestra series, ingresos usan azul (`primary-700`) y gastos naranja (`accent-600`). La paleta no sustituye a las etiquetas.
- No confíes solo en el color: etiqueta las series o incluye una leyenda legible, y ofrece valores o una tabla equivalente accesible.
- Mantén el mismo significado de color de serie en todas las pantallas.
- Lee los colores desde variables CSS (`--app-color-primary-700`, `--app-color-accent-600`) mediante constantes en el JavaScript que corresponda a `docs/08`. No escribas valores de color en JavaScript.
- No añadas una librería de gráficos sin aprobación. Si no hay una aprobada, utiliza SVG o CSS sencillos.
- Cada gráfico incluye un título, una descripción breve (`aria-describedby`) y una alternativa textual o tabular.

En listas y tablas, un ingreso puede mostrarse en verde (`success-700`) con signo “+”; en un gráfico, la serie de ingresos es azul. Son contextos distintos y se diferencian con texto, signo y etiqueta.

---

## 10. Estados de la interfaz

Toda pantalla o componente con datos define los estados aplicables de esta tabla.

| Estado | Reglas |
|---|---|
| Cargando | Indicador visible. Si dura más de un instante, usa esqueleto o `spinner-border` con `role="status"` y texto oculto “Cargando…”. |
| Vacío | Explica qué puede hacerse a continuación y ofrece la acción. Ejemplo: “Aún no tienes movimientos. Registra el primero para empezar.” No uses “No hay datos”. |
| Error | Explica qué ocurrió y qué hacer, con una acción de reintento o salida. No muestres detalles técnicos, trazas ni identificadores internos. |
| Éxito | Confirmación breve y clara (sección 8.9). |

```cshtml
<section class="app-empty-state" aria-labelledby="empty-title">
  <h2 id="empty-title" class="app-empty-state__title">@Model.Title</h2>
  <p class="app-empty-state__text">@Model.Text</p>
  <a class="btn btn-primary" asp-action="@Model.ActionName">@Model.ActionLabel</a>
</section>
```

---

## 11. Tono y textos

- Segunda persona del singular (“tú”). Cercano, claro y sin jerga financiera innecesaria.
- Mayúscula inicial y resto en minúsculas, salvo nombres propios, en botones, encabezados, pestañas y etiquetas.
- Sin signos de exclamación en mensajes del sistema.
- Verbo primero en acciones: “Crear categoría”, “Guardar cambios”.
- Nunca culpabilices: describe la situación y ofrece el siguiente paso.
- Evita “por favor” en etiquetas e instrucciones de interfaz.
- Los textos salen del `ViewModel` o de recursos; no se dejan literales dispersos en JavaScript (`docs/08-estandar-javascript.md`).

---

## 12. Accesibilidad

Objetivo: **WCAG 2.2 AA**, coherente con RNF-09 de `docs/01-especificacion.md`.

- Toda función se puede operar con teclado, con orden de foco lógico.
- Los bordes visibles que identifican un control interactivo deben alcanzar al menos 3:1 frente a la superficie adyacente. Usa `--app-color-control-boundary` en botones secundarios delineados y en controles de formulario, selección y casilla; conserva `--app-color-border` para tarjetas y divisores no interactivos. Verifica también los estados de foco, selección y error.
- El foco es visible en todos los controles:

```css
:focus-visible {
  outline: var(--app-focus-outline-width) solid var(--app-color-focus);
  outline-offset: var(--app-focus-outline-offset);
}

.btn:focus-visible,
.form-control:focus-visible,
.form-select:focus-visible,
.form-check-input:focus-visible {
  box-shadow: none;
}

.app-on-dark :focus-visible,
.app-nav :focus-visible {
  outline-color: var(--app-color-focus-on-dark);
}
```

Este CSS global se carga después de Bootstrap para que el contorno propio sustituya la sombra de foco de los controles enumerados. Comprueba el foco de cada componente Bootstrap añadido; `--bs-focus-ring-color` por sí solo no configura todos sus controles.

- Usa HTML semántico: `header`, `nav`, `main`, `section`, `article` y `footer`. Un solo `main` por página.
- Asocia etiquetas y mensajes de error con sus controles (`for`, `aria-describedby`).
- Estado y selección nunca dependen solo del color: usa texto, icono o forma.
- Objetivo táctil de 44 px siempre que sea posible.
- Iconos decorativos con `aria-hidden="true"`. Botones solo con icono llevan `aria-label` en español.
- Tablas con `<th scope>`; gráficos con alternativa textual.
- Usa `lang="es"` en `<html>`.
- Permite zoom al 200 % y reflujo a 320 px sin pérdida de contenido ni scroll horizontal, salvo contenido que realmente lo requiera dentro de un contenedor propio.

### Movimiento

- Transiciones breves (≤ 200 ms) y no esenciales.
- Respeta la preferencia del usuario:

```css
@media (prefers-reduced-motion: reduce) {
  *, *::before, *::after {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
    scroll-behavior: auto !important;
  }
}
```

Este bloque es el único uso permitido de `!important`, por ser una medida estándar de accesibilidad.

---

## 13. Iconos

- La librería de iconos o el uso de SVG propio sigue pendiente de decisión y aprobación; registra la decisión en `docs/adr/` si procede.
- Hasta esa decisión, no añadas paquetes ni fuentes remotas. Si una vista necesita iconos, usa SVG inline mediante un parcial compartido como propuesta de implementación, no como un archivo que ya exista.
- Iconos previstos cuando los estados correspondientes estén implementados: éxito, aviso, error, información, flecha de comparación, menú y cerrar.
- Un icono nunca es la única forma de comunicar algo: acompáñalo de texto o, si es un botón iconográfico, de `aria-label`.

---

## 14. Lista de comprobación para PR de interfaz

**Tokens y estilos**

- [ ] No hay valores hexadecimales en el CSS propio fuera de `tokens.css`, ni `style="..."` o bloques `<style>` en vistas.
- [ ] Se usan tokens `--app-*` y clases `app-*`, de la funcionalidad o de Bootstrap.
- [ ] No hay `!important` fuera del bloque de movimiento reducido.
- [ ] No se añaden paquetes, fuentes, iconos o librerías sin aprobación.

**Contenido y estados**

- [ ] Los textos visibles están en español; los identificadores y clases, en inglés.
- [ ] Los datos llegan preparados desde el `ViewModel`; la vista no calcula ni formatea.
- [ ] La pantalla define los estados aplicables de carga, vacío, error y éxito.
- [ ] Ningún estado depende solo del color.

**Accesibilidad**

- [ ] Todo es operable con teclado y el foco es visible, también en los controles Bootstrap usados.
- [ ] Cada campo tiene etiqueta visible y su error asociado.
- [ ] El contraste de cualquier combinación nueva se ha revisado.
- [ ] Hay objetivos táctiles de 44 px donde sea viable.
- [ ] Se ha probado a 320 px, 768 px y escritorio.

**Marca**

- [ ] Hay una sola acción primaria por pantalla.
- [ ] El naranja se usa de forma controlada, nunca para errores ni para selección.
- [ ] Un gasto alto no se presenta con estilo de error.

---

## 15. Decisiones pendientes

| Tema | Estado |
|---|---|
| Librería de iconos o SVG propio | Pendiente de decisión y aprobación |
| Librería de gráficos | Pendiente de aprobación |
| Ancho de las barras de progreso sin `style="..."` | Pendiente de elegir entre clases preparadas por el `ViewModel` y JavaScript; si afecta a la arquitectura, registrar un ADR |
| Símbolo de moneda | No especificado en `docs/01-especificacion.md`; no lo presupongas en la interfaz |
| Versión de Bootstrap | El CSS local comprobado es 5.3.3; verifica nuevamente variables y estilos si cambia |
| Tema oscuro | Adoptado. Se mantiene mediante overrides completos de tokens en `tokens.css`, persistencia local y control accesible en la topbar |
