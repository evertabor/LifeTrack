# Pruebas de UX con Playwright

## Objetivo

Añadir pruebas de navegador reproducibles para detectar regresiones en los recorridos financieros principales de LifeTrack y ejecutarlas en CI.

## Problema

La solución solo tiene pruebas unitarias y de integración HTTP. No existe una prueba que interactúe con la aplicación mediante un navegador real, por lo que JavaScript, navegación, formularios y comportamiento adaptable pueden fallar sin ser detectados.

## Por qué

La guía de pruebas propone validar el flujo completo de alta, creación de categoría y registro de movimiento. La aplicación necesita cubrir ese recorrido en navegador, comprobar resultados financieros visibles y conservar la semántica y seguridad configuradas.

## Alcance autorizado

- Añadir un proyecto Playwright para .NET integrado con xUnit y `LifeTrack.sln`.
- Ejecutar la aplicación en Kestrel contra una base SQL Server aislada por ejecución, sin usar Azure ni datos de desarrollo.
- Cubrir autenticación y acceso anónimo, categorías, validación y creación de movimientos, totales visibles, teclado y tamaños de pantalla móvil/escritorio.
- Corregir el filtro de categorías, cuyo script inline es incompatible con la CSP predeterminada, y verificarlo desde el navegador.
- Integrar la ejecución de Chromium en `.github/workflows/ci.yml` y documentar la ejecución local.
- No ampliar la funcionalidad financiera, relajar controles de seguridad ni modificar la configuración de las pruebas de arquitectura.

## Restricciones y decisiones

- Rama de trabajo: `feat/playwright-ux-tests`.
- Modo TDD: desactivado explícitamente en `openspec/config.yaml` (`strict_tdd: false`); ejecutor funcional existente: `dotnet test`.
- Código, identificadores y nombres de pruebas en inglés; documentación del repositorio en español.
- No incluir comentarios en el código.
- Mantener SQL Server como proveedor de producción; las pruebas usan una base desechable y no comparten datos entre ejecuciones.
- No almacenar credenciales de prueba en archivos versionados.
- Estimación ajustada: aproximadamente 1.000 líneas editadas, incluyendo la integración/documentación prevista y excluyendo el lockfile generado. Estrategia de entrega: `ask-on-risk`; cadena elegida por el usuario: `feature-branch-chain`. No se crea ni publica un PR.
- Preservar `docs/guion-defensa-tfm.md` y `docs/presentacion-lifetrack.html`, preexistentes y sin seguimiento.

## Criterios de aceptación

- La suite ejecuta Chromium contra Kestrel y SQL Server local de prueba; falla con un mensaje accionable si faltan requisitos del host.
- Un flujo de registro valida la sesión y las categorías predeterminadas en la interfaz.
- La navegación anónima conduce al login; los errores de autenticación no revelan si una cuenta existe.
- La interfaz permite crear y filtrar categorías y muestra validación ante datos duplicados.
- Un movimiento válido aparece en la lista y los totales del periodo coinciden con los valores exactos esperados; las entradas inválidas muestran errores visibles.
- Los controles principales pueden operarse con teclado y la navegación/formularios se verifican en viewport móvil y de escritorio.
- El filtro de tipo de categoría funciona sin violaciones CSP causadas por JavaScript inline.
- CI instala Chromium y ejecuta la suite junto al build y los tests existentes.
- La documentación describe requisitos, comandos locales, aislamiento de datos y límites de la cobertura automatizada.

## Tareas

- [x] UX-01: Crear el proyecto de pruebas Playwright .NET/xUnit, el host Kestrel y el ciclo de vida de la base SQL Server desechable.
- [x] UX-02: Implementar los recorridos de navegador y corregir/verificar el filtro de categorías bajo CSP.
- [x] UX-03: Integrar Chromium y los requisitos de SQL Server en CI y documentar el flujo de ejecución local.
- [ ] UX-04: Ejecutar ambos tests Playwright contra SQL Server local desechable y Chromium, y observar el teardown completo.

## Verificación aplicable

- `dotnet restore`
- `dotnet build -c Release`
- `dotnet test`
- `dotnet format --verify-no-changes`
- Instalar Chromium con el script Playwright generado para el proyecto y ejecutar el proyecto UX contra SQL Server local desechable.
- Confirmar que la ejecución elimina la base de pruebas y que no cambia los archivos sin seguimiento preexistentes.

## Evidencia de trabajo

- Commit de implementación: `1923fa4a81a88141159085bc0d9f9d4edd97caf8` — `test(ux): add Playwright browser journeys`.
- Hooks del commit: pruebas de arquitectura, solución y formato pasaron; 255 tests pasaron y los dos tests Playwright se omitieron por falta de SQL local; compilación sin warnings y auditoría NuGet sin paquetes vulnerables.
- Verificación manual adicional: `dotnet restore --locked-mode` pasó. El workflow CI y el runtime con SQL Server/Chromium no se ejecutaron desde este entorno.
- Reversión: revertir el commit anterior elimina el proyecto Playwright, la corrección del filtro CSP, la configuración CI y la guía; no afecta los documentos de defensa preexistentes.

## Progreso y siguiente paso

- Estado: UX-01, UX-02 y UX-03 están implementadas. UX-04 queda pendiente porque no hay una conexión SQL Server local configurada ni Chromium instalado.
- Verificación observada: `dotnet restore --locked-mode`, `dotnet build -c Release --no-restore`, `dotnet format --verify-no-changes --no-restore` y la comprobación parental `dotnet test -c Release --no-build` pasan; 255 pruebas pasan y las dos pruebas Playwright se omiten porque falta `LIFETRACK_UX_TESTS_SQLSERVER_CONNECTION_STRING`.
- Cobertura de navegador implementada: registro y categorías predeterminadas, errores de login genéricos para cuenta inexistente y existente, redirección privada anónima, creación/duplicados/filtro de categorías, validación y registro de movimientos, totales exactos de movimientos y dashboard, navegación por teclado/móvil y ausencia de error CSP por script inline.
- La revisión independiente estática no encontró defectos introducidos en los recorridos, la frontera SQL, el CI ni el runbook; cada fixture limpia solo el pool de su base GUID, sin afectar pruebas paralelas. Tampoco certifica el comportamiento del navegador ni el workflow remoto.
- Límite pendiente: no se abrió una conexión, no se inició Docker ni se instalaron navegadores; el flujo Kestrel + SQL Server + Chromium aún no se ha ejecutado.
- Siguiente paso: revisar y registrar el commit de trabajo sin abrir/publicar PR; después, ejecutar UX-04 cuando estén disponibles los prerrequisitos locales.
