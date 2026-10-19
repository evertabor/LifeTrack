# LifeTrack

Finanzas personales con dashboard inteligente. Proyecto final (TFM) de un máster de desarrollo con IA.
**.NET 10 · Clean Architecture · ASP.NET Core MVC · EF Core · Azure SQL (gratis) · OWASP Top 10:2025**

Este paquete contiene **solo la estructura de carpetas y los documentos de reglas**. El código lo genera la IA siguiendo esas reglas.

## Qué hay dentro

| Archivo | Contenido |
|---|---|
| [`AGENTS.md`](AGENTS.md) | Reglas para la IA: arquitectura, clean code, seguridad, EF, pruebas, receta, DoD y prohibiciones |
| `src/*/AGENTS.md`, `tests/AGENTS.md` | Reglas específicas de cada capa |
| [`.github/copilot-instructions.md`](.github/copilot-instructions.md) | Instrucciones específicas para Copilot, alineadas con `AGENTS.md` |
| [`docs/01-especificacion.md`](docs/01-especificacion.md) | Especificación funcional: requisitos, reglas de negocio, historias, RNF, modelo de datos, fases |
| [`docs/02-arquitectura.md`](docs/02-arquitectura.md) | Capas, dependencias, flujo de una petición, convenciones |
| [`docs/03-seguridad-owasp.md`](docs/03-seguridad-owasp.md) | OWASP Top 10:2025 → controles, reglas para la IA, verificación, checklist de PR |
| [`docs/04-calidad-y-testing.md`](docs/04-calidad-y-testing.md) | Clean code, puertas de calidad, estrategia de pruebas, CI |
| [`docs/05-flujo-con-ia.md`](docs/05-flujo-con-ia.md) | Contexto y controles del proyecto para el flujo gestionado con gentle-ai |
| [`docs/06-despliegue-azure.md`](docs/06-despliegue-azure.md) | Recursos gratuitos, App Settings, migraciones, CI/CD con OIDC |
| [`docs/07-patrones-de-referencia.md`](docs/07-patrones-de-referencia.md) | Índice de ejemplos por configuración, capa y tests |
| [`docs/08-estandar-javascript.md`](docs/08-estandar-javascript.md) | Patrón IIFE, constantes obligatorias, componentes compartidos, prohibiciones |
| [`docs/09-estandar-vistas-mvc.md`](docs/09-estandar-vistas-mvc.md) | Controllers, `ViewModels`, vistas sin lógica, parciales, layout, accesibilidad |
| [`docs/adr/`](docs/adr) | Decisiones de arquitectura |

## Estructura

```
src/LifeTrack.Domain          entidades y reglas (sin dependencias)
src/LifeTrack.Application     casos de uso e interfaces (solo Domain)
src/LifeTrack.Infrastructure  EF Core, Identity, repositorios
src/LifeTrack.Web             Controllers · ViewModels · vistas Razor y composición
tests/                        Domain.Tests · Application.Tests · Architecture.Tests
```

## Cómo empezar

### Opción A — con gentle-ai (recomendada)
Usa gentle-ai para orquestar el flujo de trabajo. Este repositorio aporta el contexto específico de LifeTrack: los requisitos están en `docs/01-especificacion.md`, la arquitectura en `docs/02-arquitectura.md` y las reglas globales y por capa en `AGENTS.md`.

La guía [`docs/05-flujo-con-ia.md`](docs/05-flujo-con-ia.md) reúne las decisiones y verificaciones del proyecto que acompañan ese flujo. No duplica sus fases ni sus prompts.

### Opción B — a mano (Fase 0)
```bash
dotnet new sln --format sln -n LifeTrack
dotnet new classlib -n LifeTrack.Domain         -o src/LifeTrack.Domain         -f net10.0
dotnet new classlib -n LifeTrack.Application    -o src/LifeTrack.Application    -f net10.0
dotnet new classlib -n LifeTrack.Infrastructure -o src/LifeTrack.Infrastructure -f net10.0
dotnet new mvc     -n LifeTrack.Web            -o src/LifeTrack.Web            -f net10.0
dotnet new xunit    -n LifeTrack.Domain.Tests       -o tests/LifeTrack.Domain.Tests       -f net10.0
dotnet new xunit    -n LifeTrack.Application.Tests  -o tests/LifeTrack.Application.Tests  -f net10.0
dotnet new xunit    -n LifeTrack.Architecture.Tests -o tests/LifeTrack.Architecture.Tests -f net10.0

dotnet add src/LifeTrack.Application    reference src/LifeTrack.Domain
dotnet add src/LifeTrack.Infrastructure reference src/LifeTrack.Application
dotnet add src/LifeTrack.Web            reference src/LifeTrack.Application src/LifeTrack.Infrastructure
dotnet add tests/LifeTrack.Domain.Tests       reference src/LifeTrack.Domain
dotnet add tests/LifeTrack.Application.Tests  reference src/LifeTrack.Application
dotnet add tests/LifeTrack.Architecture.Tests reference src/LifeTrack.Domain src/LifeTrack.Application src/LifeTrack.Infrastructure src/LifeTrack.Web

dotnet sln add (Get-ChildItem -Recurse src,tests -Filter *.csproj)    # PowerShell
# bash:  dotnet sln add $(find src tests -name '*.csproj')
```
Luego copia las plantillas de [`docs/referencias/configuracion.md`](docs/referencias/configuracion.md) (`global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `.gitignore`, `docker-compose.yml`) y quita los atributos `Version=` de los `PackageReference` generados por las plantillas (las versiones viven en `Directory.Packages.props`). Elimina los archivos de ejemplo (`Class1.cs`, `UnitTest1.cs`, páginas de plantilla).

### Entorno local (cuando haya código)
```bash
echo 'MSSQL_SA_PASSWORD=<contraseña-fuerte>' > .env   # el archivo .env no se sube (.gitignore)
docker compose up -d
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<cadena>" --project src/LifeTrack.Web
dotnet dev-certs https --trust
dotnet run --project src/LifeTrack.Web --launch-profile https
```
Las plantillas de configuración (incluido `.env.example`) están en [`docs/referencias/configuracion.md`](docs/referencias/configuracion.md).

## Principios no negociables
1. La especificación manda: nada se implementa si no está en `docs/01-especificacion.md`.
2. Las dependencias apuntan hacia dentro y un test lo comprueba.
3. Toda consulta filtra por el usuario actual.
4. Sin secretos en el repositorio; sin paquetes nuevos sin aprobación.
5. Nada se da por terminado sin `build + test + format`.
6. Las vistas solo renderizan: los datos llegan ya formateados desde el `ViewModel`.
7. En JavaScript, ningún selector ni literal fuera de los bloques de constantes.
8. Todo lo que ver con código en inglés; los textos visibles al usuario y la documentación en español.
