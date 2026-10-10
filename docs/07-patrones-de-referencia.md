# Patrones de referencia

Índice de ejemplos **canónicos** para adaptar al código real. No son archivos listos para copiar sin revisión: hay que aplicar las reglas de `AGENTS.md`, verificar las APIs contra la versión instalada y ejecutar las comprobaciones del proyecto.

## Estado de verificación

- **Domain:** compilado con .NET 10 SDK, `TreatWarningsAsErrors` y reglas de estilo activas, con cero warnings. La lógica de rachas y validaciones se comprobó con una consola de verificación.
- **Application, Infrastructure, Web y tests:** los ejemplos no pudieron restaurarse ni compilarse cuando se generaron por falta de acceso a NuGet. Hay que validarlos al incorporarlos; los ejemplos de MediatR reflejan la API documentada de v13.

## Cómo usar estos ejemplos

Consulta únicamente el documento que corresponde a la capa o tarea, adapta nombres y reglas de negocio, y verifica el resultado. Los bloques de código no incluyen comentarios, según `AGENTS.md` §6; la explicación está en la prosa adyacente. Los archivos de configuración conservan comentarios cuando son su único canal explicativo.

## Índice

| Área | Documento |
|---|---|
| Configuración de la solución | [`referencias/configuracion.md`](referencias/configuracion.md) |
| Domain | [`referencias/domain.md`](referencias/domain.md) |
| Application | [`referencias/application.md`](referencias/application.md) |
| Infrastructure | [`referencias/infrastructure.md`](referencias/infrastructure.md) |
| Web y MVC | [`referencias/web-mvc.md`](referencias/web-mvc.md) |
| Tests | [`referencias/tests.md`](referencias/tests.md) |
