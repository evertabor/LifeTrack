# AGENTS.md — LifeTrack

Reglas para cualquier agente de IA y para las personas que trabajen en este repositorio.
Si una instrucción del usuario contradice este archivo, **avisa del conflicto antes de actuar**.

## 1. Qué es este proyecto

Aplicación web personal para **finanzas personales y hábitos** con un dashboard que cruza ambos mundos.
Es el proyecto final (TFM) de un máster de desarrollo con IA, pensado para desplegarse **gratis en Azure**.

- **Stack:** .NET 10 (LTS), ASP.NET Core MVC (vistas Razor), EF Core, SQL Server (Azure SQL free offer), ASP.NET Core Identity.
- **Arquitectura:** Clean Architecture (ver `docs/02-arquitectura.md`).
- **Idiomas:** **inglés** para todo lo que ver con código: identificadores, namespaces, nombres de archivo y de test, migraciones, mensajes de commit, clases CSS, atributos `data-*`, identificadores JavaScript y URLs. **Español** para los textos visibles al usuario y para **toda la documentación**.

## 2. Antes de escribir código (obligatorio)

1. Lee `docs/01-especificacion.md`: es la **fuente de verdad** de qué construir. No implementes nada que no esté ahí.
2. Lee `docs/02-arquitectura.md` y el `AGENTS.md` de cada capa donde vayas a trabajar. Consulta además la guía pertinente: seguridad (`docs/03-seguridad-owasp.md`), calidad y pruebas (`docs/04-calidad-y-testing.md`), vistas (`docs/09-estandar-vistas-mvc.md`), JavaScript (`docs/08-estandar-javascript.md`) o el índice de ejemplos (`docs/07-patrones-de-referencia.md`).
3. Si un requisito es ambiguo, **pregunta o declara tu suposición en el plan**. No inventes reglas de negocio.
4. Propón un **plan corto** (archivos que tocarás, tests que escribirás) y espera confirmación en cambios de más de ~3 archivos.
5. Trabaja en **rebanadas verticales** (una funcionalidad completa de dominio a UI), no capa por capa. Receta en la sección 9.

## 3. Estructura de la solución

```
LifeTrack/
├─ AGENTS.md  README.md
├─ .github/copilot-instructions.md  # entrada específica para Copilot
├─ docs/                      # fuentes funcionales, estándares, referencias y ADRs
│  ├─ referencias/             # ejemplos separados por configuración, capa y tests
│  └─ adr/                     # decisiones de arquitectura
├─ src/
│  ├─ LifeTrack.Domain/       # entidades, reglas de negocio. SIN dependencias
│  ├─ LifeTrack.Application/  # casos de uso (contratos + handlers), interfaces. Depende de Domain y de MediatR
│  ├─ LifeTrack.Infrastructure/ # EF Core, repositorios, Identity, reloj. Depende de Application
│  └─ LifeTrack.Web/          # Controllers, ViewModels, vistas Razor, composición, seguridad HTTP
│     ├─ Controllers/         # un controller por área
│     ├─ ViewModels/          # un modelo por pantalla, solo para pintar
│     ├─ Views/               # vistas, parciales y layout
│     ├─ TagHelpers/  Security/  Composition/  Extensions/  Services/
│     └─ wwwroot/             # css, js, lib
└─ tests/
   ├─ LifeTrack.Domain.Tests/
   ├─ LifeTrack.Application.Tests/
   └─ LifeTrack.Architecture.Tests/   # hace cumplir la regla de dependencias
```

`LifeTrack.Web` sigue **ASP.NET Core MVC con vistas Razor**. Detalle completo de la capa de
presentación en `docs/09-estandar-vistas-mvc.md`; patrón de JavaScript en `docs/08-estandar-javascript.md`.

## 4. Regla de dependencias (la más importante)

```
Web ──► Infrastructure ──► Application ──► Domain
 └───────────────────────► Application
```

- Las flechas apuntan **hacia dentro**. Una capa interna **nunca** referencia una externa.
- `Domain`: cero paquetes NuGet, cero referencias a proyectos.
- `Application`: solo referencia a `Domain` y al paquete **`MediatR`** (`13.*`, pineado en `Directory.Packages.props`), que es lo único que referencia. Ni EF, ni ASP.NET, ni logging.
- `Infrastructure`: implementa las interfaces definidas en `Application`. No referencia `Web`.
- `Web`: los **controllers** hablan **solo con casos de uso de Application**, mediante `ISender` de MediatR, nunca con `DbContext` ni repositorios. `Web` **no referencia `Domain`**: los `ViewModels` se construyen desde los DTOs de Application, nunca desde entidades. Las **excepciones de frontera** (`BusinessRuleException`, `ResourceNotFoundException`) viven en `Application` y son lo único que un controller captura.
- `LifeTrack.Architecture.Tests` falla el build si se rompe cualquiera de estas reglas. **Nunca los desactives ni los debilites para que pasen.**

