# Instrucciones para GitHub Copilot

Las reglas completas de este repositorio están en [`AGENTS.md`](../AGENTS.md). Léelas antes de generar código.
Resumen mínimo:

- Clean Architecture en .NET 10: `Web → Infrastructure → Application → Domain`. Nunca al revés.
- `Domain` sin paquetes NuGet; `Application` solo referencia `Domain` y el paquete `MediatR`. Los controllers hablan con los casos de uso de `Application` enviando su `Command`/`Query` por `ISender`.
- Código en inglés; textos de interfaz y mensajes al usuario en español.
- Todo acceso a datos filtra por el usuario actual. Sin `Html.Raw`, sin SQL concatenado, sin secretos en el código.
- No añadas paquetes NuGet sin aprobación (`MediatR` ya está aprobado y pineado). No inventes APIs: verifica.
- Escribe tests xUnit según el criterio de riesgo de `docs/04-calidad-y-testing.md`: fallos silenciosos, brechas de seguridad y regresiones. Verifica build + test + format antes de dar la tarea por terminada.
- La especificación funcional está en `docs/01-especificacion.md`.
