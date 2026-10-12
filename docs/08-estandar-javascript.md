# Estándar de JavaScript

Norma obligatoria para todo el JavaScript del proyecto. Los identificadores van en **inglés**;
los textos que ve el usuario, en **español**.

## 1. Dónde vive cada fichero

```
wwwroot/js/
├── shared/            # Componentes reutilizables. API pública, sin prefijo _
│   ├── modal.js
│   ├── confirm.js
│   ├── toast.js
│   ├── dropdown.js
│   ├── tabs.js
│   └── form-validation.js
└── pages/             # Un fichero por pantalla. Prefijo propio de la feature
    ├── dashboard.js
    ├── categories.js
    ├── transactions.js
    └── habits.js
```

- Un fichero por pantalla o por componente. **Prohibido** un fichero "general" que mezcle features.
- Los nombres de fichero van en `kebab-case` y describen la feature: `habits.js`, no `main.js`.
- No se carga JavaScript desde un CDN. Todo se auto-aloja en `wwwroot/lib` y `wwwroot/js`.

## 2. Cómo se incluye

Cada script se etiqueta con su ámbito, para no arrastrar componentes a páginas que no los usan.

La pantalla de hábitos, por ejemplo, necesita el dropdown, el toast y su propio script de página. Cada
etiqueta declara solo su ámbito: arrastrar un componente a una página que no lo usa es coste, no
funcionalidad.

```html
<script src="~/js/shared/dropdown.js" defer></script>
<script src="~/js/shared/toast.js" defer></script>
<script src="~/js/pages/habits.js" defer></script>
```

Todos con `defer`. **Prohibido** `<script>` en línea: la CSP los bloquea.

## 3. El patrón: IIFE

Todo JavaScript se encapsula en una IIFE. Ninguna variable de un script queda en el ámbito global.

```javascript
const habitsPageScript = (function () {
    "use strict";

    return {};
}());
```

Ese asigna a una `const` para que el fichero sea identificable en las herramientas de desarrollo,
pero **el valor real de la IIFE es el aislamiento**: nada de lo que declares dentro es global.

## 4. Visibilidad: `_` marca lo privado

Esta es la regla más importante del fichero.

| Forma | Significado |
|---|---|
| `_nombreFuncion` | **Privada.** Solo se usa dentro de la IIFE. Nadie fuera puede llamarla |
| `nombreFuncion` | **Pública.** Expuesta deliberadamente en el objeto global del componente |

```javascript
const habitsPageScript = (function () {
    "use strict";

    function _preserveCurrentPage(form) {
    }

    function refresh() {
    }

    return { refresh };
}());
```

`_preserveCurrentPage` es **privada**: solo la usa esta IIFE y nadie fuera puede llamarla, así que no
aparece en el `return`. `refresh` es **pública**: se expone deliberadamente en el objeto global del
componente porque otra parte de la aplicación la necesita.

**Regla:** si lleva `_`, es privado y **no se expone** en el `return`. Si se expone, **no lleva `_`**.
No hay excepciones. Un nombre con `_` no se documenta como API.

### Puntos de entrada

El script de una página tiene **un único** punto de entrada, `_initialize()`, invocado al final de la IIFE.

```javascript
const habitsPageScript = (function () {
    "use strict";

    function _initialize() {
    }

    _initialize();

    return {};
}());
```

El cuerpo de `_initialize()` engancha los *listeners* y no hace nada más: es el único punto por el que
el script cobra vida, y por eso hay exactamente uno por fichero.

Un componente compartido **no** lleva `_initialize()`: se engancha con listeners delegados, de modo que
funciona aunque el script se cargue después de que el DOM ya exista.

## 5. Los tres bloques de constantes

Todo fichero JavaScript empieza con **tres bloques de constantes**, en este orden, y nada más antes
de las funciones.