| Capa | Qué va aquí | Qué NO va aquí |
|---|---|---|
| Domain | Entidades, agregados, enums, `DomainException`, invariantes | EF, atributos de persistencia, HTTP, DTOs, async, I/O |
| Application | Casos de uso (`ICommand<T>` / `IQuery<T>` / `IRequest` + `IRequestHandler`), DTOs, interfaces (`I…Repository`, `IUnitOfWork`, `ICurrentUser`), `ApplicationAssemblyAnchor` | `DbContext`, `IQueryable`, `HttpContext`, Identity, paquetes NuGet distintos de `MediatR` |
| Infrastructure | `DbContext`, configuraciones EF, migraciones, repositorios, Identity, reloj con zona horaria | Reglas de negocio, lógica de UI |
| Web | Controllers, ViewModels, vistas Razor, `TagHelpers`, cabeceras de seguridad, `Program.cs`, `CurrentUserService` | Lógica de negocio, consultas, acceso directo a datos, entidades de `Domain` en la vista |

## 5. Comandos

```bash
dotnet restore
dotnet build -c Release                      # en CI los warnings son errores (CI=true)
dotnet test
dotnet format --verify-no-changes            # estilo; usa `dotnet format` para corregir
dotnet run --project src/LifeTrack.Web --launch-profile https
dotnet ef migrations add <Nombre> --project src/LifeTrack.Infrastructure --startup-project src/LifeTrack.Web --output-dir Persistence/Migrations
dotnet list package --vulnerable --include-transitive
```

Las **versiones** viven solo en `Directory.Packages.props` (CPM); ningún `.csproj` lleva `Version`.
`MediatR` está pineado ahí como `13.*`. Si necesitas referenciar un paquete nuevo, se declara su
`PackageVersion` en `Directory.Packages.props` y se añade la referencia sin versión en el proyecto:

```bash
dotnet add src/LifeTrack.Application package MediatR    # sin --version: la versión la pone el CPM
```

Antes de dar una tarea por terminada, ejecuta **build + test + format**. No digas "funciona" sin haberlo ejecutado.

## 6. Clean code — reglas obligatorias

**Nombres y estructura**
- Nombres que revelan intención (`CheckInHabitHandler`, no `HabitService2`). Sin abreviaturas crípticas.
- `PascalCase` para tipos y miembros públicos, `camelCase` para parámetros y locales, `_camelCase` para campos privados, prefijo `I` en interfaces.
- Namespaces *file-scoped* (`namespace X;`) y `using` fuera del namespace.
- Un tipo público por archivo. Los casos de uso se organizan por feature o agregado, y cada comando o caso de uso tiene su propia carpeta.
- El comando y su handler correspondiente viven juntos, en archivos separados, dentro de la carpeta del caso de uso bajo `Commands/` de su feature.
- Las excepciones compartidas de dominio viven en `Domain/Common/Exceptions` y las excepciones de Application en `Application/Common/Exceptions`.
- Los contratos compartidos de Application, como `ICommand` e `IQuery`, viven en `Application/Common/Contracts`.
- Handlers: `sealed`, nombre `<Verbo><Sustantivo>Handler`, e implementan `IRequestHandler<TRequest, TResponse>` o `IRequestHandler<TRequest>` cuando no devuelven nada (en ese caso el método devuelve `Task`, no `Task<Unit>`). El método se llama `Handle(request, CancellationToken)`.
- Contratos de casos de uso: `record` que implementa `ICommand<T>` (escritura con resultado), `IQuery<T>` (lectura) o `IRequest` (escritura sin resultado).

