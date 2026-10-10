# Calidad de código y pruebas

Complementa la sección 6 y 10 de `AGENTS.md` con el porqué y las puertas automáticas.

## 1. Principios de código limpio aplicados

| Principio | Cómo se concreta en LifeTrack |
|---|---|
| **Nombres que revelan intención** | `CheckInHabitHandler`, `ResourceNotFoundException`, `Habit.CurrentStreak` |
| **SRP** | Entidad = reglas; handler `IRequestHandler` = orquestación; repositorio = acceso a datos; controller = traducción HTTP; `ViewModel` = datos listos para pintar; vista = render |
| **Funciones pequeñas** | ~20 líneas por método; más de 30 se divide |
| **Pocos parámetros** | Máximo 4; si no, `record`. Un controller ya no inyecta un handler por acción, sino `ISender` y `ILogger<TController>` |
| **Guard clauses** | Validar y salir temprano; máximo 2 niveles de anidación |
| **Sin magia** | Constantes con nombre; sin cadenas ni números sueltos |
| **Inmutabilidad** | `record` para comandos/DTOs, `private set` en entidades |
| **DRY sin sobre-abstraer** | Sin interfaces "por si acaso" (YAGNI). Aplicar la guarda de diseño de esta guía antes de extraer una abstracción |
| **Cero comentarios** | Ni XML, ni `//`, ni `/* */`, ni `@* *@`, ni `TODO`. El código se explica con nombres, tipos y estructura; la razón que el código no puede defender va en `docs/` |
| **Tell, don't ask** | `habit.CheckIn(...)` en vez de manipular su lista desde fuera |
| **Fallar rápido** | Invariantes validados en la creación; excepciones tipadas |

SOLID se aplica letra por letra contra este repositorio, con sus trampas concretas. **Solo D es exigible por herramienta**: el compilador y `LifeTrack.Architecture.Tests` comprueban el sentido de las dependencias; SRP, OCP, ISP y LSP se evalúan mediante revisión de diseño.

### SOLID aplicado a LifeTrack

**S — Responsabilidad única.** Significa una razón para cambiar, no un método por clase. Si un cambio en el modelo de datos obliga a tocar el controller, las responsabilidades están mal separadas: el controller transporta HTTP, no conoce tablas. No dividas un handler pequeño en `Validator`, `Mapper` y `Service` sin una razón independiente para cada tipo.

**O — Abierto/cerrado.** Añadir un caso de uso debe ser un archivo nuevo, no un `switch` que crece. MediatR permite que un controller envíe un nuevo contrato por `ISender` sin añadir una dependencia por handler. Evita también sustituir un `switch` sencillo por jerarquías de herencia innecesarias.

**L — Sustitutibilidad.** Cada implementación debe respetar el contrato de la interfaz. Si `IHabitRepository` promete devolver únicamente recursos del usuario, el repositorio real y el fake deben filtrar por el mismo `userId`; de lo contrario, el test puede pasar y producción quedar expuesta a IDOR.

**I — Segregación de interfaces.** Diseña la interfaz para quien la consume. `ICurrentUser` expone el identificador que necesita Application, no el `ClaimsPrincipal` completo. Evita interfaces generales que obliguen a consumidores y fakes a implementar operaciones que no usan.

**D — Inversión de dependencias.** Las interfaces pertenecen a la capa que las consume. `IHabitRepository` vive en Application porque esa capa define el contrato que Infrastructure implementa; así las dependencias apuntan hacia dentro.

### Guarda anti-sobre-ingeniería

Antes de extraer una abstracción, responde sí a la primera pregunta:

1. ¿Existe una segunda implementación real o una necesidad concreta de test? No "por si acaso".
2. ¿El nombre aporta precisión o solo longitud?
3. ¿La separación responde a una responsabilidad distinta o solo a una preferencia estética?
4. ¿La abstracción evita filtrar EF, HTTP u otro concepto externo hacia Application?

No crear una capa `IHabitService` que envuelva el handler, un `IHabitValidator` si la validación pertenece al dominio, un `IHabitMapper` para una proyección pequeña ni un behavior de MediatR sin una necesidad concreta. La testabilidad no sustituye la escritura de un test: un fake pequeño puede cubrir el caso sin introducir una abstracción permanente.

## 2. Puertas de calidad automáticas

