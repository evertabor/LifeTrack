# AGENTS.md — tests

Reglas generales en `/AGENTS.md` (secciones 6 y 10).

## Qué se prueba

> **Se prueba cuando el fallo sería silencioso, una brecha de seguridad, o una regresión ya ocurrida.**

| Categoría | Qué es | Cómo se prueba |
|---|---|---|
| **Fallo silencioso** | Un número incorrecto que se muestra al usuario y nadie detecta: un total que no cuadra, una proyección errónea | Un `Assert` sobre el valor exacto |
| **Brecha de seguridad** | Cada handler que recibe un id | Test de IDOR que espera `ResourceNotFoundException` **y comprueba que no se persistió nada** (`SaveCount == 0`) |
| **Regresión** | Cada bug ya corregido | El test que lo reproduce, escrito primero |

No se escribe un test por cada rama ni por cada criterio de aceptación. Un test de IDOR que solo
mira la excepción no verifica que el sistema quedó consistente: la brecha puede dejar un registro a
medias y el test pasa.

## Qué no se prueba

- **Plumbing de framework**: que MediatR resuelva el contrato al handler, que un repositorio implemente su interfaz, que DI registre el servicio. Eso lo comprueban los tests de arquitectura, no un test unitario.
- **Proyecciones triviales**: un `record` DTO, un `From(...)` que copia campos sin transformar.
- **Paso a paso**: un handler que solo reenvía a otro caso de uso sin lógica propia.
- **Renderizado de vistas**: una vista Razor no se prueba unitariamente; si hay lógica, el fallo es que esa lógica existe en la vista, y lo detecta la revisión.
- **Getters y setters** de entidades sin invariantes.

## Cobertura: se mide y se reporta, no se persigue

`RNF-06` exige el dato de cobertura de `Domain` y `Application`, así que el CI lo mide y lo publica.
La cobertura **no decide qué tests se escriben**: lo decide el criterio de riesgo. Se lee como
**diagnóstico**: se ejecuta, se mira qué ramas quedan sin cubrir y se pregunta *"si fallara ahí,
¿dolería?"*. Si dolería, falta un test; si no, no hace falta. **Prohibido rellenar cobertura**: un
test sin aserción relevante, o que solo ejercita una línea para tocarla, es peor que no escribirlo —
se rompe en cada refactor, no asserta nada y da una falsa sensación de seguridad.

## Convenciones

- **Cero comentarios** en el código de test: ni XML, ni `//`, ni `/* */`. El nombre del test
  (`Método_Condición_ResultadoEsperado`) y el aserto explican el caso; si la razón no cabe en el
  código, va en `docs/`.
- **xUnit**. Nombre `Método_Condición_ResultadoEsperado`. Arrange / Act / Assert. Un concepto por test.
- Nombres de test, fixtures y clases en **inglés**.
- `LifeTrack.Domain.Tests`: reglas de dominio puras, sin dobles, con `[Theory]` para los bordes.
- `LifeTrack.Application.Tests`: casos de uso enviados por **`ISender`** con *fakes* en memoria (`Fakes/`): `FakeCurrentUser`, `FakeUnitOfWork`, `FixedTimeProvider`, `InMemory…Repository`. **Opción preferida: enviar el contrato por `ISender`** con `AddMediatR` sobre un `ServiceCollection` real, porque es exactamente lo que hace un controller y así el test cubre también el enlace contrato → handler (un contrato mal declarado se detecta en el propio test). Construir el handler directamente sigue siendo válido y es más rápido cuando el test se centra en un único handler y no necesita el dispatcher. Cada caso de uso con id incluye un **test de IDOR** que además comprueba `SaveCount == 0`. Sin librerías de mocking: los dobles son *fakes* escritos a mano.
- Un *fake* es una implementación más de la interfaz, así que le aplica LSP: `InMemoryTransactionRepository` filtra por `userId` igual que `TransactionRepository`. Si no lo hiciera, el test de IDOR passing contra el *fake* fallaría en producción.
- `LifeTrack.Architecture.Tests` (NetArchTest.Rules): Domain sin dependencias; Application sin Infrastructure/Web/EF/ASP.NET; Infrastructure sin Web; **controllers sin `Domain` ni `Infrastructure`** y sin handlers concretos inyectados; ningún `ViewModel` heredando de `Domain`; handlers `sealed`. **No se modifican para "hacerlos pasar".** Son la puerta que hace exigible la **D** de SOLID; las otras cuatro son criterio de revisión.
- Un test de arquitectura más comprueba que `Program.cs` registra **`AutoValidateAntiforgeryToken`** como filtro global. En MVC el anti-forgery no es automático; si alguien lo quita, los POST quedan sin token y debe fallar el build.
- `ViewModels`: test de la fábrica `From(...)` que comprueba el formato y los valores de visibilidad, **cuando ese cálculo alimenta un número que el usuario ve**. Es la única lógica que las vistas ya no pueden hacer, así que un fallo ahí es silencioso.
- Sin `Thread.Sleep`, sin reloj real, sin red, sin base de datos real en tests unitarios.
- Pruebas de integración (opcional): `WebApplicationFactory<Program>` con SQLite en memoria; nunca contra la base de datos de Azure.