**Funciones y clases**
- Una responsabilidad por clase y por método (SRP). Métodos de ~20 líneas o menos; más de 30 es señal de que debe dividirse.
- Máximo 4 parámetros; si hay más, usa un `record`. **Salvedad:** un controller ya no inyecta un handler por acción, sino `ISender` y `ILogger<TController>`, así que la regla ya no aprieta ahí. Se mantiene para el resto de métodos.
- *Guard clauses* y retorno temprano en lugar de `if` anidados (máximo 2 niveles).
- Sin números ni cadenas mágicas: constantes con nombre (`MaxAmount`, `MaxRowsInFile`), definidas junto al código que las usa.
- Prefiere inmutabilidad: `record` para DTOs y comandos, `private set` en entidades, `IReadOnlyCollection<T>` para exponer colecciones.
- No dupliques lógica (DRY), pero no abstraigas por adelantado (YAGNI): sin interfaces ni capas "por si acaso". Aplica la guarda de diseño de `docs/04-calidad-y-testing.md`.

**§ Constructores primarios para DI**
- Toda clase inyectada por DI en las capas Web, Application o Infrastructure que no tenga restricciones de framework **debe** usar constructor primario de C# 12.
- Excepciones expresas que **no** deben recibir constructor primario:
  - `DbContext` y tipos que heredan de `IdentityDbContext` — requieren `base(options)` explícito.
  - Entidades de dominio — tienen fábrica estática `Create(...)` y constructor privado sin parámetros para EF.
  - Excepciones de dominio y aplicación (`DomainException`, `NotFoundException`, `BusinessRuleException`, `ResourceNotFoundException`).
  - Migraciones EF generadas.
- Los input models de screen (formularios) viven siempre bajo `ViewModels/<Area>/` y nunca como clases anidadas dentro de un controller.

**Cero comentarios**
- **Cero comentarios en el código.** Sin comentarios de documentación XML, sin comentarios de línea (`//`), sin comentarios de bloque (`/* */`), sin `@* *@` en Razor, sin `TODO`.
- El código se explica con **nombres, tipos y estructura**. Un comentario que repite lo que el código dice es ruido con formato: en C# el lenguaje es expresivo y el compilador ya documenta la forma, así que el comentario no aporta información, solo ocupa líneas y se queda obsoleto en cuanto el código cambia.
- Si una restricción **no se puede expresar en el código**, se documenta en la documentación, no en un comentario: la razón va en el documento (`docs/`) o en el nombre del tipo, y el código queda limpio.
- Un comentario solo se justifica cuando documenta una decisión que el código **no puede defender solo** — una restricción de plataforma, un *workaround*, un requisito externo — y en ese caso va en `docs/`, referenciado desde el punto exacto donde aplica, nunca pegado encima.
- Excepción acotada: los ficheros de configuración que no son código (`.editorconfig`, `.gitignore`, `.env.example`, `docker-compose.yml`) conservan sus comentarios `#`, porque es el único canal de documentación de esos formatos.

**C# moderno y robusto**
- `Nullable` activado: sin `!` (null-forgiving). Si hace falta, la razón se documenta en `docs/`. Un warning es un error.
- `async`/`await` de punta a punta, con `CancellationToken` como **último** parámetro. Prohibido `.Result`, `.Wait()`, `async void` y `Task.Run` para "hacer async" código síncrono.
- Tiempo: usa `TimeProvider` (nunca `DateTime.Now` / `DateTime.UtcNow` en lógica). "Hoy" es la fecha en la zona `App:TimeZoneId`.
- Dinero: `decimal` con 2 decimales; nunca `double`/`float`.
- Fechas sin hora: `DateOnly`.
- LINQ legible; sin consultas N+1; en lecturas usa `AsNoTracking()`.
- No captures `catch (Exception)` para silenciar. Capturas específicas, y nunca un `catch` vacío.

**Errores**
Dos capas, cuatro excepciones. `Domain` lanza las suyas; `Application` las traduce a las suyas; `Web` solo captura las de `Application`.

| Excepción | Capa | Significado | Mensaje |
|---|---|---|---|
| `DomainException` | `Domain` | Regla de negocio violada | En español y **seguro de mostrar** |
| `NotFoundException` | `Domain` | El recurso no existe **o no es del usuario actual** (se responde igual en ambos casos) | Genérico |
| `BusinessRuleException` | `Application` | Frontera: regla de negocio violada | En español y **seguro de mostrar** |
| `ResourceNotFoundException` | `Application` | Frontera: el recurso no existe **o no es del usuario actual** | **Genérico** |

