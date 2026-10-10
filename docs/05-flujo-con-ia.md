# Desarrollo asistido con gentle-ai

gentle-ai orquesta el flujo de trabajo del proyecto. Este documento no vuelve a definir sus fases, prompts ni comandos: establece el contexto de LifeTrack que debe acompañar ese flujo y las verificaciones que siguen siendo responsabilidad del proyecto.

## Fuentes de verdad

| Necesidad | Fuente canónica |
|---|---|
| Requisitos, reglas de negocio y criterios de aceptación | [`01-especificacion.md`](01-especificacion.md) |
| Arquitectura y dependencias | [`02-arquitectura.md`](02-arquitectura.md) y decisiones vigentes en [`adr/`](adr/) |
| Reglas para agentes y personas | [`AGENTS.md`](../AGENTS.md) y el `AGENTS.md` de cada capa afectada |
| Seguridad | [`03-seguridad-owasp.md`](03-seguridad-owasp.md) |
| Calidad y estrategia de pruebas | [`04-calidad-y-testing.md`](04-calidad-y-testing.md) y [`tests/AGENTS.md`](../tests/AGENTS.md) |
| Ejemplos de código y configuración | [`07-patrones-de-referencia.md`](07-patrones-de-referencia.md), que enlaza a ejemplos por área |
| JavaScript y vistas MVC | [`08-estandar-javascript.md`](08-estandar-javascript.md) y [`09-estandar-vistas-mvc.md`](09-estandar-vistas-mvc.md) |
| Despliegue | [`06-despliegue-azure.md`](06-despliegue-azure.md) |

La especificación funcional dentro de `LifeTrack/docs/` es la única fuente de verdad del producto. Si se requiere una copia en la raíz del repositorio, debe ser un enlace o una referencia a esa fuente, no una segunda especificación independiente.

## Contexto que acompaña cada cambio

- Vincula el cambio a los identificadores `RF-xx`, `RN-xx` y criterios de aceptación pertinentes. Si el comportamiento no está especificado, acláralo antes de implementarlo.
- Lee las instrucciones `AGENTS.md` de las capas que se modificarán y solo las guías técnicas que correspondan al cambio.
- Mantén la dirección de dependencias, el aislamiento de datos por usuario y las restricciones de seguridad descritas en las fuentes canónicas.
- No añadas paquetes NuGet sin aprobación. Verifica las APIs contra la documentación de la versión usada; no aceptes ejemplos no compilados como evidencia de que funcionan.
- Registra las decisiones de arquitectura duraderas como ADR y actualiza la especificación cuando cambie el comportamiento acordado.

## Verificación del proyecto

- Los tests se eligen por riesgo: fallos silenciosos, brechas de seguridad y regresiones. La cobertura se mide y reporta; no es una cuota que haya que rellenar.
- Para cambios de código, ejecuta `dotnet build`, `dotnet test` y `dotnet format --verify-no-changes`. Aplica también las comprobaciones de seguridad o de despliegue que correspondan al cambio.
- Para cambios solo documentales, revisa los enlaces y rutas afectados, las referencias cruzadas y el diff completo.
- No debilites autorización, anti-forgery, tests de arquitectura ni otras puertas para hacer pasar una implementación.

## Evidencia para el TFM

Conserva evidencia útil para explicar el desarrollo asistido: requisitos abordados, decisiones humanas, ADRs, errores relevantes detectados, controles que los previnieron y resultados de build y pruebas. Distingue claramente entre ejemplos de referencia, código verificado y código pendiente de compilar o probar.
