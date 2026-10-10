# ADR-0004: MediatR como dispatcher de casos de uso

- **Estado:** Aceptada
- **Fecha:** 2026-10-02
- **Revisión:** 2026-10-03. La decisión original fue **no** usar MediatR: los casos de uso eran
  clases `sealed` `<Verbo><Sustantivo>Handler` con un método público propio, registradas en `Web` por
  escaneo de clases `*Handler`, y `Application` se mantenía sin ningún paquete NuGet.
  La decisión cambió al llegar a un umbral concreto: el registro por escaneo obligaba a que cada
  controller inyectara un handler por acción, lo que producía constructores verbosos y chocaba con el
  límite de parámetros. La uniformidad del patrón y el acceso a *behaviors* pesan más que esa fricción.
  El **coste en licencia no inclinó la balanza a favor: se evaluó y se aceptó a conciencia**, junto con
  la pérdida de resolución en tiempo de compilación que se detallan en §Consecuencias.

## Contexto
`Application` contiene los casos de uso: cada uno es un comando o una consulta, con su handler, sus
DTOs de salida y las interfaces que necesita. `Web` es un adaptador HTTP delgado y no debe conocer
detalles de persistencia ni de dominio.

La primera versión de esta decisión evitaba cualquier dependencia externa en la capa de casos de uso.
Al introducir MediatR hay que responder a tres preguntas concretas:

1. **Qué** contratos implementan los comandos y las consultas.
2. **Quién** resuelve el handler y **cuándo**: en tiempo de ejecución, por nombre de tipo.
3. **Dónde** vive la decisión de enrutar cada mensaje a su handler.

MediatR v13 introduce además un cambio de licencia que condiciona el coste de la dependencia: la
versión **13.0.0 (julio de 2025)** dejó de distribuirse bajo Apache 2.0 y pasa a ser **dual, bajo
RPL-1.5 o licencia comercial**. Las versiones **v12.5.0 y anteriores** siguen bajo Apache 2.0, pero
**sin soporte, sin actualizaciones de seguridad y sin parches**.

## Decisión
**MediatR v13 es el dispatcher de los casos de uso.** Concretamente:

- Cada caso de uso es un contrato `record` que implementa `ICommand<T>` (escritura con resultado),
  `IQuery<T>` (lectura) o `IRequest` (escritura **sin** resultado).
- Cada handler es `sealed` e implementa `IRequestHandler<TRequest, TResponse>` o, cuando no devuelve
  nada, `IRequestHandler<TRequest>`. Desde MediatR v12 `IRequestHandler<TRequest>` **no** hereda de
  `IRequestHandler<TRequest, Unit>`: el método devuelve `Task`, no `Task<Unit>`.
- El punto de entrada es la interfaz mínima **`ISender`**. Los controllers inyectan `ISender` y
  envían el contrato; `IPublisher` queda reservado a notificaciones, que este proyecto no usa.
- El registro se hace en la raíz de composición (`Program.cs`) con `AddMediatR`, que escanea un
  ensamblado buscando implementaciones concretas de `IRequestHandler<,>`, `IRequestHandler<>`,
  `INotificationHandler<>`, `IStreamRequestHandler<>` e `IRequestExceptionHandler<,,>`.
- **El escaneo apunta al ensamblado de `Application`, nunca al de `Program`.** Para hacerlo sin
  acoplar `Web` a un tipo concreto de un caso de uso, `Application` publica la clase ancla
  `LifeTrack.Application.Common.ApplicationAssemblyAnchor`. Escanear `typeof(Program).Assembly`
  no encontraría ningún handler, porque los handlers viven en `Application`.
- El mapeo sigue siendo **manual** (proyecciones LINQ/`Select` y fábricas `ViewModel.From(...)`).
  Esta decisión no afecta al mapeo.

### Registro en la raíz de composición

La clave de licencia **solo es necesaria en despliegues de producción**: en desarrollo local y en CI,
MediatR escribe como mucho un warning. El escaneo apunta al ensamblado de `Application` a través de la
clase ancla, nunca al del punto de entrada.

```csharp
services.AddMediatR(cfg =>
{
    cfg.LicenseKey = configuration["MediatR:LicenseKey"] ?? string.Empty;
    cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyAnchor).Assembly);
});
```

### Envío desde un controller

```csharp
var habits = await sender.Send(new ListHabitsQuery(), cancellationToken);
await sender.Send(new CreateHabitCommand(input.Name), cancellationToken);
```

### Traducción de excepciones: sigue siendo explícita

Este ADR **no** cambia el modelo de errores. La decisión vigente es que cada handler captura
`DomainException` y `NotFoundException` del dominio y las relanza con `try`/`catch` **explícito** como
`BusinessRuleException` o `ResourceNotFoundException`, y `Web` solo captura esas dos de frontera.