| Puerta | Herramienta | Cuándo |
|---|---|---|
| Compilación sin warnings | `Directory.Build.props`: `TreatWarningsAsErrors` cuando `CI=true` | CI |
| Estilo coherente | `.editorconfig` + `dotnet format --verify-no-changes` | CI |
| Nulabilidad | `<Nullable>enable</Nullable>` | Siempre |
| Seguridad estática | Analizadores CA2100, CA3xxx, CA5xxx (en `.editorconfig`) + CodeQL | CI |
| Vulnerabilidades NuGet | `NuGetAudit` + `dotnet list package --vulnerable --include-transitive` (incluye `MediatR` y su grafo transitivo) | CI |
| Arquitectura | `LifeTrack.Architecture.Tests` | CI |
| Pruebas | `dotnet test` con cobertura (`coverlet`) | CI |

Plantillas de `Directory.Build.props`, `Directory.Packages.props` y `.editorconfig` en [`docs/referencias/configuracion.md`](referencias/configuracion.md).

## 3. Estrategia de pruebas

```
             ▲  pocas   Integración (WebApplicationFactory + SQLite en memoria)
            ╱ ╲
           ╱   ╲        Application: casos de uso enviados por ISender con fakes en memoria
          ╱     ╲
         ╱       ╲      Domain: reglas puras (rápidas y numerosas)
        ▔▔▔▔▔▔▔▔▔▔▔  muchas
```

La pirámide no se describe por **cantidad** de tests, sino por **dónde está el riesgo**: el grueso
vive donde un fallo sería silencioso (dominio y casos de uso, rápidos y sin I/O), y la integración es
escasa porque es lenta y cara de mantener.

**Criterio de riesgo.** Se prueba cuando el fallo sería **silencioso**, una **brecha de seguridad**, o
una **regresión ya ocurrida**. No se escribe un test por cada rama ni por cada criterio de aceptación.

| Categoría | Qué es | Cómo se prueba |
|---|---|---|
| **Fallo silencioso** | Un número incorrecto que se muestra al usuario y nadie detecta: una racha mal calculada, un total que no cuadra | Un `Assert` sobre el valor exacto |
| **Brecha de seguridad** | Cada handler que recibe un id | Test de IDOR que espera `ResourceNotFoundException` **y comprueba que no se persistió nada** (`SaveCount == 0`) |
| **Regresión** | Cada bug ya corregido | El test que lo reproduce, escrito primero |

Un test de IDOR que solo mira la excepción no verifica que el sistema quedó consistente: la brecha
puede dejar un registro a medias y el test pasa.

### Qué no se prueba

| No se prueba | Por qué |
|---|---|
| **Plumbing de framework**: que MediatR resuelva el contrato al handler, que un repositorio implemente su interfaz, que DI registre el servicio | Eso lo comprueban los tests de arquitectura, no un test unitario |
| **Proyecciones triviales**: un `record` DTO, un `From(...)` que copia campos sin transformar | No hay decisión: no puede fallar de forma interesante |
| **Paso a paso**: un handler que solo reenvía a otro caso de uso | El test del caso de uso destino ya lo cubre |
| **Renderizado de vistas**: una vista Razor | Si hay lógica en la vista, el fallo es que esa lógica existe en la vista, y lo detecta la revisión |
| **Getters y setters** de entidades sin invariantes | Un *getter* no puede fallar; lo que importa es el invariante que lo rodea, y ese sí se prueba |

### Por proyecto

- **Domain.Tests:** todo invariante cuya violación produce un número o un estado incorrecto que el
  usuario ve sin notar. Casos válidos, cada violación y los bordes.
- **Application.Tests:** los casos de uso con riesgo, **enviando su contrato por `ISender`** (MediatR)
  con *fakes* en memoria. Camino feliz, cada excepción **de frontera** que el caso de uso puede producir
  de verdad, y **IDOR** para los que reciben un id. También que **no se guardó nada** cuando falla.
- **Architecture.Tests:** reglas de dependencia ejecutables. Es la puerta que hace exigible la **D** de
  SOLID; las otras cuatro letras son criterio de revisión.
- **Integración (opcional, fase 6):** acceso anónimo redirige al login; cabeceras de seguridad presentes; un flujo completo (registro → crear hábito → marcar).

### Cómo se prueba un caso de uso

Opción **preferida**: enviar el contrato por `ISender` con `AddMediatR` sobre un `ServiceCollection`
real y los *fakes* en memoria registrados.