- **Cada handler traduce** las excepciones del dominio dentro de un `try`/`catch`: captura `DomainException` y `NotFoundException` y relanza `BusinessRuleException` o `ResourceNotFoundException` con el mensaje. Traducción explícita, sin *helpers*, sin pipeline de excepciones y sin decoradores. MediatR ofrece `IRequestExceptionHandler<,,>` para eso, pero **no se adopta**: sigue siendo la opción futura (ADR-0004).
- Un controller captura **`BusinessRuleException` y `ResourceNotFoundException`, nunca las de `Domain`**. La primera va a `ModelState` o `TempData`; la segunda, a un `LogWarning` con plantilla estructurada más un mensaje genérico.
- Cualquier otra excepción es un fallo inesperado: se registra y se muestra una página de error genérica. Nunca expongas `ex.Message` de excepciones desconocidas.
- Patrón canónico de casos de uso en `docs/referencias/application.md`; el resto de ejemplos está indexado en `docs/07-patrones-de-referencia.md`.

**Vistas Razor** (norma completa en `docs/09-estandar-vistas-mvc.md`)
- Una vista **solo renderiza**. Cero lógica: sin ternarios, sin declaraciones de variables, sin llamadas que calculen o formateen, sin concatenación para construir texto.
- `@if` y `@else` permitidos **solo para elegir qué se renderiza**, nunca para calcular.
- Los importes, las fechas y los rótulos llegan **ya formateados** en el `ViewModel`. Si necesitas escribir `Model.Amount.ToString("C2")` en una vista, lo que falta está en el `ViewModel`.
- Un `ViewModel` existe únicamente para pintar: se construye con una fábrica estática `From(...)` a partir del resultado del handler y **no hereda** de nada de `Domain` ni de Application.
- Toda vista es semántica, lleva `<label for>` en cada campo y es navegable por teclado. Texto visible en español, identificadores en inglés.

**JavaScript** (norma completa en `docs/08-estandar-javascript.md`)
- Todo script es una IIFE. `_` marca una función **privada**; lo público va sin `_`. Un `_` nunca se expone en el `return`.
- Selectores, clases, ids, claves `data-*` y valores literales viven **solo** en los bloques `SELECTORS`, `CSS_CLASSES` y `CONSTANTS`, al principio del fichero. **Nunca** en línea ni dentro de una función.
- Un único punto de entrada `_initialize()`, solo en scripts de página.
- Clases CSS y atributos `data-*` llevan prefijo de feature (`app-` para componentes compartidos).
- Sin `var`, sin `<script>` en línea, sin CDN.

SOLID se aplica letra por letra contra este repositorio, con las trampas y la guarda anti-sobre-ingeniería desarrolladas en `docs/04-calidad-y-testing.md`. Solo la inversión de dependencias se verifica automáticamente mediante los tests de arquitectura; las otras letras requieren revisión de diseño.

## 7. Seguridad — reglas obligatorias (OWASP Top 10:2025)

Detalle y verificación en `docs/03-seguridad-owasp.md`. Resumen para escribir código:

1. **A01 Control de acceso:** política de autorización *fallback* = usuario autenticado (denegar por defecto). **Todo** acceso a datos filtra por `ICurrentUser.UserId` dentro de la consulta. Un id de otro usuario devuelve "no encontrado". Cada handler que recibe un id necesita un test de IDOR.
2. **A02 Configuración:** secretos fuera del repositorio (`dotnet user-secrets` en local, App Settings en Azure). Cabeceras de seguridad y CSP activas. `UseExceptionHandler` + `UseHsts` en producción. `AllowedHosts` restringido. Migraciones automáticas **solo** en Development.
3. **A03 Cadena de suministro:** versiones solo en `Directory.Packages.props`. `MediatR` (`13.*`) está aprobado y pineado. **No añadas otros paquetes NuGet sin pedir aprobación** y sin comprobar que existen y están mantenidos (la IA inventa nombres de paquetes). `dotnet list package --vulnerable` limpio.
4. **A04 Criptografía:** nunca implementes cifrado ni hashing propio; usa Identity (hasher de contraseñas) y Data Protection. HTTPS siempre. Cookies `HttpOnly`, `Secure`, `SameSite`.
5. **A05 Inyección:** EF Core con LINQ (parametrizado). Prohibido concatenar SQL; si necesitas SQL crudo, `FromSql($"...")` interpolado. Razor codifica por defecto: **prohibido `Html.Raw`** con datos de usuario y `<script>` en línea. Valida entradas (DataAnnotations en el `InputModel` del controller, invariantes en el dominio).
6. **A06 Diseño inseguro:** modela invariantes en el dominio; limita tasa de peticiones; enlaza formularios a modelos de entrada dedicados (`InputModel`), **nunca** a entidades (evita *over-posting*).
7. **A07 Autenticación:** contraseña ≥ 12 caracteres, bloqueo por intentos fallidos, mensajes de login genéricos. No implementes autenticación a mano.
8. **A08 Integridad:** anti-forgery en **todos** los POST. En MVC **no** es automático: `Program.cs` registra `AutoValidateAntiforgeryToken` como filtro global y `LifeTrack.Architecture.Tests` verifica que sigue registrado. Los formularios usan tag Helpers (`asp-for`, `asp-action`) para emitir el token. **Prohibido `[IgnoreAntiforgeryToken]`** salvo en la página de error, y prohibido desactivarlo "para que algo funcione". Sin deserialización insegura (`BinaryFormatter` prohibido). Workflows de CI con permisos mínimos.
9. **A09 Registro y alertas:** `ILogger` con plantillas estructuradas (`"Habit {HabitId} not found"`), nunca interpolación de cadenas. Registra eventos de seguridad (acceso denegado, ids manipulados). **Nunca** registres contraseñas, tokens, cadenas de conexión ni datos personales innecesarios.
10. **A10 Condiciones excepcionales:** falla cerrado (sin identidad → sin datos), sin `catch` vacíos, `CancellationToken` en todo I/O, reintentos solo ante errores transitorios de BD.

