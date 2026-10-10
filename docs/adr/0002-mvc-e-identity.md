# ADR-0002: ASP.NET Core MVC con vistas Razor e Identity

- **Estado:** Aceptada
- **Fecha:** 2026-10-02
- **Sustituye:** la decisión original de UI con Razor Pages

## Contexto
Una sola persona desarrolla y opera la aplicación. Hay que minimizar piezas desplegadas, superficie de
ataque y consumo de la capa gratuita.

El proyecto necesita además una capa de presentación con una frontera clara: los datos que salen de
los casos de uso de `Application` no deben llegar tal cual a la vista. Cada pantalla necesita su
propio modelo, con los importes y las fechas ya formateados, y las vistas no deben calcular nada.

También hay que decidir el estilo visual sin añadir una cadena de herramientas de build que
consuma tiempo de CI y superficie de mantenimiento en un proyecto que debe salir adelante en un TFM.

## Decisión
- UI renderizada en servidor con **ASP.NET Core MVC** (controllers, vistas Razor, parciales y
  ViewComponents), en lugar de Razor Pages.
- Una **capa `ViewModels` dedicada**: cada pantalla tiene su modelo, construido con una fábrica
  estática `From(...)` a partir del resultado del handler. Los `ViewModels` no heredan de entidades de
  `Domain` ni de DTOs de `Application`.
- **Cero lógica en las vistas.** Sin ternarios, sin declaraciones de variables, sin llamadas que
  calculen o formateen. `@if` y `@else` solo para elegir qué se renderizar.
- Dos layouts responsive con responsabilidades separadas: un layout público por defecto y un shell
  autenticado seleccionado por `_ViewStart.cshtml` anidados. No se decide por User-Agent.
- Rutas por atributo (`[Route]`), en inglés.
- Autenticación por cookie con **ASP.NET Core Identity** y su UI por defecto, que sigue siendo Razor
  Pages y es la única parte del proyecto que usa esa tecnología.
- `AutoValidateAntiforgeryToken` registrado como filtro global obligatorio.
- **CSS propio** escrito a mano en `wwwroot/css`, *mobile-first*. Sin pipeline de build, sin Tailwind,
  sin CDN. Las librerías JS se auto-alojan en `wwwroot/lib`.

## Consecuencias
- (+) Una frontera explícita entre el caso de uso y la pantalla: los `ViewModels` son el único contrato
  de la capa de presentación, y sus fábricas son testeables sin depender del controller.
- (+) Las reglas de presentación quedan concentradas en una capa verificable, en lugar de repartidas
  por plantillas.
- (+) Parciales y ViewComponents con sitio fijo: cada pantalla se localiza con su controller, su
  modelo, su vista y sus parciales.
- (+) Dos layouts y CSS propio: la navegación privada queda fuera de las vistas públicas sin cadena de
  herramientas de JS, sin `node_modules`, sin paso de build en CI y sin procesar CSS con un tercero.
- (+) Un solo despliegue; codificación de salida de Razor; sin gestión de tokens en el navegador.
- (−) Anti-forgery deja de ser automático y pasa a ser una responsabilidad explícita. Se mitiga con el
  filtro global, los tag Helpers en todos los formularios y un test de arquitectura que falla si el
  filtro desaparece.
- (−) La UI por defecto de Identity sigue siendo Razor Pages, así que el proyecto necesita registrar
  `AddRazorPages()` y `MapRazorPages()` exclusivamente para ella. Mezcla de dos tecnologías en la capa
  de presentación, aceptada por no escribir autenticación a mano.
- (−) La UI por defecto de Identity usa estilos en línea, por lo que la CSP mantiene
  `style-src 'unsafe-inline'` (deuda de endurecimiento documentada).
- (−) Menos interactividad que una SPA; se compensa con mejoras progresivas puntuales.

## Alternativas descartadas
- **Razor Pages:** menos piezas y anti-forgery automático, pero sin frontera natural entre caso de uso
  y pantalla, lo que empuja el formato y las decisiones de presentación dentro de las plantillas.
- **SPA (React/Angular) + API:** más superficie (CORS, tokens, dos despliegues).
- **Blazor Server:** viable, pero mantiene conexiones persistentes que consumen más en la capa gratuita.
- **Tailwind con pipeline:** mejor ergonomía de estilos, pero añade Node, un paso de build y una
  dependencia externa. Descartado a favor de CSS propio.
- **Escribir controllers de cuenta propios:** más control visual, pero significa implementar
  autenticación a mano, que es exactamente lo que prohíben las reglas de seguridad del proyecto.