```javascript
const sentTransactionsPageScript = (function () {
    "use strict";

    const SELECTORS = {
        FILTERS_FORM: "[data-transactions-filters-form]",
        CATEGORY_OPTION: ".transactions-segmented-option",
        DROPDOWN: "[data-transactions-dropdown]",
        DROPDOWN_TRIGGER: "[data-transactions-dropdown-trigger]",
        DROPDOWN_LIST: "[data-transactions-dropdown-list]",
        STATUS_OPTION: ".transactions-dropdown-option",
        STATUS_SELECTED: "transactions-dropdown-option--selected",
        STATUS_VALUE_DATA: "value",
        CATEGORY_INPUT_BY_NAME: "input[name='",
        ARIA_EXPANDED: "aria-expanded",
        ARIA_SELECTED: "aria-selected"
    };

    const CSS_CLASSES = {
        OPTION_SELECTED: "transactions-dropdown-option--selected"
    };

    const CONSTANTS = {
        DEFAULT_PAGE: "1",
        TRUE: "true",
        FALSE: "false"
    };

    return {};
}());
```

El script de una página lleva sus selectores, sus clases y sus literales en esos tres bloques, y detrás
solo funciones. Ningún valor suelto, en ninguna posición.

### Qué va en cada bloque

| Bloque | Contenido |
|---|---|
| `SELECTORS` | Selectores CSS completos: clases, atributos, `data-*`, atributos ARIA |
| `CSS_CLASSES` | Nombres de clase sueltos que se añaden o quitan |
| `CONSTANTS` | Valores literales: textos por defecto, números, `true`/`false`, puntos de rupture |

### La regla que no se negocia

> **Ningún selector, clase, id, atributo `data-*` ni valor literal se escribe fuera de estos bloques.**
> Nunca en línea, nunca dentro de una función.

En el primer caso, los selectores y la clase están escritos en línea: **prohibido**. En el segundo,
todo sale de los bloques de constantes: **correcto**. Ambos implementan la misma lógica; la diferencia
es que el segundo sobrevive a un cambio de nombre de clase o de atributo.

```javascript
function _closeAllDropdowns() {
    document.querySelectorAll("[data-dropdown]").forEach(function (dd) {
        dd.querySelector(".dropdown-option--selected").classList.remove("is-open");
    });
    trigger.setAttribute("aria-expanded", "false");
}
```

```javascript
function _closeAllDropdowns() {
    document.querySelectorAll(SELECTORS.DROPDOWN).forEach(function (dd) {
        dd.querySelector(SELECTORS.STATUS_OPTION).classList.remove(CSS_CLASSES.OPTION_SELECTED);
    });
}
```

Consecuencia práctica: **cambiar el nombre de una clase o de un `data-*` es editar una constante y
nada más.** Si hay que tocar una vista y un `.js` por el mismo cambio, es señal de que ese selector
no está en el bloque. Un `grep` de `.cshtml` y de `.js` por un mismo literal debe salir vacío.

### Claves de `data-*`

Las claves de atributos `data-*` también son constantes: `SELECTORS.STATUS_VALUE_DATA` vale `"value"`,
y se lee como `element.dataset[SELECTORS.STATUS_VALUE_DATA]`. **Prohibido** escribir `dataset.category`
o `dataset.value` en línea.

### Nombres de variables CSS

Los nombres de clase CSS y los atributos `data-*` llevan **prefijo de la feature**, en `kebab-case`:

| Prefijo | Se usa en |
|---|---|
| `app-*` | Componentes compartidos de `wwwroot/js/shared/` |
| `dashboard-*` | Pantalla de resumen |
| `categories-*` | Pantalla de categorías |
| `transactions-*` | Pantalla de movimientos |
| `habits-*` | Pantalla de hábitos |

Así un selector de una pantalla no puede coincidir por accidente con el de otra, y el HTML se lee
solo: `data-habits-checkin-button` dice qué hace sin abrir el fichero.

## 6. Componentes compartidos

Todos exponen su API **sin `_`** en un objeto global. Los nombres son estables: cambiarlos rompe
todas las vistas.

### `Modal`

```javascript
Modal.open(modalId);
Modal.close(modalId);
Modal.closeAll();
```

`open` y `close` reciben el **id** del modal y lo abren o lo cierran; `closeAll` cierra todos los que
haya en la página.

Un modal se dispara con `data-modal-open="<id>"` en el elemento que lo abre. Cierra con
`data-modal-close` y al pulsar `Escape`.

### `Confirm`