**Si añades funciones de IA** (p. ej. categorizar gastos con un LLM): trata la salida del modelo como **entrada no confiable** (valida y codifica), no envíes datos personales innecesarios, aísla instrucciones de datos del usuario (*prompt injection*), no des al modelo más permisos que el mínimo, y mantén un respaldo determinista (reglas por palabra clave). Ver sección de OWASP LLM en `docs/03-seguridad-owasp.md`.

## 8. Datos y EF Core

- Cada entidad tiene su `IEntityTypeConfiguration<T>` en `Infrastructure/Persistence/Configurations`.
- Los ids se generan en el **dominio** (`Guid.NewGuid()`); configura siempre `ValueGeneratedNever()`. Sin eso EF marca como "existente" a un hijo nuevo y falla al guardar.
- Entidades con constructor privado sin parámetros (para EF), `private set` y fábrica estática `Create(...)`.
- Colecciones expuestas como `IReadOnlyCollection<T>` sobre un `List<T>` privado, con `PropertyAccessMode.Field`.
- Define `HasMaxLength`, `HasPrecision(18, 2)` para dinero, índices por `UserId` e índices únicos para invariantes (p. ej. un check-in por hábito y día).
- Los repositorios **no** llaman a `SaveChangesAsync`; lo hace el handler vía `IUnitOfWork`.
- Cada entidad del usuario tiene FK a `AspNetUsers` con borrado en cascada (derecho al olvido).
- Una migración por cambio de modelo, con nombre descriptivo. No edites migraciones ya aplicadas.

## 9. Receta: añadir una funcionalidad (rebanada vertical)

1. Localiza el requisito en `docs/01-especificacion.md` (RF-xx) y sus criterios de aceptación.
2. **Domain:** entidad/método con sus invariantes + **tests de dominio primero** (rojo → verde).
3. **Application:** interfaz de repositorio si hace falta, y el caso de uso en un archivo: el contrato `record` que implementa `ICommand<T>`, `IQuery<T>` o `IRequest`, sus DTOs de salida y el handler `sealed` que implementa `IRequestHandler<TRequest, TResponse>` o `IRequestHandler<TRequest>`, con `Handle(request, CancellationToken)`. Los tests envían el contrato por `ISender` con *fakes* en memoria. El handler **traduce las excepciones**: captura `DomainException`/`NotFoundException` y relanza `BusinessRuleException`/`ResourceNotFoundException`, así ninguna excepción de `Domain` sale de la capa. Incluye el test de IDOR si recibe un id (espera `ResourceNotFoundException`).
4. **Infrastructure:** configuración EF, repositorio, migración.
5. **Web:** controller delgado que inyecta **`ISender`** (y `ILogger<TController>`) y llama a `sender.Send(new <Verbo><Sustantivo>Command/Query(...), cancellationToken)`; nunca inyecta un handler concreto, ni `DbContext`, ni repositorios. El `InputModel` valida presencia y formato, y el controller captura `BusinessRuleException`/`ResourceNotFoundException`, nunca excepciones de `Domain`. `ViewModel` construido con su fábrica `From(...)` a partir del DTO devuelto, y vista Razor **sin lógica** (`<label>` en cada campo, mensajes de validación). Sigue `docs/09-estandar-vistas-mvc.md`.
6. Ejecuta build + test + format. Revisa la lista de la sección 11.
7. Si tomaste una decisión de arquitectura, añade un ADR en `docs/adr/`.
8. Commit pequeño con *Conventional Commits* (`feat(habits): add check-in`).