```csharp
var services = new ServiceCollection();
services.AddSingleton<ICurrentUser>(new FakeCurrentUser(userId));
services.AddSingleton(unitOfWork);
services.AddSingleton<IHabitRepository>(habits);
services.AddSingleton(TimeProvider.System);
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyAnchor).Assembly));

var sender = services.BuildServiceProvider().GetRequiredService<ISender>();
var id = await sender.Send(new CreateHabitCommand("Meditar"), CancellationToken.None);
```

Por qué esta opción: es **exactamente lo que hace un controller**, así que el test cubre el enlace
contrato → handler y un contrato mal declarado (o un handler que no se registra) falla en el propio
test en vez de en producción.

Opción alternativa: construir el handler directamente y llamar a `Handle(request, cancellationToken)`.
Sigue siendo válida y es más rápida cuando el test se centra en un único handler y no necesita el
dispatcher; no cubre el enlace contrato → handler.

En los dos casos: las **excepciones que se esperan son las de frontera** (`BusinessRuleException`,
`ResourceNotFoundException`). `DomainException` y `NotFoundException` solo se esperan en los tests de
**dominio**, que es donde nacen.

### Convenciones
- Nombre: `Método_Condición_ResultadoEsperado` (p. ej. `CheckIn_InTheFuture_Throws`).
- AAA: Arrange / Act / Assert. Un concepto por test.
- Dobles: *fakes* a mano (`FakeCurrentUser`, `FakeUnitOfWork`, `FixedTimeProvider`, `InMemory…Repository`). Sin librerías de mocking salvo aprobación.
- Determinismo: reloj fijo; sin `Thread.Sleep`; sin red; sin base de datos real en pruebas unitarias.
- Un bug corregido ⇒ un test que lo reproduce primero.

### Casos mínimos que no deben faltar
Estos casos son **riesgo real**, no cobertura: cada uno cubre un fallo silencioso, una brecha de
seguridad o un invariante cuya violación rompería el agregado.

| Tema | Casos |
|---|---|
| Rachas | consecutivos hasta hoy; vivo con solo ayer; roto tras hueco > 1 día; mejor racha; duplicados |
| Movimientos | monto 0, negativo, 3 decimales, > máximo; fecha de mañana; descripción larga; categoría ajena |
| Categorías | nombre vacío, > 50, duplicado (con espacios al borde) |
| Aislamiento | cada caso de uso con id: otro usuario ⇒ `ResourceNotFoundException` y `SaveCount == 0` |
| Contratos | cada contrato registrado envía a un handler; sin `IRequestHandler` duplicado por contrato |
| Tiempo | "hoy" cerca de medianoche con zona distinta de UTC |

## 4. Cobertura: se mide y se reporta, no se persigue

`RNF-06` exige el **dato** de cobertura de `Domain` y `Application`, así que el CI lo mide y lo
publica en cada ejecución. Eso es lo que el requisito pide: un dato del TFM, no una puerta de calidad.

La cobertura **no decide qué tests se escriben**. Esa decisión la toma el criterio de riesgo de la
sección 3. La diferencia importa: con la cobertura como meta, la respuesta natural a "aquí hay una
línea sin cubrir" es escribir un test para tocarla; con la cobertura como señal, la pregunta correcta
es *"si fallara ahí, ¿dolería?"*. Si dolería, falta un test. Si no, no hace falta.

**Prohibido rellenar cobertura.** Un test sin aserción relevante, o que solo ejercita una línea para
tocarla, es peor que no escribirlo: se rompe en cada refactor, no asserta nada y da una falsa sensación
de seguridad.

El orden es siempre el mismo: `dotnet test` con cobertura → mirar las ramas sin cubrir → decidir por
riesgo, no por porcentaje.

## 5. Pipeline de CI recomendado (pasos)
1. `checkout` → `setup-dotnet` (usando `global.json`).
2. `dotnet restore` (con `--locked-mode` cuando exista `packages.lock.json`).
3. `dotnet format --verify-no-changes --no-restore`.
4. `dotnet build -c Release --no-restore` (con `CI=true`: warnings = errores).
5. `dotnet test -c Release --no-build --collect:"XPlat Code Coverage"`.
6. `dotnet list package --vulnerable --include-transitive` y fallar si hay hallazgos.
7. Workflow aparte de **CodeQL** (C#) y **Dependabot** (NuGet y GitHub Actions).
8. Permisos del workflow: `contents: read` por defecto.
Plantilla de YAML en `docs/06-despliegue-azure.md`.