```javascript
Confirm.open({
    title: "Eliminar categoría",
    message: "Esta acción no se puede deshacer.",
    confirmText: "Eliminar",
    cancelText: "Cancelar",
    onConfirm: function () {
    }
});
```

El `onConfirm` **decide qué hacer** y es el único lugar donde vive esa decisión: el componente no envía
peticiones ni conoce la aplicación.

Los textos por defecto son **españoles**. El `onConfirm` es el único punto donde vive la decisión:
el componente no envía peticiones ni conoce la aplicación.

### `Toast`

```javascript
Toast.success("Movimiento registrado");
Toast.error("No se ha podido guardar");
Toast.info("Sin movimientos este mes");
```

### `Dropdown`

Sin API pública. Se engancha con listeners delegados a partir de `data-transactions-dropdown` y sus
hijos, y gestiona `aria-expanded` por su cuenta. Al elegir una opción, submits el formulario que
contenga el trigger si lo hay.

### `Tabs`

Sin API pública. Se engancha con `data-habits-tabs`, `data-habits-tab` y `data-habits-tab-panel`,
y mantiene sincronizados `aria-selected` y la visibilidad del panel.

### `FormValidation`

Sin API pública. Se engancha con `data-habits-validate`. Valida los `required` del navegador, muestra
el error junto al campo y bloquea el envío. **No** decide reglas de negocio: eso ya lo hizo el
dominio, y el mensaje llega al navegador desde el handler a través de su excepción de frontera.

## 7. Estilo dentro del fichero

- `"use strict"` al inicio de la IIFE.
- `function` declaradas, no flechas, para que `this` no dependa del contexto.
- **Prohibido** `var`. Usa `const`; `let` solo si la reasignación es real.
- **Prohibido** `document.getElementById` para leer atributos `data-*`: usa `dataset` con la clave de `SELECTORS`. Resolver un elemento por su `id` (como hace el modal para abrirlo) sí está permitido.
- **Prohibido** el selector por índice (`querySelectorAll(...)[0]`) para alcanzar un elemento con
  estado: usa un atributo que lo identifique.
- Al construir un selector concatenando un valor dinámico, **escapa** el valor:
  `CSS.escape(valor)`. Un `name` con una comilla rompería el selector.
- Para leer un booleano de un atributo, usa `CONSTANTS.TRUE` / `CONSTANTS.FALSE` en lugar de
  `String(!abierto)` o de la cadena `"true"` suelta.
- **Cero comentarios.** Ni `//`, ni `/* */`, ni `TODO`, ni código comentado. El script se explica con
  los nombres de las funciones, los nombres de las constantes y el orden de los bloques. Si una
  restricción no se puede expresar en el código, se documenta en `docs/`.

## 8. Lista antes de entregar

- [ ] El fichero es una IIFE, sin estado global salvo la API pública del componente.
- [ ] `_` marca lo privado y lo privado no se expone en el `return`.
- [ ] Un único punto de entrada `_initialize()`, solo en scripts de página.
- [ ] Los tres bloques de constantes están arriba, y no queda ningún selector ni literal suelto.
- [ ] Clases y `data-*` llevan el prefijo de su feature.
- [ ] Las claves de `dataset` salen de `SELECTORS`.
- [ ] Los valores booleanos salen de `CONSTANTS`.
- [ ] Textos visibles al usuario en español; identificadores en inglés.
- [ ] Sin `var`, sin `<script>` en línea, sin CDN.

## 9. Prohibiciones

- Selectores, clases, ids o `data-*` escritos en línea o dentro de una función.
- Funciones públicas con `_` al principio.
- Variables globales fuera de una IIFE.
- Punto de entrada distinto de `_initialize()`, o más de uno por fichero.
- `var`.
- `document.getElementById` para leer atributos `data-*` (resolver por `id` sí está permitido).
- Cargar JavaScript desde un CDN.
- `<script>` en línea (la CSP lo bloquea).
- Un mismo fichero que mezcle varias features.
- Nombres de clase o `data-*` compartidos entre features sin el prefijo `app-`.
- Cualquier comentario en el fichero: ni `//`, ni `/* */`, ni `TODO`, ni código comentado.