## 10. Pruebas

Aplica el criterio de riesgo de `docs/04-calidad-y-testing.md`: prueba fallos silenciosos, brechas de seguridad y regresiones; no escribas tests solo para aumentar cobertura. `tests/AGENTS.md` define las convenciones específicas por proyecto. La cobertura exigida por `RNF-06` se mide y se reporta, no se persigue como objetivo.

## 11. Definition of Done (revisa antes de entregar)

- [ ] Cumple el requisito y los criterios de aceptación de `docs/01-especificacion.md`.
- [ ] `dotnet build`, `dotnet test` y `dotnet format --verify-no-changes` pasan sin warnings.
- [ ] Los tests de arquitectura pasan y no se tocó su configuración.
- [ ] Los tests nuevos cubren los riesgos que introduce el cambio: fallo silencioso, brecha de seguridad o regresión. No se exige un test por cada rama.
- [ ] Toda consulta filtra por usuario; ningún secreto ni dato real en el repositorio.
- [ ] Sin paquetes nuevos (o aprobados explícitamente).
- [ ] Mensajes al usuario en español, formularios accesibles, sin `Html.Raw`.
- [ ] Si tocaste la capa Web: controller delgado, `ViewModel` con fábrica `From(...)`, y vistas sin lógica. Ninguna entidad de `Domain` llega a la vista.
- [ ] Si tocaste JavaScript: IIFE, `_` solo en funciones privadas, y ningún selector o literal fuera de los bloques de constantes.
- [ ] Anti-forgery: el filtro global `AutoValidateAntiforgeryToken` sigue registrado y ningún POST se ha excluido.
- [ ] Cero comentarios en el código: ni documentación XML, ni comentarios de línea, ni de bloque, ni `@* *@` en Razor, ni `TODO`. Sin código muerto; la documentación afectada está actualizada.

## 12. Prohibiciones (NUNCA)

- Referenciar capas externas desde capas internas o saltarse `Application` desde los controllers.
- **Escribir cualquier comentario en el código**: documentación XML, comentarios de línea, comentarios de bloque, `@* *@` en Razor o `TODO`. Si la restricción no se puede expresar en el código, la razón va en `docs/`.
- Que un controller o un `ViewModel` capturen o importen excepciones de `Domain`: solo `BusinessRuleException` y `ResourceNotFoundException`, las de `Application`. Y que un handler deje salir `DomainException` o `NotFoundException` sin traducir.
- Poner lógica de negocio en un controller, un `ViewModel`, un repositorio o una configuración EF.
- Exponer entidades de dominio a la vista, a un parcial o a un `ViewModel`, o enlazarlas con `[BindProperty]`.
- **Poner lógica en una vista**: ternarios, declaraciones de variables, llamadas que calculen o formateen, concatenación para construir texto. Las vistas solo renderizan datos que el `ViewModel` ya dejó listos (sección 7 y `docs/09-estandar-vistas-mvc.md`).
- Consultar datos sin filtrar por el usuario actual.
- Desactivar anti-forgery, CSP, autorización o los tests de arquitectura "para que funcione".
- Hardcodear secretos, cadenas de conexión o contraseñas; subir `.env`, `secrets.json` o datos reales.
- Añadir paquetes NuGet, `AutoMapper` u otras librerías sin aprobación. `MediatR` sí está aprobado y pineado (`13.*`), y su alcance es el descrito en ADR-0004.
- **Hardcodear la clave de licencia de MediatR** (`MediatR:LicenseKey`) en código, en `appsettings.json` o en el repositorio. Va por configuración y **solo es necesaria en producción**.
- Inventar APIs, paquetes o parámetros: si no estás seguro, **verifica en la documentación oficial** o dilo.
- Reescribir archivos completos para un cambio pequeño: haz ediciones mínimas y localizadas.
- Ejecutar migraciones automáticas en producción.
- En JavaScript: selectores, clases, ids o `data-*` fuera de los bloques `SELECTORS` / `CSS_CLASSES` / `CONSTANTS`, y métodos públicos con `_` al principio (`docs/08-estandar-javascript.md`).

## 13. Cuando dudes

Pregunta. Una pregunta corta cuesta menos que una rebanada entera construida sobre una suposición equivocada.