MediatR v13 ofrece `IRequestExceptionHandler<TRequest, TResponse, TException>` para resolver esto con
un *behavior* registrado, y esa vía queda anotada como **opción futura**. El modelo vigente
continúa siendo el `try`/`catch` explícito, porque hace legible el flujo de error y no depende de una
convención oculta. Si en el futuro se adopta `IRequestExceptionHandler<,,>`, hará falta un ADR que
redefina este punto y su §Consecuencias.

### Behaviors: no se registran por escaneo

`IPipelineBehavior`, los *behaviors* y los pre/post processors **no** se descubren escaneando el
ensamblado: requieren un registro explícito con su `Add…` correspondiente. Hoy el proyecto no registra
ninguno. Si se añade uno, es un `Add…` explícito en la raíz de composición, no una convención.

## Consecuencias

- **(−) `Application` deja de estar sin paquetes NuGet.** La capa referencia `MediatR`, que arrastra
  `Microsoft.Extensions.DependencyInjection.Abstractions` como dependencia transitiva. El núcleo ya no
  compila sin acceso a internet: `dotnet restore` necesita NuGet. Se pierde la propiedad de "núcleo
  compilable sin red" y con ella parte de la ventaja de superficie de cadena de suministro que motivó
  la decisión original.
- **(−) La resolución pasa a ser en tiempo de ejecución en lugar de en tiempo de compilación.** Un
  contrato sin handler registrado no falla al compilar: falla al ejecutarse. El fallo aparece en el
  primer `Send` de esa ruta, no en el build.
- **(−) Se pierde la navegación directa al handler.** "Ir a definición" desde el `sender.Send(...)`
  lleva al contrato, no al handler. El enlace con el handler es por tipo, en tiempo de ejecución.
- **(−) El escaneo del ensamblado correcto es una trampa fácil.** Registrar con
  `typeof(Program).Assembly` compila y no registra nada. De ahí la clase ancla explícita y la regla de
  que el escaneo apunte a `Application`.
- **(−) Una dependencia más que auditar:** NuGet Audit, `dotnet list package --vulnerable` y el
  *lock file* pasan a cubrir `MediatR` y su grafo transitivo.
- **(+) Un solo `ISender` por controller.** Desaparece la fricción de inyección: el controller ya no
  crece con un handler concreto por cada acción, y añadir un caso de uso no obliga a tocar la lista
  de dependencias del controller.
- **(+) Behaviors disponibles.** Las preocupaciones transversales (validación, logging, métricas) tienen
  un punto de extensión estándar, sin *helpers* repartidos por los handlers.
- **(+) Código uniforme.** Contratos, handlers, envío y registro siguen una convención única y
  reconocible: el handler es siempre `IRequestHandler`, siempre `sealed`, y siempre con
  `CancellationToken` como último parámetro.
- **(+) Interfaz mínima en el consumidor.** `ISender` documenta exactamente lo que un controller
  necesita, y permite sustituir el dispatcher en los tests.
- **(+) Tests más sencillos.** Un caso de uso se puede enviar por `ISender` con un contenedor real y
  fakes, sin construir el handler a mano.

## Licencia y coste

- MediatR **v13.0.0 (julio de 2025)** es dual: **RPL-1.5** o licencia comercial.
- **v12.5.0 y anteriores** permanecen bajo Apache 2.0, **sin soporte, sin actualizaciones de
  seguridad y sin parches**: no son una alternativa mantenible.
- v13 **exige aceptar la licencia** en el momento de la instalación. Para uso **personal, educativo y
  no comercial** existe la **Community License**, gratuita y de **autoservicio** (sin proceso de
  aprobación). Este proyecto es de uso personal y educativo, por lo que encaja en ese supuesto.
- **No hace falta clave de licencia** en desarrollo, CI/CD, staging, QA ni integración. **Solo en
  despliegues de producción.**
- La ejecución de la licencia es **autocontenida**: no hay servidor de licencias ni llamadas de red.
  Sin clave, con clave expirada o inválida, la librería **solo emite warnings o errores en el log**:
  no hay límites de ejecución, ni bloqueo de funcionalidad, ni degradación. La política declarada es
  "we trust our users".
- La clave se pasa por **configuración** (variable de entorno, App Settings o secret store).
  **Nunca** hardcodeada en código, ni en `appsettings.json`, ni en el repositorio.

## Cuándo reconsiderar
- Si la aplicación pasa a **uso comercial**, la Community License deja de encajar y hay que valorar la
  licencia comercial de MediatR o volver a la decisión anterior.
- Si aparece un volumen alto de preocupaciones transversales repetidas, la vía estándar es un
  `IPipelineBehavior` registrado explícitamente; si aun así sobran decoradores propios, se reevalúa la
  dependencia completa.
- Si MediatR publica una nueva versión con cambio de licencia o de modelo de precios, se revisa este
  ADR antes de actualizar la versión pineada.
